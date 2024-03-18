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
    public static float BloodletterDamageScalingInit = 0.5f;
    public static float BloodletterDamageScalingStack = 0.25f;
    public static float BloodletterProcChanceScaling = 5f;
    public static float BloodletterGashDuration = 3f;

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
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var attacker = attackerBody.master;
            var victimComponent = victim.GetComponent<HealthComponent>();
            if (attacker && attackerBody && victimComponent && victim != damageInfo.attacker)
            {
                var stackCount = GetCount(attacker);
                if (stackCount > 0)
                {
                    var finalChance = BloodletterProcChance;
                    if (damageInfo.damage / attackerBody.damage >= 6f)
                    {
                        finalChance += BloodletterProcChanceScaling;
                        if (damageInfo.damage / attackerBody.damage >= 10f)
                        {
                            finalChance += BloodletterProcChanceScaling;
                        }
                    }
                    if (Util.CheckRoll(finalChance * damageInfo.procCoefficient, attacker))
                    {
                        var procDamage = HandleDamageCalc(damageInfo.damage, attackerBody.damage, stackCount);
                        DamageInfo bloodletterProc = new DamageInfo
                        {
                            damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, procDamage),
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
                        ınflictDotInfo.damageMultiplier = damageInfo.damage / attackerBody.damage * procDamage / 2;
                        if (bloodletterProc.crit)
                        {
                            ınflictDotInfo.damageMultiplier *= attackerBody.critMultiplier;
                        }
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

    public float HandleDamageCalc(float totalDamage, float baseDamage, float stackCount)
    {
        var hitPercent = (int)(totalDamage / baseDamage);
        var scalingIncludingStacks = BloodletterDamageScalingInit + ((stackCount - 1) * BloodletterDamageScalingStack);
        if (hitPercent <= 1)
        {
            return BloodletterDamage;
        }
        if (hitPercent < 4)
        {
            return BloodletterDamage + ((hitPercent - 1) * scalingIncludingStacks);
        }
        if (hitPercent < 8)
        {
            return BloodletterDamage + (3 * scalingIncludingStacks);
        }
        if (hitPercent >= 8)
        {
            return BloodletterDamage + (4 * scalingIncludingStacks);
        }
        return 0f;
    }
}
