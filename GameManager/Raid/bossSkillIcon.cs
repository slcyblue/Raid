using Beebyte.Obfuscator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class bossSkillIcon : MonoBehaviour
{
    [SerializeField] Text description;
    [SerializeField] Image skillIconImage;
    [SerializeField] GameObject CurrentSkillEffect;
    [SerializeField] GameObject SkillDetailPanel;

    public int skillId { get; private set; }

    Raid state;
    public void InitItem(int skillId, int monsterType, Raid state)
    {
        this.skillId = skillId;
        this.state = state;
        var csv = SkillCSVContainer.GetInstance().GetSkillCSV(skillId);
        description.text = PropertyDescriptionCreater.GetSkillDescription(csv);
        if (skillIconImage != null)
        {
            switch (monsterType)
            {
                case (int)Raid.StageBuffTargetEnum.BossMonster:
                    skillIconImage.sprite = AssetManager.GetBuffIcon(csv.skillIconName);
                    break;
                case (int)Raid.StageBuffTargetEnum.NormalMonster:
                    skillIconImage.sprite = AssetManager.GetUnitProfile(state.GetCurrentMonsterCSV(Raid.StageBuffTargetEnum.NormalMonster).bodyName);
                    break;
            }
        }

        SkillDetailPanel.SetActive(false);
    }

    public void SetSkillEffect(bool isOn)
    {
        CurrentSkillEffect.SetActive(isOn);
    }

    [SkipRename]
    public void OpenSkillDetail()
    {
        SkillDetailPanel.SetActive(true);
    }

    [SkipRename]
    public void CloseSkillDetail()
    {
        SkillDetailPanel.SetActive(false);
    }
}
