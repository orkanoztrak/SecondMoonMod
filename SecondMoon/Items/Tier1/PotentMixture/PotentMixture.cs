using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier1.PotentMixture;

public class PotentMixture : Item<PotentMixture>
{
    public static ConfigOption<float> PotentMixtureExtensionInit;
    public static ConfigOption<float> PotentMixtureExtensionStack;

    public override string ItemName => "Potent Mixture";

    public override string ItemLangTokenName => "POTENT_MIXTURE";

    public override string ItemPickupDesc => $"Your damage over time effects last longer.";

    public override string ItemFullDesc => $"Your <style=cIsDamage>damage over time effects</style> last <style=cIsDamage>{PotentMixtureExtensionInit * 100}%</style> <style=cStack>(+{PotentMixtureExtensionStack * 100}% per stack)</style> longer. " +
        $"<style=cIsDamage>Collapse</style> instead does <style=cIsDamage>{PotentMixtureExtensionInit * 100}%</style> <style=cStack>(+{PotentMixtureExtensionStack * 100}% per stack)</style> more damage.";

    public override string ItemLore => "\"Are you certain that these are the right amounts?\"\r\n\r\n" +
        "\"Definitely. I checked the formula twice, thrice even.\"\r\n\r\n" +
        "\"Alright, what can go wrong?\"\r\n\r\n" +
        "----\r\n\r\n" +
        "\"So who's going to try it?\"\r\n\r\n" +
        "\"Not me, Charlie. You've been standing around doing nothing. You do it.\"\r\n\r\n" +
        "\"I looked at the formula!\"\r\n\r\n" +
        "\"And? What else?\"\r\n\r\n" +
        "\"Ugh, fine.\"\r\n\r\n" +
        "----\r\n\r\n" +
        "\"So, do you feel any different?\"\r\n\r\n" +
        "\"I don't think so. Wait. What the hell? - Do you remember that candle I lit like half an hour ago?\"\r\n\r\n" +
        "\"What does that have to do with anything?\"\r\n\r\n" +
        "\"Well, look at it! The candle is gone but the fire is still burning.\"\r\n\r\n" +
        "\"What on-\"";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.DotController.AddDot += PotentMixtureExtendDots;
    }

    private void PotentMixtureExtendDots(On.RoR2.DotController.orig_AddDot orig, RoR2.DotController self, GameObject attackerObject, float duration, RoR2.DotController.DotIndex dotIndex, float damageMultiplier, uint? maxStacksFromAttacker, float? totalDamage, RoR2.DotController.DotIndex? preUpgradeDotIndex)
    {
        var newDuration = duration;
        float? newTotalDamage = totalDamage;
        float increase;
        var attacker = attackerObject.GetComponent<CharacterBody>();
        var newDamageMultiplier = damageMultiplier;
        if (attacker)
        {
            var stackCount = GetCount(attacker);
            if (stackCount > 0)
            {
                increase = (float)(1 - ((1 - PotentMixtureExtensionInit) * Math.Pow(1 - PotentMixtureExtensionStack, stackCount - 1)));
                if (!(dotIndex == DotController.DotIndex.Fracture))
                {
                    if (totalDamage.HasValue)
                    {
                        newTotalDamage = totalDamage * (1 + increase);
                    }
                    else
                    {
                        newDuration = duration * (1 + increase);
                    }
                }
                else
                {
                    newDamageMultiplier = damageMultiplier * (1 + increase);
                }
            }
        }
        orig(self, attackerObject, newDuration, dotIndex, newDamageMultiplier, maxStacksFromAttacker, newTotalDamage, preUpgradeDotIndex);
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
        PotentMixtureExtensionInit = config.ActiveBind("Item: " + ItemName, "DOT timer extension with one " + ItemName, 0.2f, "How much should damage over time effect timers be extended by with one " + ItemName + "? Collapse's damage will be increased by this value instead. (0.2 = 20%)");
        PotentMixtureExtensionStack = config.ActiveBind("Item: " + ItemName, "DOT timer extension per stack after one " + ItemName, 0.2f, "How much should damage over time effect timers be extended by per stack of " + ItemName + " after one? Collapse's damage will be increased by this value instead. (0.2 = 20%)");
    }
}