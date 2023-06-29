using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RaidEntrancePopup : Popup
{

    public static int MAX_DIFFICULTY = 3;

    [SerializeField] RaidDetailPanel detailPanel;

    [Header("Info")]
    [SerializeField] Text txtEntranceCondition = null;
    [SerializeField] Image bossInfo = null;
    [SerializeField] Text bossHp = null;
    [SerializeField] Text bossAtk = null;
    ObjectPool<RaidMonsterItem> items = new ObjectPool<RaidMonsterItem>();
    public RectTransform itemParent = null;
    [SerializeField] RaidMonsterItem raidMonsterItem = null;
    [SerializeField] RaidMonsterInfoPanel monsterInfoPanel;
    [SerializeField] Text dailyEntranceCount = null;

    [SerializeField] Image entranceBtn = null;
    [SerializeField] Sprite availableSprite = null;
    [SerializeField] Sprite unavailableSprite = null;

    [Header("Difficulty")]
    [SerializeField] CategorySlot catePrefab;
    [SerializeField] RectTransform cateParent;
    List<CategorySlot> difficultyButtons = new List<CategorySlot>();
    public string[] cateNames = new string[] { "이지", "노말", "하드" };

    [Header("Rewards")]
    [SerializeField] RectTransform rewardParent;
    [SerializeField] ItemSlot rewardPrefab;
    List<ItemSlot> rewardList = new List<ItemSlot>();

    /*
    [Header("Buff")]
    [SerializeField] BuffInfoSlot buffPrefab;
    [SerializeField] RectTransform buffItemParent;
    List<BuffInfoSlot> buffSlots = new List<BuffInfoSlot>();
    */

    DatabaseContents_Raid raidDB;
    
    public RaidCSVContainer csvcontainer;
    //public SkillCSVContainer skillcontainer;
    public RaidDifficultyCSV csv;

    public int raidId { get; private set; }
    public int currentDifficulty { get; private set; } = 1;


    public Sprite unselectedSprite { get; private set; }
    public Sprite selectedSprite { get; private set; }

    #region Init
    public override void Init()
    {
        base.Init();

        this.raidId = 1;
        csvcontainer = RaidCSVContainer.GetInstance();
        detailPanel.Init();

        raidDB = DB.Get<DatabaseContents_Raid>();
        currentDifficulty = raidDB.GetMaxClearDifficulty(raidId);
        if (currentDifficulty >= 0) currentDifficulty = 1;

        csv = csvcontainer.GetRaidCSV(raidId, currentDifficulty);
        if (csv == null)
        {
            DebugX.LogWarning("Raid csv is null;");
            return;
        }

        unselectedSprite = AssetManager.GetETCSprite(ETCSpriteNameEnum.categoryUnSelectedSprite);
        selectedSprite = AssetManager.GetETCSprite(ETCSpriteNameEnum.categorySelectedSprite);

        catePrefab.gameObject.SetActive(false);
        rewardPrefab.gameObject.SetActive(false);
        raidMonsterItem.gameObject.SetActive(false);
        //buffPrefab.gameObject.SetActive(false);

        items.Init(raidMonsterItem, itemParent);

        detailPanel.gameObject.SetActive(false);
        monsterInfoPanel.gameObject.SetActive(false);
    }

    public override void Open()
    {
        base.Open();
        UpdateCategories();
        UpdateRewardList();
        ShowRaidInfo(currentDifficulty-1);
    }
    #endregion
    #region Monsters

    void UpdateCategories()
    {
        int index = 0;
        for (; index < MAX_DIFFICULTY; index++)
        {
            if (difficultyButtons.Count <= index) CreateCategoryObject();
            difficultyButtons[index].Init(index, ShowRaidInfo);
            difficultyButtons[index].SetText(cateNames[index]);
            difficultyButtons[index].SetBackground(unselectedSprite);
        }

        int length = difficultyButtons.Count;
        for (; index < length; index++)
        {
            difficultyButtons[index].gameObject.SetActive(false);
        }
    }

    void CreateCategoryObject()
    {
        var newItem = Instantiate(catePrefab, cateParent);
        difficultyButtons.Add(newItem);
    }

    void ChangeCategories(int index)
    {
        if (index < 0) index = 0;
        else if (difficultyButtons.Count <= index) index = difficultyButtons.Count - 1;

        difficultyButtons[currentDifficulty - 1].SetBackground(unselectedSprite);
        currentDifficulty = index + 1;
        difficultyButtons[index].SetBackground(selectedSprite);
    }

    void ShowRaidInfo(int index)
    {
        ChangeCategories(index);

        txtEntranceCondition.text = csvcontainer.GetRaidStory(raidId);
        int dailyCount = DB.Get<DatabaseContents_Raid>().GetDailyTryCount_Raid(raidId);
        int limitTryCount = csvcontainer.GetRaidDailyTryCount(raidId);

        if (dailyCount >= limitTryCount) entranceBtn.sprite = unavailableSprite;
        else entranceBtn.sprite = availableSprite;

        dailyEntranceCount.text = $"일일 도전 횟수 : {dailyCount}/{limitTryCount}";
        var monsterList = csvcontainer.GetMonsters(csv.raidId, currentDifficulty);
        if (monsterList == null || monsterList.Count <= 0) return;
        
        LoadMonsterInfo(monsterList);
        UpdateRewardList();
    }

    void LoadMonsterInfo(List<RaidMonsterCSV> list)
    {
        ReturnAllItemSlots();

        foreach(var item in list)
        {
            switch (item.monsterType)
            {
                case 1:
                    GetItemSlot((slot) => {
                        slot.InitItem(item, this);
                        slot.gameObject.SetActive(true);
                    });
                    break;
                case 2:
                    var bossBody = AssetManager.GetMonsterProfile(item.bodyName);
                    if (bossBody == null) 
                    {
                        bossInfo.gameObject.SetActive(false);
                        break;
                    }
                    bossInfo.sprite = bossBody;
                    bossHp.text = $"HP : {item.hp}";
                    bossAtk.text = $"ATK : {item.power}";
                    break;
                default:
                    DebugX.Log("Monster type is NULL");
                    break;
            }
        }
    }

    void ReturnAllItemSlots()
    {
        items.ReturnAll();
    }

    void GetItemSlot(Action<RaidMonsterItem> callback)
    {
        items.CreateNewItem((obj) =>
        {
            callback?.Invoke(obj);
        });
    }

    #endregion

    #region Reward List
    void UpdateRewardList()
    {
        var list = GetRewards();
        if (list == null || list.Count == 0)
        {
            HideRewardIcons();
            return;
        }
        int length = list.Count;
        for (int i = 0; i < length; i++)
        {
            if (rewardList.Count <= i) CreateRewardSlot();

            rewardList[i].Init(list[i]);
            rewardList[i].SetText("");
            rewardList[i].ShowAssetName();

        }

        HideRewardIcons(length);
    }

    void CreateRewardSlot()
    {
        var item = Instantiate(rewardPrefab, rewardParent);
        rewardList.Add(item);
    }

    List<Asset> GetRewards()
    {
        List<Asset> assets = new List<Asset>();
        csv = csvcontainer.GetRaidCSV(raidId, currentDifficulty);
        if (csv == null || csv.clearRewardAssetTypes == null) return null;

        int length = csv.clearRewardAssetIds.Length;
        for (int i = 0; i < length; i++)
        {
            int itemType = csv.clearRewardAssetTypes[i];
            int itemId = csv.clearRewardAssetIds[i];
            if (ExistInList(assets, itemType, itemId)) continue;
            assets.Add(new Asset(itemType, itemId, 0));
        }
        return assets;
    }


    bool ExistInList(List<Asset> assets, int type, int id)
    {
        int length = assets.Count;
        for (int i = 0; i < length; i++)
        {
            if (assets[i].type != type) continue;
            if (assets[i].id != id) continue;
            return true;
        }

        return false;
    }
    void HideRewardIcons(int startIndex = 0)
    {
        int length = rewardList.Count;
        for (int i = startIndex; i < length; i++)
        {
            rewardList[i].gameObject.SetActive(false);
        }
    }
    #endregion


    #region StageBuff

    /*
    void UpdateBuffInfo()
    {
        var csv = csvcontainer.GetRaidCSV(raidId, currentDifficulty);
        StringBuilder build = new StringBuilder();

        int length = csv?.stageBuffs?.Length ?? 0;
        int buffIndex = 0;
        for (int i = 0; i < length; i++)
        {
            int id = csv.stageBuffs[i];
            var buffCSV = csvcontainer.GetStageBuff(id);
            if (buffCSV == null) continue;
            if (buffSlots.Count <= buffIndex)
            {
                buffSlots.Add(Instantiate(buffPrefab, buffItemParent));
            }


            var slot = buffSlots[buffIndex];
            LoadBuff(buffCSV, slot);
            buffIndex++;
        }

        length = buffSlots.Count;
        for (; buffIndex < length; buffIndex++)
        {
            buffSlots[buffIndex].gameObject.SetActive(false);
        }
    }

    void LoadBuff(StageBuffCSV csv, BuffInfoSlot slot)
    {
        var buffeff = GameAssetCSVContainer.GetInstance().GetBuffEffectCSV(csv.upgradeType, csv.upgradeArgs);
        Sprite spr = null;
        if (buffeff != null && !string.IsNullOrEmpty(buffeff.effectIconName)) spr = AssetManager.GetBuffIcon(buffeff.effectIconName);

        string str = PropertyDescriptionCreater.GetDescription(csv, csv.description);

        slot.Init(str, spr);
    }
    */
    #endregion

    #region Actions
    [SkipRename]
    public void EntranceRaid() 
    {
        int dailyCount = DB.Get<DatabaseContents_Raid>().GetDailyTryCount_Raid(raidId);
        if (dailyCount >= csvcontainer.GetRaidDailyTryCount(raidId))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(StatusCode.Client_ExceedDailyLimit);
            return;
        }

        detailPanel.Open(this);
    }

    [SkipRename]
    public void ShowMiddleBossDetail()
    {
        monsterInfoPanel.Open();
    }

    public override void OnRedisplayTop()
    {
        base.OnRedisplayTop();
        detailPanel.Open(this);
    }
    #endregion
}
