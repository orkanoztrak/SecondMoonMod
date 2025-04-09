using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;

namespace SecondMoon.Items.Prototype.TremorKnuckles;

public class TremorKnucklesDormant : Item<TremorKnucklesDormant>
{
    public override string ItemName => "Tremor Knuckles (Dormant)";

    public override string ItemLangTokenName => "TREMOR_KNUCKLES_DORMANT";

    public override string ItemPickupDesc => "Does nothing. <color=#7CFDEA>Awaken this item to reveal its true power...</color>";

    public override string ItemFullDesc => $"Does nothing. Can be given to the <color=#7CFDEA>Awakening Shrine</color> to set its reward to <color=#7CFDEA>{TremorKnuckles.instance.ItemName}</color>.";

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
        EnableCheck = TremorKnuckles.instance.EnableCheck;
        if (EnableCheck)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
