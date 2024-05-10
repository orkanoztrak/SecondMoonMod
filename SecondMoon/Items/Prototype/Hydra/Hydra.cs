﻿using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.AttackTypes.Orbs.Item.Prototype.Hydra;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Equipment.EssenceChanneler.EssenceChanneler;

namespace SecondMoon.Items.Prototype.Hydra;

public class Hydra : Item<Hydra>
{
    public static ConfigOption<float> HydraBaseDamageInit;
    public static ConfigOption<float> HydraBaseDamageStack;

    public int RecursionPrevention;
    public override string ItemName => "Hydra";

    public override string ItemLangTokenName => "SECONDMOONMOD_HYDRA";

    public override string ItemPickupDesc => "You hit two extra times. Your damage is modified accordingly.";

    public override string ItemFullDesc => $"You hit <color=#7CFDEA>2 extra times</color>. Your damage on hit is <style=cIsDamage>{HydraBaseDamageInit * 100}%</style> <style=cStack>(+{HydraBaseDamageStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamage += HydraSetDamage;
        On.RoR2.GlobalEventManager.OnHitEnemy += HydraMultiHits;
    }

    private void HydraMultiHits(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0 && RecursionPrevention < 2 && damageInfo.procCoefficient > 0)
                {
                    var teamComponent = attackerBody.GetComponent<TeamComponent>();
                    var victimBody = victim ? victim.GetComponent<CharacterBody>() : null;

                    HydraOrb hydraOrb = new HydraOrb
                    {
                        origin = damageInfo.position,
                        damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, 1f) / (HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack)),
                        isCrit = damageInfo.crit,
                        totalStrikes = 1,
                        teamIndex = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral,
                        attacker = damageInfo.attacker,
                        inflictor = damageInfo.inflictor,
                        procCoefficient = damageInfo.procCoefficient,
                        damageColorIndex = damageInfo.damageColorIndex,
                        procChainMask = damageInfo.procChainMask,
                        damageType = damageInfo.damageType,
                        secondsPerStrike = 0.1f
                    };
                    HurtBox mainHurtBox2 = victimBody.mainHurtBox;
                    try
                    {
                        RecursionPrevention++;
                        if ((bool)mainHurtBox2)
                        {
                            hydraOrb.target = mainHurtBox2;
                            OrbManager.instance.AddOrb(hydraOrb);
                        }
                    }

                    finally
                    {
                        RecursionPrevention = 0;
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private void HydraSetDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
    {
        var newDamageInfo = damageInfo;
        if (newDamageInfo.attacker)
        {
            var stackCount = GetCount(newDamageInfo.attacker.GetComponent<CharacterBody>());
            if (stackCount > 0 && newDamageInfo.procCoefficient > 0)
            {
                newDamageInfo.damage *= HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack);
            }
        }
        orig(self, newDamageInfo);
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            OrbAPI.AddOrb(typeof(HydraOrb));
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        HydraBaseDamageInit = config.ActiveBind("Item: " + ItemName, "Damage modifier with one " + ItemName, 0.4f, "How much should damage be set to on hits with proc coefficient greater than zero with one Hydra? (0.4 = 40%)");
        HydraBaseDamageStack = config.ActiveBind("Item: " + ItemName, "Damage modifier per stack after one " + ItemName, 0.1f, "How much should damage be increased for hits with proc coefficient greater than zero per stack of Hydra after one? (0.1 = 10%)");
    }

    /*private void HydraSetDamage(ILContext il)
{
    var cursor = new ILCursor(il);
    cursor.GotoNext(
        x => x.MatchLdloc(4),
        x => x.MatchLdarg(0),
        x => x.MatchCall<HealthComponent>("get_fullCombinedHealth"),
        x => x.MatchLdcR4(0.9f)
        );

    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, 6);
    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0);
    cursor.EmitDelegate<Func<DamageInfo, float, CharacterMaster, float>>((damageInfo, num, master) =>
    {
        var stackCount = GetCount(master);
        if (stackCount > 0 && damageInfo.procCoefficient > 0)
        {
            num *= HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack);
        }
        return num;
    });
    cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, 6);
}
*/

}
