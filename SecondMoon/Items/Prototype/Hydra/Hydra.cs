using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.AttackTypes.Orbs.Item.Prototype.Hydra;
using SecondMoon.Items.ItemTiers.TierPrototype;
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
    public override string ItemName => "Hydra";

    public override string ItemLangTokenName => "SECONDMOONMOD_HYDRA";

    public override string ItemPickupDesc => "You hit two extra times. Your damage is modified accordingly.";

    public override string ItemFullDesc => $"You hit <color=#7CFDEA>2 extra times</color>. Your damage on hit is <style=cIsDamage>{HydraBaseDamageInit * 100}%</style> <style=cStack>(+{HydraBaseDamageStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

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
        if (damageInfo.attacker && damageInfo.procCoefficient > 0)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        var teamComponent = attackerBody.GetComponent<TeamComponent>();
                        var victimBody = victim ? victim.GetComponent<CharacterBody>() : null;

                        HydraOrb hydraOrb = new HydraOrb
                        {
                            origin = damageInfo.position,
                            damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, 1f),
                            isCrit = damageInfo.crit,
                            teamIndex = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.inflictor,
                            procCoefficient = damageInfo.procCoefficient,
                            damageColorIndex = damageInfo.damageColorIndex,
                            procChainMask = damageInfo.procChainMask,
                            damageType = damageInfo.damageType,
                        };
                        HurtBox mainHurtBox2 = victimBody?.mainHurtBox;
                        if ((bool)mainHurtBox2)
                        {
                            hydraOrb.target = mainHurtBox2;
                            OrbManager.instance.AddOrb(hydraOrb);
                        }
                        DamageInfo newDamageInfo = new()
                        {
                            damage = hydraOrb.damageValue,
                            crit = hydraOrb.isCrit,
                            inflictor = damageInfo.inflictor,
                            attacker = hydraOrb.attacker,
                            position = damageInfo.position,
                            force = damageInfo.force,
                            rejected = damageInfo.rejected,
                            procChainMask = damageInfo.procChainMask,
                            procCoefficient = damageInfo.procCoefficient,
                            damageType = hydraOrb.damageType,
                            damageColorIndex = hydraOrb.damageColorIndex
                        };
                        orig(self, newDamageInfo, victim);
                        GlobalEventManager.instance.OnHitAll(newDamageInfo, victim);
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private void HydraSetDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (damageInfo.attacker)
        {
            if (damageInfo.attacker.GetComponent<CharacterBody>())
            {
                var stackCount = GetCount(damageInfo.attacker.GetComponent<CharacterBody>());
                if (stackCount > 0 && damageInfo.procCoefficient > 0)
                {
                    damageInfo.damage *= HydraBaseDamageInit + ((stackCount - 1) * HydraBaseDamageStack);
                }
            }
        }
        orig(self, damageInfo);
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
        HydraBaseDamageInit = config.ActiveBind("Item: " + ItemName, "Damage modifier with one " + ItemName, 0.4f, "How much should damage be set to on hits with proc coefficient greater than zero with one " + ItemName + "? (0.4 = 40%)");
        HydraBaseDamageStack = config.ActiveBind("Item: " + ItemName, "Damage modifier per stack after one " + ItemName, 0.1f, "How much should damage be increased for hits with proc coefficient greater than zero per stack of " + ItemName + " after one? (0.1 = 10%)");
    }
}
