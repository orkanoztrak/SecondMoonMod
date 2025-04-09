using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;

namespace SecondMoon.Items.Prototype.Hydra;

public class HydraDormant : Item<HydraDormant>
{
    public override string ItemName => "Hydra (Dormant)";

    public override string ItemLangTokenName => "HYDRA_DORMANT";

    public override string ItemPickupDesc => "Does nothing. <color=#7CFDEA>Awaken this item to reveal its true power...</color>";

    public override string ItemFullDesc => $"Does nothing. Can be given to the <color=#7CFDEA>Awakening Shrine</color> to set its reward to <color=#7CFDEA>{Hydra.instance.ItemName}</color>.";

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
        EnableCheck = Hydra.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
