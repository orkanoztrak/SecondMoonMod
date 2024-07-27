﻿using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Tier2.PSG_ProMaxS99;

public class PSG_ProMaxS99 : Item<PSG_ProMaxS99>
{
    public static ConfigOption<float> PSGShieldInit;
    public static ConfigOption<float> PSGShieldStack;
    public static ConfigOption<float> PSGDangerStopwatchReductionInit;
    public static ConfigOption<float> PSGDangerStopwatchReductionStack;
    public static ConfigOption<float> PSGShieldRechargeRateInit;
    public static ConfigOption<float> PSGShieldRechargeRateStack;

    public override string ItemName => "P.S.G.+ Pro Max S99";

    public override string ItemLangTokenName => "SECONDMOONMOD_PSG_WITH_AN_EXTRA_CAMERA";

    public override string ItemPickupDesc => "Gain a recharging shield and increase your shield recharge rate. Get out of danger faster.";

    public override string ItemFullDesc => $"Gain a shield equal to <style=cIsHealing>{PSGShieldInit * 100}%</style> <style=cStack>(+{PSGShieldStack * 100}% per stack)</style> of your maximum health. " +
        $"Your shields recharge <style=cIsHealing>{PSGShieldRechargeRateInit * 100}%</style> <style=cStack>(+{PSGShieldRechargeRateStack * 100}% per stack)</style> faster. " +
        $"The time it takes for you to be considered out of danger is reduced by <style=cIsHealing>{PSGDangerStopwatchReductionInit * 100}%</style> <style=cStack>(+{PSGDangerStopwatchReductionStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += PSGBoostShields;
        IL.RoR2.CharacterBody.FixedUpdate += PSGQuickerOutOfDanger;
        IL.RoR2.HealthComponent.ServerFixedUpdate += PSGQuickerShieldRecharge;
    }

    [Server]
    private void PSGQuickerShieldRecharge(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield"),
                        x => x.MatchLdcR4(0.5f));

        cursor.GotoNext(x => x.MatchAdd());
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_3);
        cursor.EmitDelegate<Func<HealthComponent, float, float>>((component, num3) =>
        {
            var stackCount = GetCount(component.body);
            if (stackCount > 0)
            {
                float increase = (float)(1 - ((1 - PSGShieldRechargeRateInit) * Math.Pow(1 - PSGShieldRechargeRateStack, stackCount - 1)));
                return 2;
            }
            return 2;
        });
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Mul);
    }

    private void PSGQuickerOutOfDanger(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchLdarg(0),
                        x => x.MatchLdfld<CharacterBody>("outOfDangerStopwatch"),
                        x => x.MatchLdcR4(7));
        ILLabel target = cursor.MarkLabel();

        cursor.GotoNext(MoveType.After, x => x.MatchStloc(2));
        ILLabel target2 = cursor.MarkLabel();

        cursor.GotoPrev(x => x.MatchLdarg(0),
                        x => x.MatchLdfld<CharacterBody>("outOfDangerStopwatch"),
                        x => x.MatchLdcR4(7));

        cursor.MoveBeforeLabels();
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<CharacterBody, int>>(GetCount);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ble_S, target.Target);

        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<CharacterBody, bool>>((body) =>
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                var increase = Utils.Utils.HyperbolicScaling(PSGDangerStopwatchReductionInit + (stackCount - 1) * PSGDangerStopwatchReductionStack);
                return body.outOfDangerStopwatch >= 7 * (1 - increase);
            }
            return false;
        });
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc_2);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Br, target2.Target);
    }

    private void PSGBoostShields(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            HealthComponent healthComponent = sender.healthComponent;
            args.baseShieldAdd += healthComponent.fullHealth * (PSGShieldInit + (stackCount - 1) * PSGShieldStack);
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
        PSGShieldInit = config.ActiveBind("Item: " + ItemName, "Maximum shield increase with one " + ItemName, 0.12f, "Gain shield equal to what % of maximum health with one " + ItemName + "? (0.12 = 12%)");
        PSGShieldStack = config.ActiveBind("Item: " + ItemName, "Maximum shield increase per stack after one " + ItemName, 0.12f, "Gain shield equal to what % of maximum health per stack of " + ItemName + " after one? (0.12 = 12%)");
        PSGDangerStopwatchReductionInit = config.ActiveBind("Item: " + ItemName, "Shield recharge delay reduction with one " + ItemName, 0.15f, "How much should shield recharge delay be reduced by with one " + ItemName + "? This scales hyperbolically (0.15 = 15%, refer to Tougher Times on the wiki).");
        PSGDangerStopwatchReductionStack = config.ActiveBind("Item: " + ItemName, "Shield recharge delay reduction per stack after one " + ItemName, 0.15f, "How much should shield recharge delay be reduced by per stack of " + ItemName + " after one? This scales hyperbolically (0.15 = 15%, refer to Tougher Times on the wiki).");
        PSGShieldRechargeRateInit = config.ActiveBind("Item: " + ItemName, "Shield recharge rate increase with one " + ItemName, 0.15f, "How much should shield recharge rate be increased by with one " + ItemName + "? This scales exponentially (0.15 = 15%, refer to Fuel Cell on the wiki).");
        PSGShieldRechargeRateStack = config.ActiveBind("Item: " + ItemName, "Shield recharge rate increase per stack after one " + ItemName, 0.15f, "How much should shield recharge rate be increased by per stack of " + ItemName + " after one? This scales exponentially (0.15 = 15%, refer to Fuel Cell on the wiki).");
    }
}
