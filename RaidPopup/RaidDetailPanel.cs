using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class RaidDetailPanel : MonoBehaviour
{
    [SerializeField] Text txtRaidName;
    [SerializeField] Text txtRaidDifficulty;

    [Header("Price")]
    [SerializeField] List<ItemSlot> userResources;
    [SerializeField] RectTransform ticketParent;
    [SerializeField] ItemSlot ticketPrefab;
    List<ItemSlot> ticketSlots = new List<ItemSlot>();

    //[Header("Category")]
    //[SerializeField] CategorySlot catePrefab;
    //[SerializeField] RectTransform cateParent;
    //List<CategorySlot> attributeButtons = new List<CategorySlot>();
    //string[] cateNames = new string[] { "전체", "동굴", "미래", "초원", "화염", "파도" };

    [Header("Weapons")]
    [SerializeField] RectTransform selectMark;
    [SerializeField] EquipSlot equipPrefab;
    
    public RectTransform equipItemParent = null;
    [SerializeField] private int colCount = 5;
    ObjectPool<EquipSlot> items = new ObjectPool<EquipSlot>();

    public float DEFAULT_X;
    public float DEFAULT_Y;
    public float WIDTH;
    public float HEIGHT;

    [Header("UnitSlot")]
    [SerializeField] EquipSlot_Raid slotPrefab;
    [SerializeField] Text txtSlotCount;

    public RectTransform slotListParent = null;

    ObjectPool<EquipSlot_Raid> slotList = new ObjectPool<EquipSlot_Raid>();

    RaidEntrancePopup mgr;
    WeaponCSVContainer weaponCsvContainer = null;
    DatabaseWeapon weapDB;

    EquipSlot crrFocusWeapon = null;
    EquipSlot_Raid crrFocusSlot = null;

    Preset raidPreset = null;

    int unitListCount = 0;

    public void Init()
    {
        ticketPrefab.gameObject.SetActive(false);
        weaponCsvContainer = WeaponCSVContainer.GetInstance();
        weapDB = DB.Get<DatabaseWeapon>();

        items.Init(equipPrefab, equipItemParent);
        slotList.Init(slotPrefab, slotListParent);

        

        gameObject.SetActive(false);
        //catePrefab.gameObject.SetActive(false);
    }

    public void Open(RaidEntrancePopup mgr)
    {
        this.mgr = mgr;
        var assets = mgr.csvcontainer.GetRaidEnterPrice(mgr.raidId, mgr.currentDifficulty);

        txtRaidName.text = mgr.csvcontainer.GetRaidName(mgr.raidId);
        txtRaidDifficulty.text = mgr.cateNames[mgr.currentDifficulty -1];

        UpdateEquipedSlot();
        UpdateEnterPrice(assets);
        UpdateUserResources(assets);
        //UpdateCategories();
        LoadWeapons(0);

        gameObject.SetActive(true);
    }

    #region EquipSlot
    void UpdateEquipedSlot()
    {
        raidPreset = DB.Get<DatabaseEquipment>().GetPreset((int)PresetEnum.Raid);

        unitListCount = 0;
        slotList.ReturnAll();
        
        for (int i = 1; i <= mgr.csv.totalSlotCount; i++)
        {
            GetItemSlot(slotList, (slot) =>
            {
                bool isInitialized = false;
                if (raidPreset != null && raidPreset.equips.Count > 0)
                {
                    foreach (var item in raidPreset.equips)
                    {
                        if (item.slotId == i && item.equipKey != 0)
                        {
                            Weapon weapon = DB.Get<DatabaseWeapon>().Get(item.equipKey);
                            slot.Init(new Asset((int)AssetEnum.Weapon, weapon.weapId, 1, weapon.grade));
                            isInitialized = true;
                            unitListCount++;
                        }
                    }
                }

                if(slot.slotId == 0) slot.SetSlotId(i);
                
                if (!isInitialized)
                {
                    slot.gameObject.SetActive(true);
                    slot.Init(new Asset((int)AssetEnum.Weapon, 0));
                    slot.SetLockImage(true);
                }

                slot.AddClickListener(OnClickEquipedSlot);
                
                if (crrFocusSlot == null) slot.Click();
                else if (crrFocusSlot.slotId == i) slot.Click();
            });   
        }

        txtSlotCount.text = $"유닛배치 {unitListCount}/{mgr.csv.totalUnitCount}";
    }

    void OnClickEquipedSlot(EquipSlot_Raid slot)
    {
        if(crrFocusSlot != null) crrFocusSlot.SetFocus(false);
        crrFocusSlot = slot;
        slot.SetFocus(true);
    }
    #endregion


    #region Category
    //void UpdateCategories()
    //{
    //    int count = Enum.GetValues(typeof(AttributeEnum)).Length;
    //    int index = 0;
    //    for (; index < count; index++)
    //    {
    //        if (attributeButtons.Count <= index) CreateCategoryObject();
    //        attributeButtons[index].Init(index, SortWeapons);
    //        attributeButtons[index].SetText(cateNames[index]);
    //        attributeButtons[index].SetBackground(mgr.unselectedSprite);
    //    }

    //    int length = attributeButtons.Count;
    //    for (; index < length; index++)
    //    {
    //        attributeButtons[index].gameObject.SetActive(false);
    //    }
    //}

    //void CreateCategoryObject()
    //{
    //    var newItem = Instantiate(catePrefab, cateParent);
    //    attributeButtons.Add(newItem);
    //}

    //void ChangeCategories(int index)
    //{
    //    if (index < 0) index = 0;
    //    else if (attributeButtons.Count <= index) index = attributeButtons.Count - 1;

    //    attributeButtons[crrSortIndex - 1].SetBackground(mgr.unselectedSprite);
    //    crrSortIndex = index + 1;
    //    attributeButtons[index].SetBackground(mgr.selectedSprite);
    //}
    #endregion

    #region LoadWeapons
    void LoadWeapons(int index)
    {
        //ChangeCategories(index);

        items.ReturnAll();

        var ids = weaponCsvContainer.weaponGroups;
        if (ids == null | ids.Count == 0) return;

        float X = DEFAULT_X;
        float Y = DEFAULT_Y;
        int itemCount = 1;

        foreach (var item in ids)
        {
            int currentCount = itemCount;
            GetItemSlot(items, (slot) => {
                LoadWeaponSlot(slot, item.Value);
                
                slot.SetPosition(X, Y);

                if (currentCount % colCount == 0)
                {
                    X = DEFAULT_X;
                    Y -= HEIGHT;
                }
                else
                {
                    X += WIDTH;
                }

                if (currentCount == 1) slot.Click();
            });

            itemCount++;
        }

        float rowCnt = (itemCount / colCount) + 1;
        float height = rowCnt * HEIGHT;
        equipItemParent.sizeDelta = new Vector3(0, height, 0);
    }

    void GetItemSlot(ObjectPool<EquipSlot> pool, Action<EquipSlot> callback)
    {
        pool.CreateNewItem((obj) =>
        {
            Vector2 ancher = new Vector2(0.5f, 1f);
            obj.SetAncher(ancher, ancher);
            callback?.Invoke(obj);
        });
    }

    void GetItemSlot(ObjectPool<EquipSlot_Raid> pool, Action<EquipSlot_Raid> callback)
    {
        pool.CreateNewItem((obj) =>
        {
            Vector2 ancher = new Vector2(0.5f, 1f);
            obj.SetAncher(ancher, ancher);
            callback?.Invoke(obj);
        });
    }

    void LoadWeaponSlot(EquipSlot slot, WeaponGroupCSV weapGroup)
    {
        if (slot == null || weapGroup == null) return;
        var userweap = FindUserMaxGradeWeap(weapGroup.groupId);

        int grade = 1;
        int level = 1;
        int weaponId = 0;
        if (userweap != null)
        {
            grade = userweap.grade;
            level = userweap.level;
            weaponId = userweap.weapId;
        }
        else
        {
            var weapCSV = weaponCsvContainer.GetMaxClassWeaponCSV(weapGroup.groupId);
            if (weapCSV == null)
            {
                DebugX.LogWarning("weapCSV is null" + weapGroup.groupId);
            }
            weaponId = weapCSV.id;
        }

        slot.Init(new Asset(AssetEnum.Weapon, weaponId, 1, grade), OnClickItem);
        slot.SetLockImage(userweap == null);
    }

    Weapon FindUserMaxGradeWeap(int weapGroupId)
    {
        var list = weaponCsvContainer.GetWeaponCSVWithGroup(weapGroupId);
        if (list == null) return null;

        int currentClassId = 0;
        Weapon resultWeap = null;
        foreach (var item in list)
        {
            if (currentClassId > item.weaponClassId) continue;
            var userWewap = weapDB.Get(item.id);
            if (userWewap == null) continue;

            resultWeap = userWewap;
        }

        return resultWeap;
    }

    void OnClickItem(EquipSlot slot)
    {
        crrFocusWeapon = slot;
        ShowMarkAt(slot.GetPos());
    }

    public void ShowMarkAt(Vector2 pos)
    {
        selectMark.anchoredPosition = pos;
        selectMark.gameObject.SetActive(true);
    }
    #endregion

    #region Resources
    void UpdateEnterPrice(List<Asset> assets)
    {
        int length = assets?.Count ?? 0;
        int maxCount = ticketSlots.Count;
        for (int index = 0; index < length; index++)
        {
            if (maxCount <= index)
            {
                ticketSlots.Add(Instantiate(ticketPrefab, ticketParent));
            }

            ticketSlots[index].Init(assets[index]);
        }

        for (int index = length; index < maxCount; index++)
        {
            ticketSlots[index].gameObject.SetActive(false);
        }
       
    }

    void UpdateUserResources(List<Asset> assets)
    {
        int length = assets?.Count ?? 0;
        int maxCount = userResources.Count;

        var storage = PlayerStorage.GetInstance();
        for (int index = 0; index < length; index++)
        {
            if (maxCount <= index) break;
            var crr = assets[index];
            long count = storage.GetCount(crr.type,crr.id);
            crr.count = count;
            userResources[index].Init(crr);
            userResources[index].ShowCount();
        }
        for (int i = length; i < userResources.Count; i++)
        {

            userResources[i].gameObject.SetActive(false);
        }
    }
    #endregion

    #region Actions
    [SkipRename]
    public void EquipAtSlot()
    {
        if (crrFocusWeapon.isLocked)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm("잠긴 무기입니다.");
            return;
        }

        PopupManager.GetInstance().OpenPopup(PopupEnum.WeaponClassPopup, callback => {
            if (callback is WeaponClassPopup popup) popup.SetEquips(weaponCsvContainer.GetGroupCSVOfWeapon(crrFocusWeapon.itemId).groupId, AddList);
        });
    }

    void AddList(EquipSlot slot)
    {
        int length = slotList.ActiveObjectCount;
        for (int i = 0; i < length; i++)
        {
            var slotItem = slotList.GetItem(i);
            if (slotItem.itemId == 0) continue;

            if (weaponCsvContainer.GetGroupCSVOfWeapon(slotItem.itemId).groupId == weaponCsvContainer.GetGroupCSVOfWeapon(crrFocusWeapon.itemId).groupId)
            {
                DB.Get<DatabaseEquipment>().RemoveAt(PresetEnum.Raid, AssetEnum.Weapon, slotItem.slotId, slotItem.itemId);
                unitListCount--;
            }
        }

        if (unitListCount == mgr.csv.totalUnitCount && crrFocusSlot.itemId != 0)
        {
            DB.Get<DatabaseEquipment>().RemoveAt(PresetEnum.Raid, AssetEnum.Weapon, crrFocusSlot.slotId, crrFocusSlot.itemId);
            unitListCount--;
        }

        if (unitListCount >= mgr.csv.totalUnitCount)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm("출전 인원이 가득 찼습니다.");
            return;
        }

        DB.Get<DatabaseEquipment>().EquipAt(PresetEnum.Raid, 1, crrFocusSlot.slotId, slot.itemId);
        BackupManager.GetInstance().ShortenLocalTimer();

        UpdateEquipedSlot();
    }

    [SkipRename]
    public void UnEquipedSlot()
    {
        DB.Get<DatabaseEquipment>().RemoveAt(PresetEnum.Raid, AssetEnum.Weapon, crrFocusSlot.slotId, crrFocusSlot.itemId);
        BackupManager.GetInstance().ShortenLocalTimer();

        UpdateEquipedSlot();
    }

    [SkipRename]
    public void UnEquipedAllSlot()
    {
        int length = mgr.csv.totalSlotCount;
        for (int i = 0; i < length; i++)
        {
            var slotItem = slotList.GetItem(i);
            if (slotItem.itemId == 0) continue;

            DB.Get<DatabaseEquipment>().RemoveAt(PresetEnum.Raid, AssetEnum.Weapon, i+1, slotItem.itemId);
            BackupManager.GetInstance().ShortenLocalTimer();

            UpdateEquipedSlot();
        }
    }

    [SkipRename]
    public void Close()
    {
        gameObject.SetActive(false);
    }

    [SkipRename]
    public void ClickEnter()
    {
        if (unitListCount < mgr.csv.totalUnitCount)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm("유닛을 더 배치해주세요");
            return;
        }

        if(unitListCount > mgr.csv.totalUnitCount)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm("유닛이 초과되었습니다.");
            return;
        }

        var status  = DB.Get<DatabaseContents_Raid>().TryEnter(mgr.raidId, mgr.currentDifficulty);

        if(status == StatusCode.Client_NotEnoughMoney)
        {
            PopupManager.GetInstance().OpenPopup(PopupEnum.ConfirmPopup, (obj) =>
            {
                if (!(obj is ConfirmPopup confirm)) return;
                confirm.SetTitle(MessageContainer.ERR_NOT_ENOUGH_MONEY + MessageContainer.QUESTION_GO_SHOP);
                confirm.SetCancelButtonCallback(MessageContainer.WORD_CANCEL);
                confirm.SetOKButtonCallback(MessageContainer.WORD_OK, OnClickGoShop);
            });

            return;
        }
        else if (status != StatusCode.Success)
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(status);
            return;
        }
        
        BackupManager.GetInstance().LocalBackUp();

        GameManager.SetGameMode(GameStateEnum.Raid);
        Raid.SetGame(mgr.raidId, mgr.currentDifficulty);

        SceneLoader.LoadScene(SceneEnum.InGame);
    }

    void OnClickGoShop()
    {
        PopupManager.GetInstance().OpenPopup(PopupEnum.ShopPopup, callback => {
            if (callback is ShopPopup popup)
            {
                popup.ChangeShopCategory(1);
            }
        });
    }

    #endregion
}
