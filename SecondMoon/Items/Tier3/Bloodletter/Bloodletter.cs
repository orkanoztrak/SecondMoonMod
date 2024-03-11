using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace SecondMoon.Items.Tier3.Bloodletter;

public class Bloodletter : Item<Bloodletter>
{
    public static float BloodletterProcChance = 10f;
    public static float BloodletterDamage = 2f;
    public static float BloodletterDamageScalingInit = 1f;
    public static float BloodletterDamageScalingStack = 1f;
    public static float BloodletterDamageScalingThreshold = 1f;

    public static float BloodletterGash = 1f;
    public static float BloodletterGashDuration = 2f;
    public static float BloodletterGashScalingInit = 0.5f;
    public static float BloodletterGashScalingStack = 0.5f;
    public static float BloodletterGashScalingThreshold = 1f;

    public override string ItemName => "Bloodletter";

    public override string ItemLangTokenName => "BLOODLETTER";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

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
        On.RoR2.GlobalEventManager.OnHitEnemy += BloodletterWoundEnemy;
    }

    private void BloodletterWoundEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        if (damageInfo.attacker)
        {
            var attacker = damageInfo.attacker.GetComponent<CharacterMaster>();
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimComponent = victim.GetComponent<HealthComponent>();
            if (attacker && attackerBody && victimComponent)
            {
                var stackCount = GetCount(attacker);
                if (stackCount > 0)
                {
                    if (Util.CheckRoll(BloodletterProcChance * damageInfo.procCoefficient, attacker))
                    {
                        DamageInfo bloodletterProc = new DamageInfo
                        {
                            damage = BloodletterDamage + (damageInfo.damage / attackerBody.damage / BloodletterDamageScalingThreshold * (BloodletterDamageScalingInit + ((stackCount - 1) * BloodletterDamageScalingStack))),
                            damageColorIndex = DamageColorIndex.Item,
                            damageType = DamageType.Generic,
                            attacker = damageInfo.attacker,
                            crit = damageInfo.crit,
                            force = Vector3.zero,
                            inflictor = null,
                            position = damageInfo.position,
                            procCoefficient = 0
                        };

                        InflictDotInfo ınflictDotInfo = default(InflictDotInfo);
                        ınflictDotInfo.victimObject = victim;
                        ınflictDotInfo.attackerObject = damageInfo.attacker;
                        ınflictDotInfo.dotIndex = Gash.instance.DotIndex;
                        ınflictDotInfo.duration = BloodletterGashDuration;
                        ınflictDotInfo.damageMultiplier = BloodletterGash + (damageInfo.damage / attackerBody.damage / BloodletterGashScalingThreshold * (BloodletterGashScalingInit + ((stackCount - 1) * BloodletterGashScalingStack)));

                        EffectManager.SimpleImpactEffect(RoR2.HealthComponent.AssetReferences.executeEffectPrefab, bloodletterProc.position, Vector3.up, transmit: true);
                        victimComponent.TakeDamage(bloodletterProc);
                        DotController.InflictDot(ref ınflictDotInfo);
                    }
                }
            }
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
