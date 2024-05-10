using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;


namespace SecondMoon.Items.Tier3.SilverRevolver;

public class SilverRevolver : Item<SilverRevolver>
{
    public static ConfigOption<float> SilverRevolverCDRInit;
    public static ConfigOption<float> SilverRevolverCDRStack;
    public static ConfigOption<float> SilverRevolverInitialCritChance;

    public override string ItemName => "Silver Revolver";

    public override string ItemLangTokenName => "SECONDMOONMOD_SILVER_REVOLVER";

    public override string ItemPickupDesc => "You always critically strike against Elite monsters, and critical strikes lower cooldowns.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{SilverRevolverInitialCritChance}% critical chance</style>. " +
        $"You always <style=cIsDamage>critically strike</style> against Elite monsters. " +
        $"<style=cIsDamage>Critical strikes</style> <style=cIsUtility>reduce skill cooldowns by {SilverRevolverCDRInit}s</style> <style=cStack>(+{SilverRevolverCDRStack}s per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamage += SilverRevolverAlwaysCritsOnElite;
        GetStatCoefficients += SilverRevolverInitBuffCrit;
        On.RoR2.GlobalEventManager.OnHitEnemy += SilverRevolverReduceCooldowns;
    }

    private void SilverRevolverReduceCooldowns(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
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

    private void SilverRevolverInitBuffCrit(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += SilverRevolverInitialCritChance;
        }
    }

    private void SilverRevolverAlwaysCritsOnElite(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
    {
        if (self.body)
        {
            if (!self.body.isElite)
            {
                orig(self, damageInfo);
            }
            else
            {
                var newDamageInfo = damageInfo;
                var body = newDamageInfo.attacker.GetComponent<CharacterBody>();
                if (body)
                {
                    var stackCount = GetCount(body);
                    if (stackCount > 0)
                    {
                        damageInfo.crit = true;
                    }
                }
                orig(self, newDamageInfo);
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
        SilverRevolverCDRInit = config.ActiveBind("Item: " + ItemName, "Cooldown reduction with one " + ItemName, 0.5f, "How many seconds should cooldowns be reduced by with one Silver Revolver?");
        SilverRevolverCDRStack = config.ActiveBind("Item: " + ItemName, "Cooldown reduction per stack after one " + ItemName, 0.5f, "How many seconds should cooldowns be reduced by per stack of Silver Revolver after one ?");
        SilverRevolverInitialCritChance = config.ActiveBind("Item: " + ItemName, "Critical chance with at least one " + ItemName, 5f, "By what % should critical chance be increased by with at least one Silver Revolver?");
    }
}
