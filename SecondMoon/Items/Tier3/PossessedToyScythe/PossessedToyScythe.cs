using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier3.PossessedToyScythe;

internal class PossessedToyScythe : Item<PossessedToyScythe>
{
    public static ConfigOption<float> PossessedToyScytheProcChanceInit;
    public static ConfigOption<float> PossessedToyScytheProcChanceStack;

    public static ConfigOption<float> PossessedToyScytheFixedDamage;
    public static ConfigOption<float> PossessedToyScythePercentHealthDamage;


    public override string ItemName => "Possessed Toy Scythe";

    public override string ItemLangTokenName => "POSSESSED_TOY_SCYTHE";

    public override string ItemPickupDesc => "Your non-critical hits have a chance to deal extra damage based on target health.";

    public override string ItemFullDesc => $"<style=cIsDamage>{PossessedToyScytheProcChanceInit}%</style> <style=cStack>(+{PossessedToyScytheProcChanceStack}% per stack)</style> chance on <style=cIsDamage>non-critical hit</style> " +
        $"to deal <style=cIsDamage>{PossessedToyScytheFixedDamage * 100}%</style> TOTAL damage or damage equal to <style=cIsHealth>{PossessedToyScythePercentHealthDamage * 100}% of the target's maximum health</style>, whichever is less. " +
        $"Chance increases with <style=cIsDamage>critical chance</style> to try to ensure the overall odds stay the same.";

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
        On.RoR2.GlobalEventManager.ProcessHitEnemy += PossessedToyScytheReap;
    }

    private void PossessedToyScytheReap(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        if (!damageInfo.crit && damageInfo.procCoefficient > 0)
        {
            if (damageInfo.attacker)
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
                            var finalChance = PossessedToyScytheProcChanceInit + ((stackCount - 1) * PossessedToyScytheProcChanceStack);
                            finalChance = attackerBody.crit < 100 ? finalChance / (100 - attackerBody.crit) * 100 : 100;
                            if (Util.CheckRoll(finalChance * damageInfo.procCoefficient, attacker))
                            {
                                var procHit = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, PossessedToyScytheFixedDamage);
                                var healthDamageCheck = damageInfo.damage / attackerBody.damage > 1 ? 1 : damageInfo.damage / attackerBody.damage;
                                var healthHit = victimComponent.fullCombinedHealth * PossessedToyScythePercentHealthDamage * healthDamageCheck;
                                var finalDamage = procHit > healthHit ? healthHit : procHit;
                                DamageInfo scytheProc = new DamageInfo
                                {
                                    damage = finalDamage,
                                    damageColorIndex = DamageColorIndex.Item,
                                    damageType = DamageType.Generic,
                                    attacker = damageInfo.attacker,
                                    crit = damageInfo.crit,
                                    force = Vector3.zero,
                                    inflictor = null,
                                    position = damageInfo.position,
                                    procCoefficient = 0
                                };
                                EffectManager.SimpleImpactEffect(HealthComponent.AssetReferences.executeEffectPrefab, scytheProc.position, Vector3.up, transmit: true);
                                victimComponent.TakeDamage(scytheProc);
                            }
                        }
                    }
                }
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
        PossessedToyScythePercentHealthDamage = config.ActiveBind("Item: " + ItemName, "Percent health damage", 0.1f, "What % of maximum combined health should the proc do as damage? This value scales down with on hit damage if the proccing hit has less than 100%. (0.1 = 10%)");
        PossessedToyScytheFixedDamage = config.ActiveBind("Item: " + ItemName, "Damage of the proc at high health", 9f, "What % of TOTAL damage should the proc do? This only happens if the health check is greater than this value. (9 = 900%)");

        PossessedToyScytheProcChanceInit = config.ActiveBind("Item: " + ItemName, "Proc chance with one " + ItemName, 10f, "What % of non-critical hits should proc with one " + ItemName + "?");
        PossessedToyScytheProcChanceStack = config.ActiveBind("Item: " + ItemName, "Proc chance per stack after one " + ItemName, 10f, "What % of non-critical hits should proc per stack of " + ItemName + " after one ?");
    }
}
