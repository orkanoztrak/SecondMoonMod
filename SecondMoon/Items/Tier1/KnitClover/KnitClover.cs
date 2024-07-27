using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace SecondMoon.Items.Tier1.KnitClover;

public class KnitClover : Item<KnitClover>
{
    public static ConfigOption<float> KnitCloverProcChanceInit;
    public static ConfigOption<float> KnitCloverProcChanceStack;

    public static ConfigOption<float> KnitCloverProcCoefficientBoost;

    public override string ItemName => "Knit Clover";

    public override string ItemLangTokenName => "SECONDMOONMOD_KNIT_CLOVER";

    public override string ItemPickupDesc => "Your non-critical hits have a chance to have a higher proc coefficient.";

    public override string ItemFullDesc => $"<style=cIsDamage>{KnitCloverProcChanceInit}%</style> <style=cStack>(+{KnitCloverProcChanceStack}% per stack)</style> chance for <style=cIsDamage>non-critical hits</style> " + 
        $"to have their <style=cIsUtility>proc coefficient</style> increased by <style=cIsUtility>{KnitCloverProcCoefficientBoost * 100f}%</style>. " + 
        $"Chance increases with <style=cIsDamage>critical chance</style> to try to ensure the overall odds stay the same.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamage += KnitCloverLuck;
    }

    private void KnitCloverLuck(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (!damageInfo.crit && damageInfo.procCoefficient > 0f && damageInfo.attacker)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimBody = self.body;
            if (attackerBody && victimBody)
            {
                var vector = attackerBody.corePosition - damageInfo.position;
                if (!(attackerBody.canPerformBackstab && (damageInfo.damageType & DamageType.DoT) != DamageType.DoT && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-vector, self.body)))) 
                {
                    var attacker = attackerBody.master;
                    if (attacker)
                    {
                        var stackCount = GetCount(attacker);
                        if (stackCount > 0)
                        {
                            var finalChance = KnitCloverProcChanceInit + (stackCount - 1) * KnitCloverProcChanceStack;
                            finalChance = attackerBody.crit < 100 ? finalChance / (100 - attackerBody.crit) * 100 : 100;
                            if (Util.CheckRoll(finalChance * damageInfo.procCoefficient, attacker))
                            {
                                damageInfo.procCoefficient *= 1f + (float)KnitCloverProcCoefficientBoost;
                            }
                        }
                    }
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
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        KnitCloverProcCoefficientBoost = config.ActiveBind("Item: " + ItemName, "Proc coefficient boost", 0.33f, "By what % should proc coefficient be increased? (0.33f = 33%)");
        KnitCloverProcChanceInit = config.ActiveBind("Item: " + ItemName, "Proc chance with one " + ItemName, 10f, "What % of non-critical hits should proc with one " + ItemName + "?");
        KnitCloverProcChanceStack = config.ActiveBind("Item: " + ItemName, "Proc chance per stack after one " + ItemName, 10f, "What % of non-critical hits should proc per stack of " + ItemName + " after one ?");
    }
}