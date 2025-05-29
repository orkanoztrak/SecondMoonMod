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

    public override string ItemLangTokenName => "KNIT_CLOVER";

    public override string ItemPickupDesc => "Your non-critical hits have a chance to have a higher proc coefficient.";

    public override string ItemFullDesc => $"<style=cIsDamage>{KnitCloverProcChanceInit}%</style> <style=cStack>(+{KnitCloverProcChanceStack}% per stack)</style> chance for <style=cIsDamage>non-critical hits</style> " + 
        $"to have their <style=cIsUtility>proc coefficient</style> increased by <style=cIsUtility>{KnitCloverProcCoefficientBoost * 100f}%</style>. " + 
        $"Chance increases with <style=cIsDamage>critical chance</style> to try to ensure the overall odds stay the same.";

    public override string ItemLore => "Two outlaws sat around a campfire.\r\n\r\n" +
        "They didn't know one another. They just happened upon each other by chance.\r\n\r\n" +
        "One was a man, the other a woman. The man was holding a cup of coffee in his hand, taking rare but rich sips from it. The woman was looking at a trinket knitted for her by her niece.\r\n\r\n" +
        "City lights gleamed in the distance as the two mercenaries sat, silently.\r\n\r\n" +
        "The man was observing the woman, and she knew. He did little to hide it.\r\n\r\n" +
        "\"How lucky for you to have met me here.\" He had said an hour before and she had told him she had never needed luck. She didn't.\r\n\r\n" +
        "They kept sitting silently.\r\n\r\n" +
        "She took a bite out of a rabbit she had been roasting on the fire.\r\n\r\n" +
        "He was still studying her. She knew something was up.\r\n\r\n" +
        "His finger twitched.\r\n\r\n" +
        "She drew her revolver, she shot.\r\n\r\n" +
        "He was dead, she was alive.\r\n\r\n" +
        "After all, she had never needed luck.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamageProcess += KnitCloverLuck;
    }

    private void KnitCloverLuck(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
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