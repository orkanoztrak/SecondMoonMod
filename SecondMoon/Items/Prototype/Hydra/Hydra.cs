using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.AttackTypes.Orbs.Item.Prototype.Hydra;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SecondMoon.Equipment.EssenceChanneler.EssenceChanneler;

namespace SecondMoon.Items.Prototype.Hydra;

public class Hydra : Item<Hydra>
{
    public static float HydraBaseDamageInit = 0.4f;
    public static float HydraBaseDamageStack = 0.1f;
    public static DamageAPI.ModdedDamageType HydraHit;
    public int RecursionPrevention;
    public override string ItemName => "Hydra";

    public override string ItemLangTokenName => "SECONDMOON_HYDRA";

    public override string ItemPickupDesc => "You hit two extra times. Your damage is modified accordingly.";

    public override string ItemFullDesc => $"You hit <color=#7CFDEA>2 extra times</color>. Your damage is <style=cIsDamage>{HydraBaseDamageInit * 100}%</style> <style=cStack>(+{HydraBaseDamageStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

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

                    HydraOrb hydraOrb = new HydraOrb();
                    hydraOrb.origin = damageInfo.position;
                    hydraOrb.damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, 1f) / (HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack));
                    hydraOrb.isCrit = damageInfo.crit;
                    hydraOrb.totalStrikes = 1;
                    hydraOrb.teamIndex = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral;
                    hydraOrb.attacker = damageInfo.attacker;
                    hydraOrb.inflictor = damageInfo.inflictor;
                    hydraOrb.procCoefficient = damageInfo.procCoefficient;
                    hydraOrb.damageColorIndex = damageInfo.damageColorIndex;
                    hydraOrb.procChainMask = damageInfo.procChainMask;
                    hydraOrb.damageType = damageInfo.damageType;
                    hydraOrb.secondsPerStrike = 0.1f;
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
        var stackCount = GetCount(newDamageInfo.attacker?.GetComponent<CharacterBody>());
        if (stackCount > 0 && newDamageInfo.procCoefficient > 0)
        {
            newDamageInfo.damage *= HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack);
        }
        orig(self, newDamageInfo);
    }

    public override void Init()
    {
        HydraHit = DamageAPI.ReserveDamageType();
        OrbAPI.AddOrb(typeof(HydraOrb));
        CreateLang();
        CreateItem();
        Hooks();
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
    Debug.Log(il.ToString());
}
*/

}
