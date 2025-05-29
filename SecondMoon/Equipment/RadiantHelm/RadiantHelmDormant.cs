using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;

namespace SecondMoon.Equipment.RadiantHelm;

public class RadiantHelmDormant : Item<RadiantHelmDormant>
{
    public override string ItemName => "Radiant Helm (Dormant)";

    public override string ItemLangTokenName => "RADIANT_HELM_DORMANT";

    public override string ItemPickupDesc => "Does nothing. <color=#7CFDEA>Awaken this item to reveal its true power...</color>";

    public override string ItemFullDesc => $"Does nothing. Can be given to the <color=#7CFDEA>Awakening Shrine</color> to set its reward to <color=#7CFDEA>{RadiantHelm.instance.EquipmentName}</color>.";

    public override string ItemLore => "";

    public override ItemTierDef ItemTierDef => TierPrototypeDormant.instance.ItemTierDef;

    public override ItemTag[] Category => [];

    public override EquipmentIndex ActivateIntoPrototypeEquipment => RadiantHelm.instance.EquipmentDef.equipmentIndex;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {

    }

    public override void Init(ConfigFile config)
    {
        EnableCheck = RadiantHelm.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
