using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;

namespace SecondMoon.Items.Prototype.Thunderbolt;

public class ThunderboltDormant : Item<ThunderboltDormant>
{
    public override string ItemName => "Thunderbolt (Dormant)";

    public override string ItemLangTokenName => "THUNDERBOLT_DORMANT";

    public override string ItemPickupDesc => "Does nothing. <color=#7CFDEA>Awaken this item to reveal its true power...</color>";

    public override string ItemFullDesc => $"Does nothing. Can be given to the <color=#7CFDEA>Awakening Shrine</color> to set its reward to <color=#7CFDEA>{Thunderbolt.instance.ItemName}</color>.";

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
        EnableCheck = Thunderbolt.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
