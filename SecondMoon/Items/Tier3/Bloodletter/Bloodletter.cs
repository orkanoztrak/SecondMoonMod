using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;
using System;
using System.Collections.Generic;
using System.Drawing;
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

    public override string ItemName => "Bloodletter";

    public override string ItemLangTokenName => "SECONDMOONMOD_BLOODLETTER";

    public override string ItemPickupDesc => $"Chance on hit to wound the enemy. The stronger the hit, the better this works.";

    public override string ItemFullDesc => $"Hits have a <style=cIsDamage>{BloodletterProcChance}%</style> chance to deal <style=cIsDamage>{BloodletterDamage * 100}%</style> TOTAL damage and apply <color=#{System.Drawing.Color.Aqua}>Gash</color> for half the damage this item inflicts." +
        $" <color=#C14040>At certain damage % thresholds, this item's effects become stronger</color>:\r\n\r\n" + 
        $"• {2 * 100}%: <style=cIsDamage>+{BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {3 * 100}%: <style=cIsDamage>+{2 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{2 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {4 * 100}%: <style=cIsDamage>+{3 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{3 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {6 * 100}%: <style=cIsDamage>+{3 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{3 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + BloodletterProcChanceScaling}%</style> chance for proc.\r\n" +
        $"• {8 * 100}%: <style=cIsDamage>+{4 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{4 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + BloodletterProcChanceScaling}%</style> chance for proc.\r\n" +
        $"• {10 * 100}%: <style=cIsDamage>+{4 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{4 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + 2 * BloodletterProcChanceScaling}%</style> chance for proc.\r\n";

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
                    if (damageInfo.damage / attackerBody.damage >= 6)
                    {
                        finalChance += BloodletterProcChanceScaling;
                        if (damageInfo.damage / attackerBody.damage >= 10)
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
                        ınflictDotInfo.damageMultiplier = 1;
                        ınflictDotInfo.totalDamage = bloodletterProc.damage / 2;
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
