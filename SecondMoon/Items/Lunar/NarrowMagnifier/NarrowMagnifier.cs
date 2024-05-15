using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Lunar.NarrowMagnifier;

public class NarrowMagnifier : Item<NarrowMagnifier>
{
    //public DamageColorIndex NarrowMagnifierStrongHitColor;
    //public DamageColorIndex NarrowMagnifierWeakHitColor;
    public static ConfigOption<float> NarrowMagnifierMinRadius;
    public static ConfigOption<float> NarrowMagnifierMaxRadius;
    public static ConfigOption<float> NarrowMagnifierCCDmgConversion;

    public static ConfigOption<float> NarrowMagnifierDamageBoostInit;
    public static ConfigOption<float> NarrowMagnifierDamageBoostStack;
    public static ConfigOption<float> NarrowMagnifierDamageReductionInit;
    public static ConfigOption<float> NarrowMagnifierDamageReductionStack;

    public override string ItemName => "Narrow Magnifier";

    public override string ItemLangTokenName => "SECONDMOONMOD_NARROW_MAGNIFIER";

    public override string ItemPickupDesc => "Gain damage per critical chance and deal increased damage against enemies at a certain distance... <color=#FF7F7F>BUT deal reduced damage to enemies not at that distance and disable critical hits.</color>\n";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamage += NarrowMagnifierModifyDamage;
        RecalculateStatsAPI.GetStatCoefficients += NarrowMagnifierConvertCrit;
    }

    private void NarrowMagnifierConvertCrit(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.baseDamageAdd *= 1 + ((sender.critMultiplier - 1) * sender.crit);
        }
    }

    private void NarrowMagnifierModifyDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
                               x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.DeathMark))))
        {
            if (cursor.TryGotoNext(MoveType.After, y => y.MatchStfld(typeof(DamageInfo), nameof(DamageInfo.damageColorIndex))))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.EmitDelegate<Action<DamageInfo>>((info) => 
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
                                    //info.damageColorIndex = NarrowMagnifierStrongHitColor;
                                    info.damage *= (1 + NarrowMagnifierDamageBoostInit) * (float)Math.Pow(1 + NarrowMagnifierDamageBoostStack, stackCount - 1);
                                }
                                else
                                {
                                    //info.damageColorIndex = NarrowMagnifierWeakHitColor;
                                    info.damage *= (1 - NarrowMagnifierDamageReductionInit) * (float)Math.Pow(1 - NarrowMagnifierDamageReductionStack, stackCount - 1);
                                }
                                info.crit = false;
                            }
                        }
                    }
                });
                Debug.Log(il.ToString());
            }
            else
            {
                Debug.Log("inner if not found");
            }
        }
        else
        {
            Debug.Log("outer if not found");
        }
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            //NarrowMagnifierStrongHitColor = ColorsAPI.RegisterDamageColor(new Color32(0, 0, 255, 255));
            //NarrowMagnifierWeakHitColor = ColorsAPI.RegisterDamageColor(new Color32(0, 0, 255, 64));
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
        NarrowMagnifierCCDmgConversion = config.ActiveBind("Item: " + ItemName, "Conversion of critical chance to regular damage", 1f, "By default, conversion happens so that the disabling of critical hits does not reduce or increase damage output. Damage will be multiplied by the value of this config option.");

        NarrowMagnifierDamageBoostInit = config.ActiveBind("Item: " + ItemName, "Multiplicative damage boost with one " + ItemName, 1f, "How much should damage be increased multiplicatively within the effect radius with one Narrow Magnifier? This scales exponentially (1 = 100%, refer to Shaped Glass on the wiki).");
        NarrowMagnifierDamageBoostStack = config.ActiveBind("Item: " + ItemName, "Multiplicative damage boost per stack after one " + ItemName, 1f, "How much should damage be increased multiplicatively within the effect radius per stack of Narrow Magnifier after one? This scales exponentially (1 = 100%, refer to Shaped Glass on the wiki).");

        NarrowMagnifierDamageReductionInit = config.ActiveBind("Item: " + ItemName, "Multiplicative damage reduction with one " + ItemName, 0.33f, "How much should damage be reduced multiplicatively outside the effect radius with one Narrow Magnifier? This scales exponentially (0.33 = 33%, refer to Shaped Glass on the wiki).");
        NarrowMagnifierDamageReductionStack = config.ActiveBind("Item: " + ItemName, "Multiplicative damage reduction per stack after one " + ItemName, 0.33f, "How much should damage be reduced multiplicatively outside the effect radius per stack of Narrow Magnifier after one? This scales exponentially (0.33 = 33%, refer to Shaped Glass on the wiki).");
    }
}
