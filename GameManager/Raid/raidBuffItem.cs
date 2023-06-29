using Beebyte.Obfuscator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class raidBuffItem : MonoBehaviour
{
    [SerializeField] RectTransform rect;
    [SerializeField] Image imgIcon;
    [SerializeField] Text txtName;
    [SerializeField] Text txtDescription;

    public int skillId { get; private set; }

    Raid state;
    public void InitItem(StageBuffCSV csv)
    {
        var buffeff = GameAssetCSVContainer.GetInstance().GetBuffEffectCSV(csv.upgradeType, csv.upgradeArgs);
        Sprite icon = null;
        if (buffeff != null && !string.IsNullOrEmpty(buffeff.effectIconName)) icon = AssetManager.GetBuffIcon(buffeff.effectIconName);

        string buffName = PropertyDescriptionCreater.GetDescription(csv, csv.description);

        if (icon == null)
        {
            imgIcon.gameObject.SetActive(false);
        }
        else
        {
            imgIcon.gameObject.SetActive(true);
            imgIcon.sprite = icon;
        }

        if (txtName != null) txtName.text = buffName;
        //if (txtDescription != null) txtDescription.text = description;

        gameObject.SetActive(true);
    }
}
