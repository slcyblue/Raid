using Beebyte.Obfuscator;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaidPopup : IGameStatePopup
{
    Raid state;
    [Header("Success Panel")]
    [SerializeField] ItemSlot rewardPrefab;
    [SerializeField] RectTransform rewardParent;
    [SerializeField] Text txtNextButton;
    [SerializeField] Text nextAutoText;
    //   [SerializeField] GameObject nextDiffButton;
    [SerializeField] ButtonObject nextButton;
    [SerializeField] float minRewardBoxSize;
    [SerializeField] ScrollRect rewardRect;
    [SerializeField] ItemSlot userReousrce_successPanel;
    [SerializeField] Image imgNextPrice;
    [SerializeField] Text txtNextPrice;


    [Header("Fail Panel")]
    [SerializeField] GameObject failPanel = null;
    [SerializeField] GameObject successPanel = null;
    [SerializeField] ItemSlot userReousrce_failPanel;
    [SerializeField] Image imgRetryPrice;
    [SerializeField] Text txtRetryPrice;

    #region Init
    public override void Initialize(GameState state)
    {
        base.Initialize();
        this.state = state as Raid;
        rewardPrefab.gameObject.SetActive(false);
        gameObject.SetActive(false);
        failPanel.SetActive(false);
        successPanel.SetActive(false);
    }

    public void OpenFailPopup(Asset retryAsset)
    {
        gameObject.SetActive(true);
        failPanel.SetActive(true);
        //txtNextButton.text = "";
        //nextAutoText.text = "";

        LoadUserResource(userReousrce_failPanel, retryAsset);
        imgRetryPrice.sprite = AssetManager.GetProfile(retryAsset.type, retryAsset.id);
        txtRetryPrice.text = string.Format("x{0}", retryAsset.count);
    }

    #endregion

    #region Success
    public void OpenSuccessPopup(List<Asset> rewards)
    {
        SoundManager.GetInstance().PlaySFX(SoundKey.ui_clear_content);
        gameObject.SetActive(true);
        successPanel.SetActive(true);
        LoadReward(rewards);
       
        nextAutoText.text = "";
    }

    public void HideNextStage()
    {
        nextButton.gameObject.SetActive(false);
        userReousrce_successPanel.gameObject.SetActive(false);
    }

    public void OpenNextStage(Asset nextPrice)
    {
        nextButton.gameObject.SetActive(true);

        if (Raid.isAutoOn) StartCoroutine(IAutoWait());
        LoadUserResource(userReousrce_successPanel, nextPrice);

        imgNextPrice.sprite = AssetManager.GetProfile(nextPrice.type, nextPrice.id);
        txtNextPrice.text = string.Format("x{0}", nextPrice.count);
    }


    void LoadUserResource(ItemSlot slot, Asset asset)
    {
        if (slot == null)
        {
            return;
        }

        long userCnt = PlayerStorage.GetInstance().GetPayableCount(asset.type, asset.id);
        slot.Init(new Asset(asset.type, asset.id, userCnt));
        slot.ShowCount();

    }

    IEnumerator IAutoWait()
    {
        string msgFormat = "";
        Action func = null;
        
        if (Raid.isAutoOn)
        {
            msgFormat = MessageContainer.FORMAT_WAIT_RETRY;
            func = EnterCurrentStage;

            float time = 3;
            while (true)
            {
                nextAutoText.text = string.Format(msgFormat, time);
                yield return new WaitForSeconds(1f);
                time -= 1;
                if (time <= 0) break;
            }
        }

        txtNextButton.text = "";
        nextAutoText.text = "";

        func?.Invoke();
    }

    void LoadReward(List<Asset> rewards)
    {
        int rewardCount = rewards?.Count ?? 0;
        for (int i = 0; i < rewardCount; i++)
        {
            var item = Instantiate(rewardPrefab, rewardParent);
            item.Init(rewards[i]);
            // item.ShowAssetName();
        }

        float size = rewardParent.sizeDelta.x;

        if (size < minRewardBoxSize)
        {
            rewardRect.movementType = ScrollRect.MovementType.Clamped;
        }
        else
        {
            rewardRect.movementType = ScrollRect.MovementType.Elastic;
        }
        rewardParent.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region Actions

    [SkipRename]
    public void ClickWorldMap()
    {
        StopAllCoroutines();
        PopupManager.GetInstance().OpenPopup(PopupEnum.RaidEntrancePopup, (obj) =>
        {
            if (!(obj is RaidEntrancePopup popup)) return;

            int raidId = Raid.raidId;
            int diff = Raid.difficulty;


            //popup.SetStage(raidId);
        });
    }

    [SkipRename]
    public void ReTry()
    {
        bool needToQuit = SessionManager.CheckGameQuit(false);
        if (needToQuit) return;

        SoundManager.GetInstance().PlayClickSound();
        EnterCurrentStage();
    }

    [SkipRename]
    public void GoNext()
    {
        SoundManager.GetInstance().PlayClickSound();
        bool needToQuit = SessionManager.CheckGameQuit(false);
        if (needToQuit) return;

        EnterNextStage();
    }

    void EnterCurrentStage()
    {
        StopAllCoroutines();
        int raidId = Raid.raidId;
        int diff = Raid.difficulty;

        TryEnter(raidId, diff);
    }
    void EnterNextStage()
    {
        StopAllCoroutines();

        int raidId = Raid.raidId;
        int diff = StageChallenge.difficulty + 1;
        TryEnter(raidId, diff);
    }
    void TryEnter(int raidId, int diff)
    {
        SoundManager.GetInstance().PlayClickSound();
        var status = DB.Get<DatabaseContents_Raid>().TryEnter(Raid.raidId, diff);
        if (status == StatusCode.Success)
        {
            Raid.SetGame(raidId, diff);
            SceneLoader.LoadScene(SceneEnum.InGame);
        }
        else if (status == StatusCode.Client_NotEnoughMoney)
        {
            OpenShopGuidePopup(raidId, diff);
        }
        else
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(status);
        }
    }

    void OpenShopGuidePopup(int raidId, int diffId)
    {
        var container = StageCSVContainer.GetInstance();
        var list = container.GetChallengeEnterPrice(raidId, diffId);
        if (list == null) return;

        var storage = PlayerStorage.GetInstance();

        foreach (var item in list)
        {
            long ownCnt = storage.GetPayableCount(item.type, item.id);
            if (ownCnt < item.count)
            {
                PopupManager.GetInstance().OpenPopup(PopupEnum.ShopGuidePopup, (obj) =>
                {
                    if (!(obj is ShopGuidePopup popup)) return;
                    popup.OpenGuidePopup(item, GoGacha, GoShop, GoRaid);
                });

                return;
            }
        }

    }

    void GoGacha(int machine)
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.GachaPopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_GoGacha(machine));
    }
    [SkipRename]
    public void GoShop(int shopId)
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.ShopPopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_GoShop(shopId));
    }

    [SkipRename]
    public void GoGacha()
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.GachaPopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_OpenPopup(PopupEnum.GachaPopup));
    }
    [SkipRename]
    public void GoStat()
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.StatUpgradePopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_OpenPopup(PopupEnum.StatUpgradePopup));
    }

    [SkipRename]
    public void GoRune()
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.RunePopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_OpenPopup(PopupEnum.RunePopup));
    }

    [SkipRename]
    public void GoEquip()
    {
        if (!PopupManager.GetInstance().CheckOwnFurniture(PopupEnum.EquipPopup))
        {
            PostAlarmManager.GetInstance().CreatePostAlarm(MessageContainer.MSG_OPEN_FURNITURE);
            return;
        }
        state.QuitGame(new SideJob_OpenPopup(PopupEnum.EquipPopup));
    }

    void GoRaid(int raidId)
    {
        state.QuitGame(new SideJob_GoRaidEntrance(raidId));
    }


    [SkipRename]
    public void Back()
    {
        SoundManager.GetInstance().PlayClickSound();
        state.QuitGame(new SideJob_GoRaidEntrance(Raid.raidId));
    }
    #endregion
}