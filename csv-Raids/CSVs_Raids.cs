using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static DataFormatConverter;

public class RaidCSV : ICSVData
{
    public int raidId { get; private set; }
    public string raidName { get; private set; }
    public string raidStory { get; private set; }
    public int raidDailyTryCount { get; private set; }
    public string BGMKey { get; private set; }
    public override void Init(Dictionary<string, object> readData)
    {
        raidId = ParseInt("raidId", readData);
        raidName = ParseString("raidName", readData);
        raidStory = ParseString("raidStory", readData);
        raidDailyTryCount = ParseInt("raidDailyTryCount", readData);
        BGMKey = ParseString("BGMKey", readData);
    }

}

public class RaidDifficultyCSV : ICSVData
{
    
    public int raidId { get; private set; }
    public int difficulty { get; private set; }
    public string raidName { get; private set; }
    public string backgroundName { get; private set; }

    public int[] firstPhaseBuffIds { get; private set; }
    public int[] secondPhaseBuffIds { get; private set; }
    public int[] thirdPhaseBuffIds { get; private set; }

    public int maxRespawnCount { get; private set; }
    public int totalUnitCount { get; private set; }
    public int totalSlotCount { get; private set; }
    public int totalPhaseCount { get; private set; }
    public float challengeLimitTs { get; private set; }
    public float middleBossRespawnTs { get; private set; }
    public float middleBossActiveTs { get; private set; }

    public int[] ticketAssetTypes { get; private set; }
    public int[] ticketAssetIds { get; private set; }
    public long[] ticketAssetCounts { get; private set; }
 
    public float[] clearRewardRates { get; private set; }
    public int[] clearRewardAssetTypes { get; private set; }
    public int[] clearRewardAssetIds { get; private set; }
    public long[] clearRewardAssetCounts { get; private set; }

    public Vector2[] unitRespawnPosition { get; private set; }

    public override void Init(Dictionary<string, object> readData)
    {
        raidId = ParseInt("raidId", readData);
        difficulty = ParseInt("difficulty", readData);

        raidName = ParseString("raidName", readData);
        backgroundName = ParseString("backgroundName", readData);

        firstPhaseBuffIds = ParseToIntArray("firstPhaseBuffIds", readData);
        secondPhaseBuffIds = ParseToIntArray("secondPhaseBuffIds", readData);
        thirdPhaseBuffIds = ParseToIntArray("thirdPhaseBuffIds", readData);

        maxRespawnCount = ParseInt("maxRespawnCount", readData);

        totalUnitCount = ParseInt("totalUnitCount", readData);
        totalSlotCount = ParseInt("totalSlotCount", readData);
        totalPhaseCount = ParseInt("totalPhaseCount", readData);

        challengeLimitTs = ParseFloat("challengeLimitTs", readData);
        middleBossRespawnTs = ParseFloat("middleBossRespawnTs", readData);
        middleBossActiveTs = ParseFloat("middleBossActiveTs", readData);

        ticketAssetTypes = ParseToIntArray("ticketAssetTypes", readData);
        ticketAssetIds = ParseToIntArray("ticketAssetIds", readData);
        ticketAssetCounts = ParseToLongArray("ticketAssetCounts", readData);

        clearRewardRates = ParseToFloatArray("clearRewardRates", readData);
        clearRewardAssetTypes = ParseToIntArray("clearRewardAssetTypes", readData);
        clearRewardAssetIds = ParseToIntArray("clearRewardAssetIds", readData);
        clearRewardAssetCounts = ParseToLongArray("clearRewardAssetCounts", readData);

        unitRespawnPosition = ParseToVector2Array("unitRespawnPosition", readData);
    }

}

public class RaidMonsterCSV : ICSVData
{
    public int raidId { get; private set; }
    public string monsterName { get; private set; }
    public int difficulty { get; private set; }

    public int monsterType { get; private set; }
    public string bodyName { get; private set; }
    public int[] skillIds { get; private set; }
    public Vector2 respawnPosition { get; private set; }

    public float hp { get; private set; }
    public float power { get; private set; }
    //public int attribute { get; private set; }

    public override void Init(Dictionary<string, object> readData)
    {
        raidId = ParseInt("raidId", readData);
        monsterName = ParseString("monsterName", readData);

        difficulty = ParseInt("difficulty", readData);

        monsterType = ParseInt("monsterType", readData);
        bodyName = ParseString("bodyName", readData);
        if (readData.ContainsKey("skillIds")) skillIds = ParseToIntArray("skillIds", readData);
        
        respawnPosition = ParseVector2("respawnPosition", readData);

        hp = ParseFloat("hp", readData);        
        power = ParseFloat("power", readData);
    }

    public float GetPower(int diff)
    {
        /*
        int diffGap = diff - minDifficulty;
        if (diffGap <= 0) return power;

        float crrPower = power;

        if (hpIncrType == 1) crrPower = power + diffGap * powerIncrValue;
        else if (hpIncrType == 2) crrPower = power * Mathf.Pow(powerIncrValue, diffGap);
        
        return crrPower;
        */
        return power;
    }

    public float GetHp(int diff)
    {
        /*
        int diffGap = diff - minDifficulty;
        if (diffGap <= 0) return hp;

        float crrHp = hp;
        if (hpIncrType == 1) crrHp = hp + diffGap * hpIncrValue;
        else if (hpIncrType == 2) crrHp = hp * Mathf.Pow(hpIncrValue, diffGap);

        return crrHp;
        */
        return hp;
    }
}