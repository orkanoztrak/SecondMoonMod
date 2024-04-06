using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Tier1.PotentMixture;

public class PotentMixture : Item<PotentMixture>
{
    public static float PotentMixtureExtensionInit = 0.2f;
    public static float PotentMixtureExtensionStack = 0.2f;

    public override string ItemName => "Potent Mixture";

    public override string ItemLangTokenName => "SECONDMOONMOD_POTENT_MIXTURE";

    public override string ItemPickupDesc => $"Your damage over time effects last longer.";

    public override string ItemFullDesc => $"Your <style=cIsDamage>damage over time</style> effects last <style=cIsDamage>{PotentMixtureExtensionInit * 100}%</style> <style=cStack>(+{PotentMixtureExtensionStack * 100}% per stack)</style> longer. <style=cIsDamage>Collapse</style> instead does <style=cIsDamage>{PotentMixtureExtensionInit * 100}%</style> <style=cStack>(+{PotentMixtureExtensionStack * 100}% per stack)</style> more damage.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier1;

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
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

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}