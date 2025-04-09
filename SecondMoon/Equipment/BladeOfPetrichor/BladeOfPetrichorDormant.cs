using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;

namespace SecondMoon.Equipment.BladeOfPetrichor;

public class BladeOfPetrichorDormant : Item<BladeOfPetrichorDormant>
{
    public override string ItemName => "Blade of Petrichor (Dormant)";

    public override string ItemLangTokenName => "BLADE_OF_PETRICHOR_DORMANT";

    public override string ItemPickupDesc => "Does nothing. <color=#7CFDEA>Awaken this item to reveal its true power...</color>";

    public override string ItemFullDesc => $"Does nothing. Can be given to the <color=#7CFDEA>Awakening Shrine</color> to set its reward to <color=#7CFDEA>{BladeOfPetrichor.instance.EquipmentName}</color>.";

    public override string ItemLore => "";

    public override ItemTierDef ItemTierDef => TierPrototypeDormant.instance.ItemTierDef;

    public override ItemTag[] Category => [];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {

    }

    public override void Init(ConfigFile config)
    {
        EnableCheck = BladeOfPetrichor.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
