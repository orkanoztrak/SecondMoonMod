using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;


namespace SecondMoon.Items.Tier3.SilverRevolver;

public class SilverRevolver : Item<SilverRevolver>
{
    public static ConfigOption<float> SilverRevolverCDRInit;
    public static ConfigOption<float> SilverRevolverCDRStack;
    public static ConfigOption<float> SilverRevolverInitialCritChance;

    public override string ItemName => "Silver Revolver";

    public override string ItemLangTokenName => "SILVER_REVOLVER";

    public override string ItemPickupDesc => "You always critically strike against Elite monsters, and critical strikes lower cooldowns.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{SilverRevolverInitialCritChance}% critical chance</style>. " +
        $"You always <style=cIsDamage>critically strike</style> against elites. " +
        $"<style=cIsDamage>Critical strikes</style> <style=cIsUtility>reduce skill cooldowns by {SilverRevolverCDRInit}s</style> <style=cStack>(+{SilverRevolverCDRStack}s per stack)</style>.";

    public override string ItemLore => "Item Log: 62774\r\n\r\n" +
        "Identification: Silver Revolver\r\n\r\n" +
        "Notes:\r\n\r\n" +
        "Ancient firearm found in one of the dig sites down south.\r\n\r\n" +
        "The archaeology team believe with almost certainty that it was used in the second civil war. The design fits the time period too.\r\n\r\n" +
        "Alongside the usual detritus of passing time, we found traces of gunpowder and marks left by bullets leaving the chamber, so it was most likely a practical piece, rather than ceremonial.\r\n\r\n" +
        "No other equivalent from its time period have yet been found, so storage safety is PARAMOUNT.\r\n\r\n" +
        "Only to be handled by the archaeology team. Don't let the guides touch; especially Jeremy.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamageProcess += SilverRevolverAlwaysCritsOnElite;
        RecalculateStatsAPI.GetStatCoefficients += SilverRevolverInitBuffCrit;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += SilverRevolverReduceCooldowns;
    }

    private void SilverRevolverReduceCooldowns(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.crit)
        {
            if (damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    var stackCount = GetCount(attackerBody);
                    if (stackCount > 0)
                    {
                        var cdr = SilverRevolverCDRInit + (stackCount - 1) * SilverRevolverCDRStack;
                        cdr *= damageInfo.procCoefficient < 1 ? damageInfo.procCoefficient : 1;
                        attackerBody.skillLocator.primary.rechargeStopwatch += cdr;
                        attackerBody.skillLocator.secondary.rechargeStopwatch += cdr;
                        attackerBody.skillLocator.utility.rechargeStopwatch += cdr;
                        attackerBody.skillLocator.special.rechargeStopwatch += cdr;
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private void SilverRevolverInitBuffCrit(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += SilverRevolverInitialCritChance;
        }
    }

    private void SilverRevolverAlwaysCritsOnElite(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (self.body && damageInfo.attacker)
        {
            if (!self.body.isElite)
            {
                orig(self, damageInfo);
            }
            else
            {
                var body = damageInfo.attacker.GetComponent<CharacterBody>();
                if (body)
                {
                    var stackCount = GetCount(body);
                    if (stackCount > 0)
                    {
                        damageInfo.crit = true;
                    }
                }
                orig(self, damageInfo);
            }
        }
        else
        {
            orig(self, damageInfo);
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
        SilverRevolverCDRInit = config.ActiveBind("Item: " + ItemName, "Cooldown reduction with one " + ItemName, 0.5f, "How many seconds should cooldowns be reduced by with one " + ItemName + "?");
        SilverRevolverCDRStack = config.ActiveBind("Item: " + ItemName, "Cooldown reduction per stack after one " + ItemName, 0.5f, "How many seconds should cooldowns be reduced by per stack of " + ItemName + " after one ?");
        SilverRevolverInitialCritChance = config.ActiveBind("Item: " + ItemName, "Critical chance with at least one " + ItemName, 5f, "By what % should critical chance be increased by with at least one " + ItemName + "?");
    }
}
