using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Items.Tier2.GourmetSteak;

internal class GourmetSteak : Item<GourmetSteak>
{
    public static float GourmetSteakHealthInit = 0.08f;
    public static float GourmetSteakHealthStack = 0.08f;
    public static float GourmetSteakOSPThresholdIncreaseInit = 0.1f;
    public static float GourmetSteakOSPThresholdIncreaseStack = 0.1f;

    public override string ItemName => "Gourmet Steak";

    public override string ItemLangTokenName => "SECONDMOONMOD_GOURMET_STEAK";

    public override string ItemPickupDesc => $"Increase your maximum health and your one-shot protection threshold.";

    public override string ItemFullDesc => $"Increases <style=cIsHealing>maximum health</style> by <style=cIsHealing>{GourmetSteakHealthInit * 100}%</style> <style=cStack>(+{GourmetSteakHealthStack * 100}% per stack)</style>" +
        $" and <style=cIsHealth>one-shot protection threshold</style> by <style=cIsHealth>{GourmetSteakOSPThresholdIncreaseInit * 100}%</style> <style=cStack>(+{GourmetSteakOSPThresholdIncreaseStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;

    public override ItemTag[] Category => [ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RecalculateStats += GourmetSteakIncreaseOSPThreshold;
        GetStatCoefficients += GourmetSteakIncreaseHealth;
    }

    private void GourmetSteakIncreaseHealth(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.healthMultAdd += GourmetSteakHealthInit + (stackCount - 1) * GourmetSteakHealthStack;
        }

    }

    private void GourmetSteakIncreaseOSPThreshold(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.oneShotProtectionFraction *= 1 + (GourmetSteakOSPThresholdIncreaseInit + (stackCount - 1) * GourmetSteakOSPThresholdIncreaseStack);
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
