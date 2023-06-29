using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RaidMonsterItem : MonoBehaviour
{
    [SerializeField] Image monsterIcon;
    [SerializeField] Text monsterName;
    [SerializeField] Text monsterHp;
    [SerializeField] Text monsterSkill;


    public void InitItem(RaidMonsterCSV csv, RaidEntrancePopup mgr)
    {
        monsterIcon.sprite = AssetManager.GetUnitProfile(csv.bodyName);
        monsterName.text = csv.monsterName;
        monsterHp.text = $"HP : {csv.hp}";
        var skillcsv = SkillCSVContainer.GetInstance().GetSkillCSV(csv.skillIds[0]);
        
        monsterSkill.text = $"{PropertyDescriptionCreater.GetSkillDescription(skillcsv)}";
    }
}
