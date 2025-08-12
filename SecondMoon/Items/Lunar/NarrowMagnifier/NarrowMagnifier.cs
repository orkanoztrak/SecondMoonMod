using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Lunar.NarrowMagnifier;

public class NarrowMagnifier : Item<NarrowMagnifier>
{
    public static ConfigOption<float> NarrowMagnifierMinRadius;
    public static ConfigOption<float> NarrowMagnifierMaxRadius;
    public static ConfigOption<float> NarrowMagnifierCCDmgConversion;

    public static ConfigOption<float> NarrowMagnifierDamageBoostInit;
    public static ConfigOption<float> NarrowMagnifierDamageBoostStack;
    public static ConfigOption<float> NarrowMagnifierDamageReductionInit;
    public static ConfigOption<float> NarrowMagnifierDamageReductionStack;

    public override string ItemName => "Narrow Magnifier";

    public override string ItemLangTokenName => "NARROW_MAGNIFIER";

    public override string ItemPickupDesc => "Gain increased damage based on critical chance and deal increased damage against enemies at a certain distance... <color=#FF7F7F>BUT deal reduced damage to enemies not at that distance and disable critical hits.</color>\n";

    public override string ItemFullDesc => $"For every <style=cIsDamage>1% critical chance (up to 100%)</style>, increase <style=cIsDamage>base damage</style> by <style=cIsDamage>{1 * NarrowMagnifierCCDmgConversion}% (+{0.01 * NarrowMagnifierCCDmgConversion}% per 1% critical damage increase)</style>. " +
        $"Against enemies between <style=cIsDamage>{NarrowMagnifierMinRadius}m</style> and <style=cIsDamage>{NarrowMagnifierMaxRadius}m</style> away, " +
        $"increase <style=cIsDamage>damage dealt</style> by <style=cIsDamage>{NarrowMagnifierDamageBoostInit * 100}%</style> <style=cStack>(+{NarrowMagnifierDamageBoostStack * 100}% per stack)</style>. " +
        $"<color=#FF7F7F> Against enemies outside this range, reduce <style=cIsDamage>damage dealt</style> by <style=cIsDamage>{NarrowMagnifierDamageReductionInit * 100}%</style> <style=cStack>(+{NarrowMagnifierDamageReductionStack * 100}% per stack)</style>. " +
        $"You cannot critically strike.</color>";

    public override string ItemLore => "\"Mommy, mommy!\"\r\n\r\n" +
        "\"What is it, darling?\"\r\n\r\n" +
        "\"Look at this toy I found! Things look bigger when I look through it, but sometimes they look weird!\"﻿\r\n\r\n" +
        "\"Oh honey, you took this from daddy's lab didn't you? We've talked about this before, haven't we?\"\r\n\r\n" +
        "\"Sorry... I got curious...\"﻿\r\n\r\n" +
        "\"Well, since we have it here, at least let me show you how it works. See, this isn't a toy, it's a tool to see things more clearly. That's why things look bigger through it.\"﻿\r\n\r\n" +
        "\"Ooh!\"﻿\r\n\r\n" +
        "\"But that doesn't mean it can't be fun or interesting! See, when it's sunny and you take a piece of paper, like this...\"﻿\r\n\r\n" +
        "\"Eep! It's burning! But it's cool! How did you know it would do that?\"﻿\r\n\r\n" +
        "\"Hehe, mommy was young once too, you know. My friends and I would do this often. Now be a good girl and put it back where you found it, won't you?\"\r\n\r\n" +
        "\"Yes!\"\r\n\r\n" +
        "\"And no snacks until tomorrow.\"\r\n\r\n" +
        "\"Waah, not fair!!\"";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamageProcess += NarrowMagnifierModifyDamage;
    }

    private void NarrowMagnifierModifyDamage(ILContext il)
    {
        var damageIndex = 7;
        var flagIndex = 5;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
                               x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.DeathMark))))
        {
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchLdloc(flagIndex)))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, damageIndex);
                cursor.EmitDelegate<Func<DamageInfo, float, float>>((info, num) =>
                {
                    if (info.attacker)
                    {
                        var attackerBody = info.attacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            var stackCount = GetCount(attackerBody);
                            if (stackCount > 0)
                            {
                                var position = attackerBody.corePosition - info.position;
                                if (position.sqrMagnitude >= Math.Pow(NarrowMagnifierMinRadius, 2) && position.sqrMagnitude <= Math.Pow(NarrowMagnifierMaxRadius, 2))
                                {
                                    info.damageColorIndex = SecondMoonModColors.NarrowMagnifierStrongHitColor;
                                    num *= (1 + NarrowMagnifierDamageBoostInit) * (float)Math.Pow(1 + NarrowMagnifierDamageBoostStack, stackCount - 1);
                                }
                                else
                                {
                                    info.damageColorIndex = SecondMoonModColors.NarrowMagnifierWeakHitColor;
                                    num *= (1 - NarrowMagnifierDamageReductionInit) * (float)Math.Pow(1 - NarrowMagnifierDamageReductionStack, stackCount - 1);
                                }
                                if (info.crit)
                                {
                                    info.crit = false;
                                    num /= attackerBody.critMultiplier;
                                }
                            }
                        }
                    }
                    return num;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, damageIndex);
            }
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
        NarrowMagnifierMinRadius = config.ActiveBind("Item: " + ItemName, "Minimum radius of the damage boost", 13f, "Enemies closer than this many meters take reduced damage instead.");
        NarrowMagnifierMaxRadius = config.ActiveBind("Item: " + ItemName, "Maximum radius of the damage boost", 25f, "Enemies farther than this many meters take reduced damage instead.");
        NarrowMagnifierCCDmgConversion = config.ActiveBind("Item: " + ItemName, "Conversion of critical chance to regular damage", 1f, "By default, conversion happens so that the disabling of critical hits does not reduce or increase damage output. Damage boost will be multiplied by the value of this config option.");

        NarrowMagnifierDamageBoostInit = config.ActiveBind("Item: " + ItemName, "Multiplicative damage boost with one " + ItemName, 1f, "How much should damage be increased multiplicatively within the effect radius with one " + ItemName + "? This scales exponentially (1 = 100%, refer to Shaped Glass on the wiki).");
        NarrowMagnifierDamageBoostStack = config.ActiveBind("Item: " + ItemName, "Multiplicative damage boost per stack after one " + ItemName, 1f, "How much should damage be increased multiplicatively within the effect radius per stack of " + ItemName + " after one? This scales exponentially (1 = 100%, refer to Shaped Glass on the wiki).");

        NarrowMagnifierDamageReductionInit = config.ActiveBind("Item: " + ItemName, "Multiplicative damage reduction with one " + ItemName, 0.33f, "How much should damage be reduced multiplicatively outside the effect radius with one " + ItemName + "? This scales exponentially (0.33 = 33%, refer to Shaped Glass on the wiki).");
        NarrowMagnifierDamageReductionStack = config.ActiveBind("Item: " + ItemName, "Multiplicative damage reduction per stack after one " + ItemName, 0.33f, "How much should damage be reduced multiplicatively outside the effect radius per stack of " + ItemName + " after one? This scales exponentially (0.33 = 33%, refer to Shaped Glass on the wiki).");
    }

}
