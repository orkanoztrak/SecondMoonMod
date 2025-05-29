using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
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

    public override string ItemLangTokenName => "GOURMET_STEAK";

    public override string ItemPickupDesc => $"Increase your maximum health and your one-shot protection threshold.";

    public override string ItemFullDesc => $"Gain <style=cIsHealing>{GourmetSteakHealthInit * 100}%</style> <style=cStack>(+{GourmetSteakHealthStack * 100}% per stack)</style> <style=cIsHealing>maximum health</style>" +
        $" and increase <style=cIsHealth>one-shot protection threshold</style> by <style=cIsHealth>{GourmetSteakOSPThresholdIncreaseInit * 100}%</style> <style=cStack>(+{GourmetSteakOSPThresholdIncreaseStack * 100}% per stack)</style>.";

    public override string ItemLore => "Everyone is invited! This weekend, Lord Holden is hosting the biggest ball of the year, maybe even decade, maybe even century! Exotic dancers, the most lavish of gourmet dishes, and a night you will never forget!\r\n\r\n" +
        "I repeat, everyone is invited!";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += GourmetSteakIncreaseOSPThreshold;
        RecalculateStatsAPI.GetStatCoefficients += GourmetSteakIncreaseHealth;
    }

    private void GourmetSteakIncreaseOSPThreshold(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield"),
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_cursePenalty")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                if (body)
                {
                    var stackCount = GetCount(body);
                    if (stackCount > 0)
                    {
                        var limitedStacks = stackCount > GourmetSteakOSPThresholdIncreaseLimit ? GourmetSteakOSPThresholdIncreaseLimit : stackCount;
                        body.oneShotProtectionFraction *= 1 + (GourmetSteakOSPThresholdIncreaseInit + (limitedStacks - 1) * GourmetSteakOSPThresholdIncreaseStack);
                    }
                }
            });
        }
    }

    private void GourmetSteakIncreaseHealth(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.healthMultAdd += GourmetSteakHealthInit + (stackCount - 1) * GourmetSteakHealthStack;
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
        GourmetSteakHealthInit = config.ActiveBind("Item: " + ItemName, "Maximum health increase with one " + ItemName, 0.08f, "How much should maximum health be increased by with one " + ItemName + "? (0.08 = 8%)");
        GourmetSteakHealthStack = config.ActiveBind("Item: " + ItemName, "Maximum health increase per stack after one " + ItemName, 0.08f, "How much should maximum health be increased by per stack of " + ItemName + " after one? (0.08 = 8%)");
        GourmetSteakOSPThresholdIncreaseInit = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase with one " + ItemName, 0.1f, "How much should the one-shot protection threshold be increased by with one " + ItemName + "? (0.1 = 10%)");
        GourmetSteakOSPThresholdIncreaseStack = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase per stack after one " + ItemName, 0.1f, "How much should the one-shot protection threshold be increased by per stack of " + ItemName + " after one? (0.1 = 10%)");
        GourmetSteakOSPThresholdIncreaseLimit = config.ActiveBind("Item: " + ItemName, "One-shot protection threshold increase limit", 40, "After how many " + ItemName + "s should the one-shot protection threshold stop increasing? Note that for default settings, if you set this too high, you will be unable to take damage after 90 stacks.");
    }
}
