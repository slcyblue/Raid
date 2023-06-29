using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class RaidUI : IGameStateUI
{
    public RectTransform mainPanel;
    [Header("RespawnCount")]
    [SerializeField] Text txtRemainRespawnCount;

    [Header("Timer")]
    [SerializeField] Text txtRemainTs;
    [SerializeField] GameObject timerObject = null;
    [SerializeField] Text txtNextWaveTs;

    [Header("AutoPlay")]
    [SerializeField] Animator goAutoAnim;
    [SerializeField] Text autoBtnTitle;

    [Header("BuffUI")]
    [SerializeField] RectTransform buffUIContainer;
    [SerializeField] Text txtStageName;
    //[SerializeField] StageBuffBox buffbox;
    ObjectPool<raidBuffItem> buffItems = new ObjectPool<raidBuffItem>();
    public RectTransform buffItemParent = null;
    [SerializeField] raidBuffItem raidbuffItem = null;

    [Header("Boss UI")]
    [SerializeField] GameObject bossHPBarObject;
    [SerializeField] Slider bossHPSlider;
    [SerializeField] Text txtBossHp;
    [SerializeField] Text txtBossName;
    [SerializeField] Slider bossSkillSlider;
    //[SerializeField] GameObject bossSkillPanel;
    //[SerializeField] GameObject bossSkillInfoButton;
    //[SerializeField] Text bossSkillDescription;
    ObjectPool<bossSkillIcon> skillIcons = new ObjectPool<bossSkillIcon>();
    public RectTransform skillIconParent = null;
    [SerializeField] bossSkillIcon bossSkillIcon = null;

    [SerializeField] GameObject activeMiddleBossObject;
    [SerializeField] Text activeMiddleBossSkill;



    [Header("MiddleBoss UI")]
    [SerializeField] GameObject middleBossHPBarObject;
    [SerializeField] Slider middleBossHPSlider;
    [SerializeField] Text txtMiddleBossHp;
    [SerializeField] Text txtMiddleBossName;
    [SerializeField] Slider middleBossSkillSlider;
    [SerializeField] bossSkillIcon middleBossSkillPanel = null;

    [Header("Effects")]
    [SerializeField] UIEffect_BossAppear bossAppearEff;
    [SerializeField] UIEffectObject startTimerEff;
    [SerializeField] TextMeshProUGUI startEff_stageName;
    [SerializeField] TextMeshProUGUI startEff_startText;
    [SerializeField] EffectObject clearEffect;

    Raid state;

    private void Update()
    {
        txtRemainTs.text = TimeSpan.FromSeconds(state.remainTs).ToString(@"mm\:ss");
        UpdateBossHp();
        UpdateBossSkill();
        UpdateMiddleBossHp();
        UpdateMiddleBossSkill();
    }


    #region Init
    public override void Initialize(GameState state)
    {
        base.Initialize(state);
        this.state = state as Raid;
        //bossSpeechMgr.Initialize();

        gameObject.SetActive(true);
        timerObject.SetActive(true);

        bossHPBarObject.SetActive(false);
        bossSkillSlider.gameObject.SetActive(false);
        middleBossHPBarObject.SetActive(false);
        middleBossSkillSlider.gameObject.SetActive(false);

        mainPanel.gameObject.SetActive(false);
        clearEffect.InActive();
        if (bossAppearEff != null) bossAppearEff.InActive();

        skillIcons.Init(bossSkillIcon, skillIconParent);
        buffItems.Init(raidbuffItem, buffItemParent);
        
        HideTimerEff();
        //UpdateGoAutoButton();
    }

    public void UpdateBossSkillIcon(RaidMonsterCSV bossCSV)
    {
        ReturnAllSkillIconSlots();

        int length = bossCSV.skillIds.Length;
        for (int i = 0; i < length; i++)
        {
            GetSkillIconSlot((slot) => {
                slot.InitItem(bossCSV.skillIds[length - i-1], (int)Raid.StageBuffTargetEnum.BossMonster, state);
                slot.gameObject.SetActive(true);
            });
        }
    }

    void ReturnAllSkillIconSlots()
    {
        skillIcons.ReturnAll();
    }

    void GetSkillIconSlot(Action<bossSkillIcon> callback)
    {
        skillIcons.CreateNewItem((obj) =>
        {
            callback?.Invoke(obj);
        });
    }

    public void UpdateBuff()
    {
        ReturnAllbuffItemSlots();

        int length = state.raidBuffs.Count;
        for (int i = 0; i < length; i++)
        {
            GetBuffItemSlot((slot) => {
                slot.InitItem(state.raidBuffs[i]);
                slot.gameObject.SetActive(true);
            });
        }



        //buffbox.Init();

        //var buffs = state.raidBuffs;
        //buffbox.UpdateBuffInfo(buffs);

        //var originSize = buffUIContainer.sizeDelta;
        //float height = buffbox.GetTotalHeigth() + 40;
        //buffUIContainer.sizeDelta = new Vector2(originSize.x, height);
    }

    void ReturnAllbuffItemSlots()
    {
        buffItems.ReturnAll();
    }

    void GetBuffItemSlot(Action<raidBuffItem> callback)
    {
        buffItems.CreateNewItem((obj) =>
        {
            callback?.Invoke(obj);
        });
    }
    #endregion

    #region UI
    public float ShowClearEffect()
    {
        if (clearEffect == null) return 0;
        //bossSkillPanel.SetActive(false);
        //bossSkillInfoButton.SetActive(false);
        timerObject.SetActive(false);
        clearEffect.Show();
        return clearEffect.GetTotalEffectTs();
    }

    public void HideBossAppearEffect()
    {
        if (bossAppearEff == null) return;
        bossAppearEff.InActive();
    }

    public void ShowBossAppearEffect(UnitGroup unit, string bossBody)
    {
        if (bossAppearEff == null) return;
        int max = unit.units?.Count ?? 0;

        List<string> unitBodies = new List<string>();
        for (int i = 0; i < max; i++)
        {
            try
            {
                var appear = unit.units[i].bodyMgr.GetAppearence();
                unitBodies.Add(appear.profileName);
            }
            catch (Exception) { }
        }

        bossAppearEff.SetUnit(unitBodies, bossBody);
        bossAppearEff.Show();
    }



    public void SetSideUIs(bool isOn)
    {
        mainPanel.gameObject.SetActive(isOn);
        if (isOn)
        {
            //UpdateGoAutoButton();

        }
    }
    public void UpdateGoAutoButton()
    {
        if (!goAutoAnim.gameObject.activeInHierarchy) return;
        var state = StageChallenge.autoState;
        bool isOn = state != StageAutoEnum.None;
        string aniName = isOn ? "autoAni_active" : "autoAni_normal";
        goAutoAnim.Play(aniName);
    }
    public void SetAutoButtonText(string str)
    {
        autoBtnTitle.text = str;
    }
    public void UpdateRaidName(string name)
    {
        txtStageName.text = name;
    }
    public void UpdateRespawnCount(int count)
    {
        txtRemainRespawnCount.text = string.Format("x{0}", count);
    }
    public void ShowTimerEff()
    {
        startEff_stageName.text = txtStageName.text;
        startEff_startText.text = MessageContainer.WORD_READY_BATTLE;
        startTimerEff.Show();
    }

    public void HideTimerEff()
    {
        startTimerEff.gameObject.SetActive(false);
    }

    public void SetMiddleBossRespawnTs(float middleBossRespawnTs)
    {
        txtNextWaveTs.text = $"중간보스 생성 남은시간 : {middleBossRespawnTs - state.middleBossRemainTs}";
    }

    public void SetBossSkillEffect(int skillId)
    {
        int length = skillIcons.ActiveObjectCount;
        for (int i = 0; i < length; i++)
        {
            var item = skillIcons.GetItem(i);
            if (skillId == item.skillId) item.SetSkillEffect(true);
            else item.SetSkillEffect(false);
        }
    }

    public void ShowMiddleBossSkill(int skillId)
    {        
        var csv = SkillCSVContainer.GetInstance().GetSkillCSV(skillId);
        string description = PropertyDescriptionCreater.GetSkillDescription(csv);

        StartCoroutine(ActiveBossAlert(description));
    }

    IEnumerator ActiveBossAlert(string description)
    {
        activeMiddleBossObject.SetActive(true);
        activeMiddleBossSkill.text = $"중간보스 스킬 발동 \n {description}";
        yield return new WaitForSeconds(1f);

        activeMiddleBossObject.SetActive(false);
    }
    #endregion

    #region BossHp
    MonsterUnit targetBoss;
    MonsterUnit targetMiddleBoss;
    float targetBossRatio;
    float calculateHpTs;
    float targetMiddleBossRatio;
    float calculateMiddleBossHpTs;
    void UpdateBossHp()
    {
        if (targetBoss == null) return;

        if (targetBoss.IsDie())
        {
            bossHPSlider.value = 0f;
            return;
        }

        calculateHpTs += Time.deltaTime;
        if (calculateHpTs > 0.5f)
        {
            calculateHpTs = 0f;
            SetTargetBossRatio();
        }

        RefreshBossHpSlider();
    }

    void UpdateMiddleBossHp()
    {
        if (targetMiddleBoss == null) return;

        if (targetMiddleBoss.IsDie())
        {
            middleBossHPSlider.value = 0f;
            DisConnnectMiddleBossHp();
            return;
        }

        calculateMiddleBossHpTs += Time.deltaTime;
        if (calculateMiddleBossHpTs > 0.5f)
        {
            calculateMiddleBossHpTs = 0f;
            SetTargetMiddleBossRatio();
        }

        RefreshMiddleBossHpSlider();
    }

    void UpdateBossSkill()
    {
        if (targetBoss == null) return;
        var skillMgr = targetBoss.skillMgr;
        float max = skillMgr.GetMaxSkillCool(0);

        float targetRatio = 0f;
        if (max != 0)
        {
            float cool = skillMgr.GetCurrentSkillCool(0);
            targetRatio = 1 - (cool / max);
        }

        bossSkillSlider.value = targetRatio;

        if (targetRatio > bossSkillSlider.value) bossSkillSlider.value += Time.deltaTime * 10f;
        else bossSkillSlider.value -= Time.deltaTime * 10f;
    }

    void UpdateMiddleBossSkill()
    {
        if (targetMiddleBoss == null) return;
        var csv = RaidCSVContainer.GetInstance().GetRaidCSV(Raid.raidId, Raid.difficulty);
        float max = csv.middleBossActiveTs;

        float targetRatio = 0f;
        if (max != 0)
        {
            float cool = state.middleBossActiveTs;
            targetRatio = (cool / max);
            if (targetRatio >= 1) targetRatio = 1;
        }

        middleBossSkillSlider.value = targetRatio;

        //if (targetRatio > middleBossSkillSlider.value) middleBossSkillSlider.value += Time.deltaTime * 10f;
        //else middleBossSkillSlider.value -= Time.deltaTime * 10f;
    }

    void RefreshBossHpSlider()
    {
        float gap = bossHPSlider.value - targetBossRatio;
        float productVal = gap > 0 ? -0.1f : 0.1f;
        gap = Mathf.Abs(gap);
        if (gap < 0.01f) return;

        if (bossHPSlider.value < targetBossRatio || gap > 0.7)
        {
            bossHPSlider.value = targetBossRatio;
            return;
        }

        float speed = Time.deltaTime * 2f;
        if (gap > 0.4) speed *= 4;

        bossHPSlider.value += speed * productVal;
    }

    void RefreshMiddleBossHpSlider()
    {
        float gap = middleBossHPSlider.value - targetMiddleBossRatio;
        float productVal = gap > 0 ? -0.1f : 0.1f;
        gap = Mathf.Abs(gap);
        if (gap < 0.01f) return;

        if (middleBossHPSlider.value < targetMiddleBossRatio || gap > 0.7)
        {
            middleBossHPSlider.value = targetMiddleBossRatio;
            return;
        }

        float speed = Time.deltaTime * 2f;
        if (gap > 0.4) speed *= 4;

        middleBossHPSlider.value += speed * productVal;
    }

    public void UpdateBossName(string bossName)
    {
        txtBossName.text = bossName;
    }

    public void UpdateMiddleBossName(string bossName)
    {
        txtMiddleBossName.text = bossName;
    }

    public void ConnectBoss(Unit boss, int skillId)
    {
        if (boss == null) return;
        bossHPBarObject.SetActive(true);
        bossSkillSlider.gameObject.SetActive(true);
        bossSkillSlider.value = 0f;

        targetBoss = boss as MonsterUnit;

        bossHPSlider.value = 1f;
        SetTargetBossRatio();
        InitSkillPanel(skillId);
    }

    public void ConnectMiddleBoss(Unit middleBoss, int skillId)
    {
        if (middleBoss == null) return;
        middleBossHPBarObject.SetActive(true);
        middleBossSkillSlider.gameObject.SetActive(true);
        middleBossSkillSlider.value = 0f;

        targetMiddleBoss = middleBoss as MonsterUnit;

        middleBossHPSlider.value = 1f;
        SetTargetMiddleBossRatio();
        SetTargetMiddleBossSkill(skillId);
    }



    void SetTargetMiddleBossSkill(int skillId)
    {
        middleBossSkillPanel.InitItem(skillId, (int)Raid.StageBuffTargetEnum.NormalMonster, state);
        middleBossSkillPanel.gameObject.SetActive(true);
    }

    void InitSkillPanel(int skillId)
    {
        //bossSkillPanel.SetActive(false);
        SkillCSV csv = null;
        if (skillId != 0) csv = SkillCSVContainer.GetInstance().GetSkillCSV(skillId);
        //bossSkillInfoButton.SetActive(csv != null);
        if (csv != null)
        {
            //bossSkillDescription.text = PropertyDescriptionCreater.GetSkillDescription(csv);
        }
    }

    [SkipRename]
    public void OnClickBossInfoButton()
    {
        //bossSkillPanel.SetActive(!bossSkillPanel.activeSelf);
    }

    void SetTargetBossRatio()
    {
        float ratio = 0f;
        float MAXHP = 0;
        float crrHp = 0;

        if (!targetBoss.IsDie())
        {
            crrHp = targetBoss.stat.crrHp;
            MAXHP = targetBoss.stat?.Get(StatEnum.Hp) ?? 0;
            if (MAXHP != 0) ratio = crrHp / targetBoss.stat.Get(StatEnum.Hp);
        }
        txtBossHp.text = string.Format("{0:0}/{1:0}", crrHp, MAXHP);

        targetBossRatio = ratio;

    }

    void SetTargetMiddleBossRatio()
    {
        float ratio = 0f;
        float MAXHP = 0;
        float crrHp = 0;

        if (!targetMiddleBoss.IsDie())
        {
            crrHp = targetMiddleBoss.stat.crrHp;
            MAXHP = targetMiddleBoss.stat?.Get(StatEnum.Hp) ?? 0;
            if (MAXHP != 0) ratio = crrHp / targetMiddleBoss.stat.Get(StatEnum.Hp);
        }
        txtMiddleBossHp.text = string.Format("{0:0}/{1:0}", crrHp, MAXHP);

        targetMiddleBossRatio = ratio;

    }

    public void DisConnnectBossHp()
    {
        bossHPBarObject.SetActive(false);
        bossSkillSlider.gameObject.SetActive(false);
        targetBoss = null;
    }

    public void DisConnnectMiddleBossHp()
    {
        middleBossHPBarObject.SetActive(false);
        middleBossSkillSlider.gameObject.SetActive(false);
        targetMiddleBoss = null;
        middleBossSkillPanel.gameObject.SetActive(false);
    }

    public void ShowBossSpeech(MonsterSpeechCSV csv, MonsterSpeechEnum type)
    {
        if (csv == null) return;
        var iconSpr = AssetManager.GetUnitProfile(csv.iconName);
        string speech = csv.GetSpeech(type);
        //bossSpeechMgr.CreateSpeech(iconSpr, speech);
    }
    #endregion

    #region Actions

    [SkipRename]
    public void ClickGiveUp()
    {
        PopupManager.GetInstance().OpenPopup(PopupEnum.ConfirmPopup, (obj) =>
        {
            if (!(obj is ConfirmPopup confirm)) return;

            confirm.SetTitle(MessageContainer.QUESTION_GIVEUP);
            confirm.SetOKButtonCallback(MessageContainer.WORD_OK, () => state.QuitGame());
            confirm.SetCancelButtonCallback(MessageContainer.WORD_CANCEL, null);
        });
    }


    //[SkipRename]
    //public void ClickGoAutoButton()
    //{
    //    if (state == null) return;
    //    state.OnClickAutoButton();
    //}
    #endregion


}
