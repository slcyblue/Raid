using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class RaidCSVContainer : Singleton<RaidCSVContainer>, ICSVContainer
{
    Dictionary<int, RaidCSV> raids;
    Dictionary<int, List<RaidDifficultyCSV>> raidDifficulties;
    Dictionary<int, List<RaidMonsterCSV>> raidMonsters;
    Dictionary<int, StageBuffCSV> raidBuffs;

    public IEnumerator ParseRoutine(Action complete)
    {
        CSVReader.LoadCSVDict<int, RaidCSV>("Assets/CSVs/raids/contents _ raid - raid.csv", "raidId", (dict) => raids = dict);
        CSVReader.LoadCSVListDic<int, RaidDifficultyCSV>("Assets/CSVs/raids/contents _ raid - raidDifficulties.csv", "raidId", (dict) => raidDifficulties = dict);
        CSVReader.LoadCSVListDic<int, RaidMonsterCSV>("Assets/CSVs/raids/contents _ raid - raidMonsters.csv", "raidId", (dict) => raidMonsters = dict, "raidId");
        CSVReader.LoadCSVDict<int, StageBuffCSV>("Assets/CSVs/raids/contents _ raid - raidBuffs.csv", "propertyId", (dict) => raidBuffs = dict);
        yield return new WaitForSeconds(.1f);

        complete?.Invoke();
    }

    public string GetRaidName(int raidId)
    {
        if (!raids.TryGetValue(raidId, out RaidCSV csv)) return null;

        return csv.raidName;
    }

    public string GetRaidStory(int raidId)
    {
        if (!raids.TryGetValue(raidId, out RaidCSV csv)) return null;

        return csv.raidStory;
    }

    public int GetRaidDailyTryCount(int raidId)
    {
        if (!raids.TryGetValue(raidId, out RaidCSV csv)) return 0;

        return csv.raidDailyTryCount;
    }

    public string GetRaidSoundKey(int raidId)
    {
        if (!raids.TryGetValue(raidId, out RaidCSV csv)) return null;

        return csv.BGMKey;
    }

    public RaidDifficultyCSV GetRaidCSV(int raidId, int difficulty)
    {
        if (!raidDifficulties.TryGetValue(raidId, out List<RaidDifficultyCSV> list)) return null;

        foreach(var item in list)
        {
            if (item.difficulty == difficulty) return item;
        }

        return null;
    }

    public List<RaidMonsterCSV> GetMonsters(int raidId, int difficulty)
    {
        if (raidMonsters == null || raidMonsters.Count <= 0) return null;

        List<RaidMonsterCSV> bossList = new List<RaidMonsterCSV>();
        raidMonsters.TryGetValue(raidId, out List<RaidMonsterCSV> list);

        foreach(var item in list)
        {
            if (item.difficulty == difficulty) bossList.Add(item);
        }

        return bossList;
    }

    public List<Asset> GetRaidEnterPrice(int raidId, int difficulty)
    {
        RaidDifficultyCSV csv = GetRaidCSV(raidId, difficulty);

        if (csv == null) return null;

        List<Asset> assets = new List<Asset>();
        int length = csv.ticketAssetTypes.Length;
        for (int i = 0; i < length; i++)
        {
            Asset asset = new Asset(csv.ticketAssetTypes[i], csv.ticketAssetIds[i], csv.ticketAssetCounts[i]);
            assets.Add(asset);
        }

        return assets;
    }

    public List<Asset> GetRaidNormalRewards(int raidId, int difficulty)
    {
        RaidDifficultyCSV csv = GetRaidCSV(raidId, difficulty);

        if (csv == null) return null;

        List<Asset> assets = new List<Asset>();
        int length = csv.clearRewardAssetTypes.Length;
        for (int i = 0; i < length; i++)
        {
            if (csv.clearRewardRates[i] != 100) continue;
            Asset asset = new Asset(csv.clearRewardAssetTypes[i], csv.clearRewardAssetIds[i], csv.clearRewardAssetCounts[i]);
            assets.Add(asset);
        }

        return assets;
    }

    public List<Asset> GetRaidRandomRewards(int raidId, int difficulty)
    {
        RaidDifficultyCSV csv = GetRaidCSV(raidId, difficulty);

        if (csv == null) return null;

        List<Asset> assets = new List<Asset>();
        int length = csv.clearRewardAssetTypes.Length;
        for (int i = 0; i < length; i++)
        {
            if (csv.clearRewardRates[i] == 100) continue;

            Asset asset = new Asset(csv.clearRewardAssetTypes[i], csv.clearRewardAssetIds[i], csv.clearRewardAssetCounts[i]);
            assets.Add(asset);
        }

        return assets;
    }

    public List<Vector2> GetPositionList(int raidId, int difficulty)
    {
        var csv = GetRaidCSV(raidId, difficulty);

        if (csv == null) return null;

        return csv.unitRespawnPosition.ToList();
    }

    public StageBuffCSV GetRaidBuff(int id)
    {
        raidBuffs.TryGetValue(id, out var csv);
        return csv;
    }

    public List<StageBuffCSV> GetRaidBuffs(int raidId, int diff, int phaseNum)
    {
        var csv = GetRaidCSV(raidId, diff);
        if (csv == null) return null;

        List<StageBuffCSV> list = new List<StageBuffCSV>();
        int[] buffIds;
        switch (phaseNum)
        {
            case 1:
                buffIds = csv.firstPhaseBuffIds;
                break;
            case 2:
                buffIds = csv.secondPhaseBuffIds;
                break;
            case 3:
                buffIds = csv.thirdPhaseBuffIds;
                break;
            default:
                buffIds = null;
                break;
        }

        int length = buffIds?.Length ?? 0;
        for (int i = 0; i < length; i++)
        {
            int id = buffIds[i];
            var buffCSV = GetRaidBuff(id);
            if (buffCSV != null) list.Add(buffCSV);
        }
        return list;
    }
}
