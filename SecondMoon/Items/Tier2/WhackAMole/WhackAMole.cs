using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier2.WhackAMole;

internal class WhackAMole : Item<WhackAMole>
{
    public static ConfigOption<float> WhackAMoleProcChanceInit;
    public static ConfigOption<float> WhackAMoleProcChanceStack;

    public static ConfigOption<float> WhackAMoleDamage;
    public static ConfigOption<float> WhackAMoleRadius;

    public override string ItemName => "Whack-A-Mole Hammer";

    public override string ItemLangTokenName => "WHACKAMOLE";

    public override string ItemPickupDesc => "Your non-critical hits have a chance to explode!";

    public override string ItemFullDesc => $"<style=cIsDamage>{WhackAMoleProcChanceInit}%</style> <style=cStack>(+{WhackAMoleProcChanceStack}% per stack)</style> chance for <style=cIsDamage>non-critical hit</style> " +
        $"to <style=cIsDamage>explode</style> in a <style=cIsDamage>{WhackAMoleRadius}m</style> radius for a bonus <style=cIsDamage>{WhackAMoleDamage * 100}%</style> TOTAL damage to nearby enemies. " +
        $"Chance increases with <style=cIsDamage>critical chance</style> to try to ensure the overall odds stay the same.";

    public override string ItemLore => "\"Jeremy.\"\r\n\r\n" +
        "\"Yeah?\"\r\n\r\n" +
        "\"Don't you \"yeah\" me. You know what I'm talking about.\"\r\n\r\n" +
        "\"I see no problems with it. I don't get why you're so upset about this.\"\r\n\r\n" +
        "\"First of all, it's an ancient artifact, you can't just give it to some rando. Second of all, I don't have a clue how you actually got someone to get on board with this. Third of all, I have even less of a clue how you got the idea of \"what if it went\", and I quote, \"BOOMNK!\". Fourth of all-\"\r\n\r\n" +
        "\"Don't you at least wonder how we got it working?\"";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += WhackAMoleExplode;
    }

    private void WhackAMoleExplode(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        if (!damageInfo.crit && damageInfo.procCoefficient > 0)
        {
            if (damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    var attacker = attackerBody.master;
                    if (attacker)
                    {
                        var stackCount = GetCount(attacker);
                        if (stackCount > 0)
                        {
                            var finalChance = WhackAMoleProcChanceInit + ((stackCount - 1) * WhackAMoleProcChanceStack);
                            finalChance = attackerBody.crit < 100 ? finalChance / (100 - attackerBody.crit) * 100 : 100;
                            if (Util.CheckRoll(finalChance * damageInfo.procCoefficient, attacker))
                            {
                                var baseDamage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, WhackAMoleDamage);
                                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"), new EffectData
                                {
                                    origin = damageInfo.position,
                                    scale = WhackAMoleRadius,
                                    rotation = Util.QuaternionSafeLookRotation(damageInfo.force)
                                }, transmit: true);
                                new BlastAttack()
                                {
                                    position = damageInfo.position,
                                    baseDamage = baseDamage,
                                    baseForce = 0f,
                                    radius = WhackAMoleRadius,
                                    attacker = damageInfo.attacker,
                                    inflictor = null,
                                    teamIndex = TeamComponent.GetObjectTeam(damageInfo.attacker),
                                    crit = damageInfo.crit,
                                    procChainMask = damageInfo.procChainMask,
                                    procCoefficient = 0f,
                                    damageColorIndex = DamageColorIndex.Item,
                                    falloffModel = BlastAttack.FalloffModel.None,
                                    damageType = DamageType.Generic
                                }.Fire();
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
        WhackAMoleDamage = config.ActiveBind("Item: " + ItemName, "Damage of the proc", 1f, "What % of TOTAL damage should the proc do? (1 = 100%)");
        WhackAMoleRadius = config.ActiveBind("Item: " + ItemName, "Radius of the proc", 6f, "The explosion proc will have a radius of this many meters.");

        WhackAMoleProcChanceInit = config.ActiveBind("Item: " + ItemName, "Proc chance with one " + ItemName, 10f, "What % of non-critical hits should proc with one " + ItemName + "?");
        WhackAMoleProcChanceStack = config.ActiveBind("Item: " + ItemName, "Proc chance per stack after one " + ItemName, 10f, "What % of non-critical hits should proc per stack of " + ItemName + " after one ?");
    }
}
