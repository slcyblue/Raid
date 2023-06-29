using LitJson;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DatabaseContents_Raid : IDatabase
{
    Dictionary<int, List<RaidData>> recordByRaidId = new Dictionary<int, List<RaidData>>();
    RaidCSVContainer csvcontainer;
    #region Init
    public override void Initialize()
    {
        base.Initialize();
        csvcontainer = RaidCSVContainer.GetInstance();
    }
    public override JsonData InsertDataToJson(JsonData origin)
    {
        List<RaidData> raidDatas = new List<RaidData>();
        foreach (var list in recordByRaidId)
        {
            foreach(var data in list.Value)
            {
                raidDatas.Add(data);
            }
        }
        
        return LocalParser_Raid.InsertToLocal(origin, raidDatas);
    }

    public void InitRaidData(List<RaidData> list)
    {
        if (list == null) return;
        int length = list.Count;
        for (int i = 0; i < length; i++)
        {
            int raidId = list[i].raidId;
            if (!recordByRaidId.ContainsKey(raidId)) recordByRaidId.Add(raidId, new List<RaidData>());
            recordByRaidId[raidId].Add(list[i]);
        }
    }
    #endregion


    public void SetIndate(string indate)
    {
        foreach (var dict in recordByRaidId)
        {
            foreach(var item in dict.Value)
            {
                item.SetIndate(indate);
            }
        }
    }


    public StatusCode TryEnter(int raidId, int difficulty)
    {
        var assets = csvcontainer.GetRaidEnterPrice(raidId, difficulty);

        if (assets.Count > 0)
        {
            bool isSuccess = PlayerStorage.GetInstance().Use(assets);
            if (!isSuccess) return StatusCode.Client_NotEnoughMoney;
        }

        var clearData = GetRaidRecord(difficulty, raidId);
        if (clearData == null)
        {
            clearData = new RaidData(difficulty, raidId);

            if (!recordByRaidId.ContainsKey(raidId)) recordByRaidId.Add(raidId, new List<RaidData>());
            clearData.SetIndate(FindIndate(raidId));
            recordByRaidId[raidId].Add(clearData);

        }

        int dailyLimitCount = csvcontainer.GetRaidDailyTryCount(raidId);
        if (GetDailyTryCount_Raid(raidId) >= dailyLimitCount) return StatusCode.Client_ExceedDailyLimit;

        clearData.dailyTryCount++;

        BackupManager.GetInstance().AddToBackupList(clearData);
        return StatusCode.Success;
    }

    public List<RaidData> GetClearRecordsByRaidId(int raidId)
    {
        recordByRaidId.TryGetValue(raidId, out var list);
        return list;
    }

    public int GetTryCount()
    {
        if (recordByRaidId == null || recordByRaidId.Count <= 0) return 0;
        int clearCount = 0;
        foreach(var list in recordByRaidId)
        {
            foreach(var item in list.Value)
            {
                clearCount += item.dailyTryCount;
            }
        }

        return clearCount;
    }

    public void ClearRaid(int raidId, int difficulty, float recordTs)
    {
        var clearData = GetRaidRecord(difficulty, raidId);
        if(clearData == null)
        {
            DebugX.LogWarning("ClearData not exist.");
            return;
        }
        
        //if (recordTs < clearData.secRecord) clearData.secRecord = Calculator.Roundf(recordTs);

        clearData.dailyClearCount++;
        BackupManager.GetInstance().AddToBackupList(clearData);
        BackupManager.GetInstance().LocalBackUp();
    }

    string FindIndate(int raidId)
    {
        if (!recordByRaidId.ContainsKey(raidId)) return "";
        if (recordByRaidId[raidId].Count == 0) return "";
       
        foreach(var item in recordByRaidId[raidId])
        {
            string indate = item.GetIndate();
            if (string.IsNullOrEmpty(indate)) continue;
            return indate;
        }

        return "";
    }

    public override void OnMidnight()
    {
        var backup = BackupManager.GetInstance();
        foreach(var item in recordByRaidId)
        {
            int length = item.Value.Count;
            for (int i = 0; i < length; i++)
            {
                var record = recordByRaidId[item.Key][i];
                if (record.dailyTryCount == 0) continue;
                record.ResetDailyCount();
                backup.AddToBackupList(record);
            }
        }
    }

    #region Getter
    RaidData GetRaidRecord(int difficulty, int raidId)
    {
        if (!recordByRaidId.ContainsKey(raidId)) return null;
        var raidRecords = recordByRaidId[raidId];
        foreach(var item in raidRecords)
        {
            if (!(item is RaidData raid)) continue;
            if (raid.difficulty != difficulty) continue;
            if (raid.raidId != raidId) continue;

            return raid;
        }
        return null;
    }

    public Dictionary<int, List<RaidData>> GetRecords()
    {
        return recordByRaidId;
    }

    
    public int GetMaxClearDifficulty(int raidId)
    {
        List<RaidData> datas = new List<RaidData>();
        recordByRaidId.TryGetValue(raidId, out datas);

        if (datas == null || datas.Count == 0) return 0;
        datas = datas.OrderBy(x => x.difficulty).ToList();

        int length = datas.Count;
        if (length > 0) return datas[length - 1].difficulty;
        
        return 0;
    }

    public int GetDailyTryCount_Raid(int raidId)
    {
        int sum = 0;
        foreach(var raidList in recordByRaidId)
        {
            var list = raidList.Value;
            foreach(var item in list)
            {
                if (item.raidId != raidId) continue;
                sum += item.dailyTryCount;
            }
        }

        return sum;
    }

    public bool ChangeDailyTryCount(int raidId, int difficulty, int dailyCnt)
    {
        try
        {
            foreach (var raidList in recordByRaidId)
            {
                var list = raidList.Value;
                foreach (var item in list)
                {
                    if (item.raidId != raidId) continue;
                    if (item.difficulty == difficulty)
                    {
                        item.dailyTryCount = dailyCnt;
                        return true;
                    }
                }
            }
            return false;
        }
        catch(Exception e)
        {
            DebugX.LogWarning(e.Message);
            return false;
        }
    }

    #endregion
}
