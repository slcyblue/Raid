using BackEnd;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Raid : GameState
{
    public enum StageBuffTargetEnum { BossMonster = 1, NormalMonster = 2, PlayerUnit = 3 }

    #region Variables
    RaidUI uiMgr;
    RaidPopup popupMgr;

    RaidCSVContainer csvcontainer;
    RaidDifficultyCSV csv;
    UnitGroup unitGroup;
    Action InitSuccessListener = null;

    public List<StageBuffCSV> raidBuffs { get; private set; } = new List<StageBuffCSV>();
    List<RaidMonsterCSV> middleMonsters = new List<RaidMonsterCSV>();
    List<RaidMonsterCSV> bossMonsters = new List<RaidMonsterCSV>();
    public static int difficulty { get; private set; }
    public static int raidId { get; private set; }
    public static bool isAutoOn { get; private set; }

    Asset respawnPrice;
    private ContentInGameStateEnum gameStatus;

    public float remainTs { get; private set; }
    private int normalMobIndex = 0;
    private RaidUnitStatUI statUI = null;

    private int crrBossSkillId = 0;
    private int crrPhaseCount = 1;

    private int remainRespawnCount = 0;
    Unit mainBoss;
    Unit middleBoss;
    #endregion


    #region Init
    public static void SetGame(int _raidId, int _difficulty)
    {
        raidId = _raidId;
        difficulty = _difficulty;

        var medStat = DB.Get<DatabaseStat>().GetMedianStat();
        PresetPropertyManager.GetInstance().SetStat(PresetEnum.Raid, medStat);
    }
    public override void Initialize(Action completeInitCallback = null)
    {
        this.InitSuccessListener = completeInitCallback;
        csvcontainer = RaidCSVContainer.GetInstance();
        csv = csvcontainer.GetRaidCSV(raidId, difficulty);
        if (csv == null)
        {
            DebugX.LogErrorFormat("raidCSV not exst. id:{0}, diff:{1}", raidId, difficulty);
            return;
        }
        SoundManager.GetInstance().PlayBGM(csvcontainer.GetRaidSoundKey(raidId));
        remainRespawnCount = csv.maxRespawnCount;
        gameStatus = ContentInGameStateEnum.Ready;

        FindRespawnPrice();
        LoadMonsters();
        StartCoroutine(ILoadObjects());
    }

    void FindRespawnPrice()
    {
        var container = StageCSVContainer.GetInstance();

        int respawnPriceType = container.GetGlobalInt(StageGlobalEnum.unitRespawnPriceType);
        int respawnPriceId = container.GetGlobalInt(StageGlobalEnum.unitRespawnPriceId);
        long priceCount = container.GetGlobalInt(StageGlobalEnum.unitRespawnPriceCount);

        respawnPrice = new Asset(respawnPriceType, respawnPriceId, priceCount);
    }

    void LoadMonsters()
    {
        middleMonsters.Clear();
        bossMonsters.Clear();

        var list = csvcontainer.GetMonsters(raidId, difficulty);
        if (list == null) return;

        foreach (var item in list)
        {
            if (item.monsterType == 1) middleMonsters.Add(item);
            else if (item.monsterType == 2) bossMonsters.Add(item);
        }
    }

    public void SetAuto(bool isOn)
    {
        isAutoOn = isOn;
        //if (uiMgr != null) uiMgr.UpdateAutoToggle();
    }

    IEnumerator ILoadObjects()
    {
        var backMgr = BackgroundManager.GetInstance();
        BackgroundManager.GetInstance().ChangeBackground(csv.backgroundName);
        CameraManager.GetInstance().SetCamSize(15);
        while (backMgr.currentMap == null) yield return new WaitForSeconds(.1f);

        yield return StartCoroutine(LoadUnits());

        AssetManager.CreateObject(ResourceTypeEnum.UI, "RaidUI", GameManager.GetInstance().uiTransform, OnLoadUI);
        while (uiMgr == null) yield return new WaitForSeconds(.1f);

        AssetManager.CreateObject(ResourceTypeEnum.UI, "RaidStatUI", uiMgr.mainPanel, OnLoadStatUI);
        while (statUI == null) yield return new WaitForSeconds(.1f);

        AssetManager.CreateObject(ResourceTypeEnum.Popup, "RaidPopup", GameManager.GetInstance().popupTransform, OnLoadPopup);
        while (popupMgr == null) yield return new WaitForSeconds(.1f);

        OnInitComplete();
    }

    IEnumerator LoadUnits()
    {
        List<Unit> units = new List<Unit>();

        CreateUnits(units, csv.totalUnitCount);
        yield return new WaitUntil(() => units.Count == csv.totalUnitCount);
        SetUnitGroup(units);
    }

    void CreateUnits(List<Unit> units, int unitCount)
    {
        for (int i = 0; i < unitCount; i++)
        {
            AssetManager.CreateObject(ResourceTypeEnum.Unit, "unit", transform, (obj) =>
            {
                var unit = obj.GetComponent<CharacterUnit>();
                units.Add(unit);
                //unit.OnActiveSkill += OnUserActiveSkill;
            });
        }
    }

    void SetUnitGroup(List<Unit> units)
    {
        unitGroup = new PlayerUnitGroup();
        var container = PresetPropertyManager.GetInstance().GetPresetContainer(PresetEnum.Raid);

        if (container == null)
        {
            DebugX.LogWarning("RaidPresetContainer not exist.");
            return;
        }

        unitGroup.Initialize(UnitEnum.UserPlayer, units, container as IUnitGroupPropertyContainer);
        BackgroundManager.GetInstance().SetMapSyncUnit(units);
        var list = csvcontainer.GetPositionList(raidId, difficulty);
        var preset = container as PresetPropertyContainer_Raid;

        int index = 0;
        foreach (var item in preset.sortList)
        {
            unitGroup.SetUnitPosition(list[item.slotId - 1], index);
            index++;
        }                
        
    }

    void OnLoadStatUI(GameObject obj)
    {
        statUI = obj.GetComponent<RaidUnitStatUI>();
        statUI.Init(unitGroup, true);
    }

    void OnLoadPopup(GameObject obj)
    {
        popupMgr = obj.GetComponent<RaidPopup>();
        popupMgr.SetPlaneDistance(1400);
        popupMgr.Initialize(this);
    }

    void OnLoadUI(GameObject obj)
    {
        uiMgr = obj.GetComponent<RaidUI>();
        uiMgr.Initialize(this);
        uiMgr.SetPlaneDistance(1500);
        uiMgr.UpdateRespawnCount(remainRespawnCount);
        uiMgr.UpdateRaidName(csv.raidName);
    }


    void OnInitComplete()
    {
        InitSuccessListener?.Invoke();
    }

    public override void StartGame()
    {
        remainTs = csv.challengeLimitTs;
        ActiveRaidBuffToUnits();
        StartCoroutine(IDelay());
    }

    IEnumerator IDelay()
    {
        uiMgr.SetSideUIs(false);
        uiMgr.ShowTimerEff();
     
        yield return new WaitForSeconds(0.8f);
        
        uiMgr.SetSideUIs(true);
        uiMgr.HideTimerEff();

        OnFinishDelay();
    }

    void OnFinishDelay()
    {
        StartCoroutine(INormalMobCreater());
        StartCoroutine(ITimer());

        gameStatus = ContentInGameStateEnum.InGame;
        unitGroup.StartBattle();

        CameraManager.GetInstance().SetCamSizeAuto(true);
        GameManager.GetInstance().ShowUnitBattleSpeech(unitGroup);
    }

    #endregion

    #region StageBuffs
    void ActiveRaidBuffToUnits()
    {
        var list = csvcontainer.GetRaidBuffs(raidId, difficulty, crrPhaseCount);
        
        foreach(var item in list)
        {
            if (item.targets[0] != 3) continue;
            raidBuffs.Add(item);
        }

        int length = unitGroup.units.Count;
        for (int i = 0; i < length; i++)
        {
            var unit = unitGroup.units[i];
            AddRaidBuffToUnit(unit, list);
        }

        AddRaidBuffToBoss(mainBoss, list);
        uiMgr.UpdateBuff();
    }

    void AddRaidBuffToUnit(Unit unit, List<StageBuffCSV> list)
    {
        if (unit == null || unit.IsDie()) return;
        if (unit.propMgr == null) return;

        foreach(var item in list)
        {
            if (item.targets[0] != 3) continue;

            var info = PropertyFactory.CreatePropertyInfo(item);
            unit.propMgr.AddProperty(info);
        }
    }

    void AddRaidBuffToBoss(Unit unit, List<StageBuffCSV> list)
    {
        if (unit == null || unit.IsDie()) return;
        if (unit.propMgr == null) return;

        foreach (var item in list)
        {
            if (item.targets[0] != 1) continue;
            
            var info = PropertyFactory.CreatePropertyInfo(item);
            unit.propMgr.AddProperty(info);
        }
    }

    void InActiveRaidBuffToUnits(int phaseCount)
    {
        int length = unitGroup.units.Count;
        for (int i = 0; i < length; i++)
        {
            var unit = unitGroup.units[i];
            RemoveRaidBuffToUnit(unit, StageBuffTargetEnum.PlayerUnit, phaseCount);
        }

        uiMgr.UpdateBuff();
    }

    void RemoveRaidBuffToUnit(Unit unit, StageBuffTargetEnum type, int phaseCount)
    {
        int buffType = (int)type;
        if (unit == null || unit.IsDie()) return;
        if (unit.propMgr == null) return;
        //if (!raidBuffs.ContainsKey(buffType)) return;
        //var list = raidBuffs[buffType];

        //todo
        //페이즈에 해당하는 레이드 버프만 적용하는 부분 구현

        //unit.propMgr.RemoveProperty(list[phaseCount - 1]);
    }

    
    #endregion

    #region Battle
    public override void SetPlayerStatMax()
    {
        throw new NotImplementedException();
    }
    public override void SetPlayerAutoSkill(bool isOn)
    {
        var units = unitGroup.units;
        foreach (var item in units)
        {
            item.SetAutoSkill(isOn);
        }
    }
    IEnumerator ITimer()
    {
        while (true)
        {
            yield return new WaitForEndOfFrame();
            if (gameStatus != ContentInGameStateEnum.InGame) continue;

            remainTs -= TimeManager.DeltaTime;

            if (remainTs <= 0)
            {
                FailChallenge();
                break;
            }
        }
    }

    public int middleBossRemainTs { get; private set; } = 0;
    public float middleBossActiveTs { get; private set; } = 0;

    bool IsMiddleBossUsedSkill = false;
    IEnumerator INormalMobCreater()
    {
        CreateBossMob();

        yield return new WaitForSeconds(3.0f);

        CreateMiddleBossGroup();

        while (true)
        {
            yield return new WaitForSeconds(1f);
            middleBossRemainTs += 1;
            if (middleBossRemainTs >= csv.middleBossRespawnTs)
            {
                CreateMiddleBossGroup();
                middleBossRemainTs = 0;
            }
            uiMgr.SetMiddleBossRespawnTs(csv.middleBossRespawnTs);
        }
    }

    void CreateMiddleBossGroup()
    {
        int index = normalMobIndex;
        var pos = BackgroundManager.GetInstance().GetRandomPositionOnMap();
        MonsterPool.GetInstance().CreateMonster(middleMonsters[index].bodyName, (unit) =>
        {
            unit.SetPosition(pos);
            OnCreateMiddleBoss(unit, middleMonsters[index]);
            StartCoroutine(CheckNormalMobDead(middleMonsters[index]));
        });

        normalMobIndex = (normalMobIndex + 1) % middleMonsters.Count;
    }

    void OnCreateMiddleBoss(MonsterUnit unit, RaidMonsterCSV monsterCsv)
    {
        unit.Initailze(UnitEnum.MiddleBoss);
        var state = gameObject.AddComponent<MiddleBossStateMachine>();
        unit.ChangeStateMachine(state);
        unit.SetStat(monsterCsv.GetHp(difficulty), monsterCsv.GetPower(difficulty));
        unit.AddSkill(monsterCsv.skillIds[0]);
        unit.gameObject.SetActive(true);

        unit.ActiveStateMachine();
        unit.AttachStatViewer();
        uiMgr.ConnectMiddleBoss(unit, monsterCsv.skillIds[0]);
        uiMgr.UpdateMiddleBossName(monsterCsv.monsterName);
        middleBoss = unit;
        PostAlarmManager.GetInstance().CreateRaidPostAlarm("중간보스가 생성되었습니다");
        IsMiddleBossUsedSkill = false;
    }

    IEnumerator CheckNormalMobDead(RaidMonsterCSV monsterCsv)
    {
        middleBossActiveTs = 0;

        while(middleBossActiveTs <= csv.middleBossActiveTs)
        {
            yield return new WaitForEndOfFrame();
            middleBossActiveTs += Time.deltaTime;   
        }

        if (!middleBoss.IsDie())
        {
            middleBoss.ChangeState(UnitStateEnum.Skill);
            PostAlarmManager.GetInstance().CreateRaidPostAlarm("중간보스 스킬 사용!");
            IsMiddleBossUsedSkill = true;
        }
    }

    void CreateBossMob()
    {
        var csv = bossMonsters[0];

        MonsterPool.GetInstance().CreateMonster(csv.bodyName, (unit) => OnCreateBossMonster(unit, csv));
    }

    void OnCreateBossMonster(MonsterUnit unit, RaidMonsterCSV monsterCsv)
    {
        if (unit == null) return;
        mainBoss = unit;

        crrBossSkillId = monsterCsv.skillIds[0];
        unit.SetPosition(monsterCsv.respawnPosition);

        unit.Initailze(UnitEnum.Boss);
        unit.AddSkill(crrBossSkillId);

        unit.SetStat(monsterCsv.GetHp(difficulty), monsterCsv.GetPower(difficulty));
        unit.battleInfo.distance = 20;
        unit.gameObject.SetActive(true);
        unit.ActiveStateMachine();
        unit.AttachStatViewer();
        uiMgr.ConnectBoss(unit, monsterCsv.skillIds[0]);
        uiMgr.UpdateBossSkillIcon(monsterCsv);
        uiMgr.SetBossSkillEffect(crrBossSkillId);
        uiMgr.UpdateBossName(monsterCsv.monsterName);

        StartCoroutine(CheckBossCondition(unit, monsterCsv));
    }

    IEnumerator CheckBossCondition(MonsterUnit unit, RaidMonsterCSV monsterCsv)
    {
        while (!unit.IsDie())
        {
            float hpRatio = mainBoss.stat.crrHp / monsterCsv.GetHp(difficulty) * 100;
            int bossSkillId;

            int phaseCount = csv.totalPhaseCount;
            for (int i = 0; i < phaseCount; i++)
            {
                if (hpRatio <= (100 / phaseCount) * (phaseCount - i) && hpRatio > (100 / phaseCount) * (phaseCount - (i + 1)))
                {
                    bossSkillId = monsterCsv.skillIds[i];
                    if (crrBossSkillId != bossSkillId) SwapBossSkill(unit, bossSkillId, i+1);

                    break;
                }
            }

            yield return new WaitForEndOfFrame();
        }
    }

    void SwapBossSkill(MonsterUnit unit, int bossSkillId, int phaseCount)
    {
        unit.skillMgr.RemoveSkill(crrBossSkillId);
        crrBossSkillId = bossSkillId;
        unit.AddSkill(crrBossSkillId);
        uiMgr.SetBossSkillEffect(crrBossSkillId);

        if (crrPhaseCount != phaseCount) crrPhaseCount = phaseCount;
        ActiveRaidBuffToUnits();
    }

    void RemoveBossMob(string bodyName)
    {
        int length = bossMonsters.Count;
        for (int i = 0; i < length; i++)
        {
            if (bossMonsters[i].bodyName.Equals(bodyName))
            {
                bossMonsters.RemoveAt(i);
                return;
            }
        }

    }

    #endregion

    #region Unit Die
    public override void OnUnitDie(Unit unit)
    {
        switch (unit.unitTag)
        {
            case UnitEnum.Boss:
            case UnitEnum.MiddleBoss:
                OnMonsterDie(unit as MonsterUnit);
                break;
            case UnitEnum.UserPlayer:
                OnUserDie(unit as CharacterUnit);
                break;
        }
    }

    void OnUserDie(CharacterUnit unit)
    {
        if (unitGroup.AliveUnitCount() <= 0)
        {
            FailChallenge();
            return;
        }

        if (remainRespawnCount > 0)
        {
            statUI.OpenRespawnPanel(unit, respawnPrice, TryRespawnUnit);
        }
        else
        {
            statUI.SetDarkProfile(unit);
        }
        //GameManager.GetInstance().ShowDeathBattleSpeech(unit);
    }

    void TryRespawnUnit(Unit unit)
    {
        if (remainRespawnCount <= 0) return;
        if (!unit.IsDie()) return;

        bool success = PlayerStorage.GetInstance().Use(respawnPrice);
        if (!success)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(StatusCode.Client_NotEnoughMoney, PostAlarmEnum.Small);
            return;
        }

        DecrRespawnCount();
        unitGroup.RespawnUnit(unit);
        //AddStageBuffToUnit(unit, StageBuffTargetEnum.PlayerUnit);
        GameManager.GetInstance().GiveRespawnInvincibleBuff(unit);
        if (statUI != null) statUI.ReLoadTarget(unit);
    }

    void DecrRespawnCount()
    {
        remainRespawnCount--;
        uiMgr.UpdateRespawnCount(remainRespawnCount);

        if (remainRespawnCount <= 0) statUI.HidePersonalRespawnIcons();
    }

    void OnMonsterDie(MonsterUnit unit)
    {
        if (unit.unitTag == UnitEnum.MiddleBoss)
        {
            if(!IsMiddleBossUsedSkill) PostAlarmManager.GetInstance().CreateRaidPostAlarm("중간보스가 처치되었습니다.");
            uiMgr.DisConnnectMiddleBossHp();
        }
        else if (unit.unitTag == UnitEnum.Boss)
        {
            uiMgr.DisConnnectBossHp();
            RemoveBossMob(unit.monsterBodyName);

            if (bossMonsters.Count == 0) SuccessChallenge();
            else CreateBossMob();
        }
    }



    //void RespawnUnits()
    //{
    //    Vector2 pos = BackgroundManager.GetInstance().currentMap.unitActivePosition;
    //    unitGroup.SetPosition(pos);
    //    unitGroup.RespawnAllUnits();

    //    if (statUI != null) statUI.ConnectUnit();
    //}
    #endregion

    #region Finish Game
    public void FailChallenge()
    {
        if (gameStatus == ContentInGameStateEnum.Win) return;
        gameStatus = ContentInGameStateEnum.Lose;

        SetAuto(false);
        StopAllCoroutines();
        unitGroup.StopBattle();
        MonsterPool.GetInstance().ReturnAllMonsters();
        popupMgr.OpenFailPopup(new Asset(csv.ticketAssetTypes[0], csv.ticketAssetIds[0], csv.ticketAssetCounts[0]));

        var preset = PresetPropertyManager.GetInstance().GetPresetContainer(PresetEnum.Raid) as PresetPropertyContainer_Raid;
        preset.RemoveUnitGroup();
    }
    private void SuccessChallenge()
    {
        if (gameStatus == ContentInGameStateEnum.Lose) return;
        gameStatus = ContentInGameStateEnum.Win;
        MonsterPool.GetInstance().ReturnAllMonsters();
        unitGroup.StopBattle();
        StopAllCoroutines();

        var preset = PresetPropertyManager.GetInstance().GetPresetContainer(PresetEnum.Raid) as PresetPropertyContainer_Raid;
        preset.RemoveUnitGroup();

        StartCoroutine(ISuccessDelay());
    }

    IEnumerator ISuccessDelay()
    {
        var rewards = GiveSuccessReward();
        DB.Get<DatabaseContents_Raid>().ClearRaid(raidId, difficulty, csv.challengeLimitTs - remainTs);
        //ObserverManager.GetInstance().Notify(ObservingEnum.StageClear, stageId, difficulty);
        BackupManager.GetInstance().LocalBackUp();
        SendClearLog();

        foreach (var unit in unitGroup.units)
        {
            unit.PlayAnimation(UnitAnimEnum.victory.ToString());
        }

        float waitSec = uiMgr.ShowClearEffect();
        if (waitSec < 1.5f) waitSec = 1.5f;
        yield return new WaitForSeconds(waitSec);

        MonsterPool.GetInstance().ReturnAllMonsters();
        popupMgr.OpenSuccessPopup(rewards);

        //int maxDiff = csvcontainer.GetMaxDifficulty(stageId);
        //if (difficulty == maxDiff)
        //{
        //    popupMgr.HideNextStage();
        //}
        //else
        //{
        //    int nextDiff = mode == StageMode.Stage ? difficulty + 1 : difficulty;
        //    var needPrices = csvcontainer.GetChallengeEnterPrice(stageId, nextDiff);
        //    popupMgr.OpenNextStage(needPrices?[0] ?? new Asset());
        //}
    }

    void SendClearLog()
    {
#if UNITY_ANDROID
        if (BackendService.GetInstance().CheckStageLogEvent(difficulty))
        {
            Param param = new Param();
            string format = $"Difficulty #{difficulty}";
            param.Add($"RaidClear #{raidId}", format);
            BackendService.GetInstance().SendLog(BackendLogEnum.StageClear, param);
        }
#endif
    }

    List<Asset> GiveSuccessReward()
    {

        List<Asset> rewards = new List<Asset>();
        int length = csv.clearRewardAssetTypes?.Length ?? 0;
        for (int i = 0; i < length; i++)
        {
            float rate = csv.clearRewardRates[i];
            bool isWin = Calculator.IsRandomWin(rate);
            if (!isWin) continue;

            int type = csv.clearRewardAssetTypes[i];
            int id = csv.clearRewardAssetIds[i];
            long cnt = csv.clearRewardAssetCounts[i];

            var reward = new Asset(type, id, cnt);
            bool success = PlayerStorage.GetInstance().Gain(reward);

            if (success) rewards.Add(reward);
        }

        return rewards;

    }

    public void QuitGame(ISideJob job = null)
    {
        if (job != null) SideJobManager.GetInstance().AddJob(job);

        var preset = PresetPropertyManager.GetInstance().GetPresetContainer(PresetEnum.Raid) as PresetPropertyContainer_Raid;
        preset.RemoveUnitGroup();

        GameManager.SetGameMode(GameStateEnum.StageHunting);
        SceneLoader.LoadScene(SceneEnum.InGame);
        //SetAutoState(StageAutoEnum.None);
    }
    #endregion

    #region getter
    public override List<Unit> GetEnemies()
    {
        var pool = MonsterPool.GetInstance();
        return pool.GetAllMonsters();
    }

    public override List<Unit> GetNearestAlly(Vector2 posision, int maxCount = 1)
    {

        if (unitGroup == null) return null;
        return unitGroup.GetNearestUnits(posision, maxCount);
    }


    public override List<Unit> GetNearestEnemy(Vector2 position, int maxCount)
    {
        return MonsterPool.GetInstance().GetNearestMonster(position, maxCount);
    }

    public override List<Unit> GetAllys()
    {
        List<Unit> units = new List<Unit>();

        foreach (var item in unitGroup.units)
        {
            units.Add(item);
        }

        return units;
    }

    public override void DestroyGameResources()
    {
        throw new NotImplementedException();
    }

    public RaidMonsterCSV GetCurrentMonsterCSV(StageBuffTargetEnum target)
    {
        switch (target)
        {
            case StageBuffTargetEnum.BossMonster:
                return null;
            case StageBuffTargetEnum.NormalMonster:
                int crrIndex = normalMobIndex - 1;
                if (crrIndex < 0) crrIndex = middleMonsters.Count-1;
                return middleMonsters[crrIndex];
            default:
                return null;
        }
    }
    #endregion
}
