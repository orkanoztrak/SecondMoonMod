using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier2.GourmetSteak;

public class GourmetSteak : Item<GourmetSteak>
{
    public static ConfigOption<float> GourmetSteakHealthInit;
    public static ConfigOption<float> GourmetSteakHealthStack;
    public static ConfigOption<float> GourmetSteakOSPThresholdIncreaseInit;
    public static ConfigOption<float> GourmetSteakOSPThresholdIncreaseStack;
    public static ConfigOption<int> GourmetSteakOSPThresholdIncreaseLimit;

    public override string ItemName => "Gourmet Steak";

    public override string ItemLangTokenName => "SECONDMOONMOD_GOURMET_STEAK";

    public override string ItemPickupDesc => $"Increase your maximum health and your one-shot protection threshold.";

    public override string ItemFullDesc => $"Increases <style=cIsHealing>maximum health</style> by <style=cIsHealing>{GourmetSteakHealthInit * 100}%</style> <style=cStack>(+{GourmetSteakHealthStack * 100}% per stack)</style>" +
        $" and <style=cIsHealth>one-shot protection threshold</style> by <style=cIsHealth>{GourmetSteakOSPThresholdIncreaseInit * 100}%</style> <style=cStack>(+{GourmetSteakOSPThresholdIncreaseStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RecalculateStats += GourmetSteakIncreaseOSPThreshold;
        RecalculateStatsAPI.GetStatCoefficients += GourmetSteakIncreaseHealth;
    }

    private void GourmetSteakIncreaseHealth(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
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
            var limitedStacks = stackCount > GourmetSteakOSPThresholdIncreaseLimit ? GourmetSteakOSPThresholdIncreaseLimit : stackCount;
            self.oneShotProtectionFraction *= 1 + (GourmetSteakOSPThresholdIncreaseInit + (limitedStacks - 1) * GourmetSteakOSPThresholdIncreaseStack);
        }
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        GourmetSteakHealthInit = config.ActiveBind("Item: " + ItemName, "Maximum health increase with one " + ItemName, 0.08f, "How much should maximum health be increased by with one Gourmet Steak? (0.08 = 8%)");
        GourmetSteakHealthStack = config.ActiveBind("Item: " + ItemName, "Maximum health increase per stack after one " + ItemName, 0.08f, "How much should maximum health be increased by per stack of Gourmet Steak after one? (0.08 = 8%)");
        GourmetSteakOSPThresholdIncreaseInit = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase with one " + ItemName, 0.1f, "How much should the one-shot protection threshold be increased by with one Gourmet Steak? (0.1 = 10%)");
        GourmetSteakOSPThresholdIncreaseStack = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase per stack after one " + ItemName, 0.1f, "How much should the one-shot protection threshold be increased by per stack of Gourmet Steak after one? (0.1 = 10%)");
        GourmetSteakOSPThresholdIncreaseLimit = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase limit", 40, "After how many Gourmet Steaks should the one-shot protection threshold stop increasing? Note that for default settings, if you set this too high, you will be unable to take damage after 90 stacks.");
    }
}
