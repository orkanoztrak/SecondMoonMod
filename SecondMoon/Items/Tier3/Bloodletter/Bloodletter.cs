using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace SecondMoon.Items.Tier3.Bloodletter;

public class Bloodletter : Item<Bloodletter>
{
    public static ConfigOption<float> BloodletterProcChance;
    public static ConfigOption<float> BloodletterDamageInit;
    public static ConfigOption<float> BloodletterDamageStack;
    public static ConfigOption<float> BloodletterDamageScalingInit;
    public static ConfigOption<float> BloodletterDamageScalingStack;
    public static ConfigOption<float> BloodletterProcChanceScaling;

    public override string ItemName => "Bloodletter";

    public override string ItemLangTokenName => "SECONDMOONMOD_BLOODLETTER";

    public override string ItemPickupDesc => $"Chance on hit to wound the enemy. The stronger the hit, the better this works.";

    public override string ItemFullDesc => $"Hits have a <style=cIsDamage>{BloodletterProcChance}%</style> chance to deal <style=cIsDamage>{BloodletterDamageInit * 100}%</style> <style=cStack>(+{BloodletterDamageStack * 100} per stack)</style> TOTAL damage and apply <color=#C14040>Gash</color> for half the damage this item inflicts." +
        $" <color=#C14040>At certain damage % thresholds, this item's effects become stronger</color>:\r\n\r\n" +
        $"• {2 * 100}%: <style=cIsDamage>+{BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {3 * 100}%: <style=cIsDamage>+{2 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{2 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {4 * 100}%: <style=cIsDamage>+{3 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{3 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage.\r\n" +
        $"• {6 * 100}%: <style=cIsDamage>+{3 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{3 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + BloodletterProcChanceScaling}%</style> chance for proc.\r\n" +
        $"• {8 * 100}%: <style=cIsDamage>+{4 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{4 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + BloodletterProcChanceScaling}%</style> chance for proc.\r\n" +
        $"• {10 * 100}%: <style=cIsDamage>+{4 * BloodletterDamageScalingInit * 100}%</style> <style=cStack>(+{4 * BloodletterDamageScalingStack * 100}% per stack)</style> extra damage. <style=cIsDamage>{BloodletterProcChance + 2 * BloodletterProcChanceScaling}%</style> chance for proc.";

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
        On.RoR2.GlobalEventManager.OnHitEnemy += BloodletterWoundEnemy;
    }

    private void BloodletterWoundEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimComponent = victim.GetComponent<HealthComponent>();
            if (attackerBody && victimComponent)
            {
                var attacker = attackerBody.master;
                if (attacker)
                {
                    var stackCount = GetCount(attacker);
                    if (stackCount > 0)
                    {
                        float finalChance = BloodletterProcChance;
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
                            EffectManager.SimpleImpactEffect(HealthComponent.AssetReferences.executeEffectPrefab, bloodletterProc.position, Vector3.up, transmit: true);
                            victimComponent.TakeDamage(bloodletterProc);
                            DotController.InflictDot(ref ınflictDotInfo);
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
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
        BloodletterProcChance = config.ActiveBind("Item: " + ItemName, "Proc chance", 10f, "The % chance of hits proccing this.");
        BloodletterDamageInit = config.ActiveBind("Item: " + ItemName, "Damage of the proc with one " + ItemName, 2f, "What % of TOTAL damage should the proc do with one " + ItemName + "? (2 = 200%)");
        BloodletterDamageStack = config.ActiveBind("Item: " + ItemName, "Damage of the proc per stack after one " + ItemName, 2f, "What % of TOTAL damage should be added to the proc per stack of " + ItemName + " after one? (2 = 200%)");
        BloodletterDamageScalingInit = config.ActiveBind("Item: " + ItemName, "Damage scaling of thresholds for stronger hits with one " + ItemName, 0.5f, "How much should the respective damage thresholds boost proc damage by with one " + ItemName + "? (0.5 = 50%)");
        BloodletterDamageScalingStack = config.ActiveBind("Item: " + ItemName, "Damage scaling of thresholds for stronger hits per stack after one " + ItemName, 0.5f, "How much should the respective damage thresholds boost proc damage by per stack of " + ItemName + " after one? (0.5 = 50%)");
        BloodletterProcChanceScaling = config.ActiveBind("Item: " + ItemName, "Proc chance scaling of thresholds for stronger hits", 5f, "What % should proc chance be increased by at respective damage thresholds?");
    }

    public float HandleDamageCalc(float totalDamage, float baseDamage, float stackCount)
    {
        var hitPercent = (int)(totalDamage / baseDamage);
        var scalingIncludingStacks = BloodletterDamageScalingInit + ((stackCount - 1) * BloodletterDamageScalingStack);
        var damageIncludingStacks = BloodletterDamageInit + (stackCount - 1) * BloodletterDamageStack;
        if (hitPercent <= 1)
        {
            return damageIncludingStacks;
        }
        if (hitPercent < 4)
        {
            return damageIncludingStacks + ((hitPercent - 1) * scalingIncludingStacks);
        }
        if (hitPercent < 8)
        {
            return damageIncludingStacks + (3 * scalingIncludingStacks);
        }
        if (hitPercent >= 8)
        {
            return damageIncludingStacks + (4 * scalingIncludingStacks);
        }
        return 0f;
    }
}
