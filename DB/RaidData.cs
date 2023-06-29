using CodeStage.AntiCheat.ObscuredTypes;
using System;

public class RaidData : IBackendData
{
    public ObscuredInt raidId;
    //public ObscuredFloat secRecord;
    public ObscuredInt difficulty;
    public ObscuredInt dailyTryCount;
    public ObscuredInt dailyClearCount;

    public RaidData() { }
    public RaidData(int diff, int raidId, int dailyTryCount = 0, int dailyClearCount = 0)
    {
        difficulty = diff;

        //if (secRecord != 0) secRecord = Calculator.Roundf(secRecord);
        //this.secRecord = secRecord;
        this.raidId = raidId;
        this.dailyClearCount = dailyClearCount;
        this.dailyTryCount = dailyTryCount;
    }
    
    public override bool IsCopiedData(IBackendData data)
    {
        if (!IsSameTypeData(data)) return false;
        if (!(data is RaidData raid)) return false;
        //if (secRecord != raid.secRecord) return false;
        if (dailyTryCount != raid.dailyTryCount) return false;
        if (dailyClearCount != raid.dailyClearCount) return false;
        return true;
    }

    public override bool IsSameTypeData(IBackendData data)
    {
        if (!(data is RaidData raid)) return false;
        if (raidId != raid.raidId) return false;
        if (difficulty != raid.difficulty) return false;

        return true;
    }

    public override void RandomizeKey()
    {
        throw new NotImplementedException();
    }

    public void ResetDailyCount()
    {
        dailyTryCount = 0;
        dailyClearCount = 0;
    }
}


