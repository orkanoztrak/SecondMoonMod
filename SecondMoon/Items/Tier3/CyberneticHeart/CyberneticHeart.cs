using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Items.Tier3.CyberneticHeart;

public class CyberneticHeart : Item<CyberneticHeart>
{
    public static float CyberneticHeartShieldInit = 0.2f;
    public static float CyberneticHeartShieldStack = 0.2f;

    public override string ItemName => "Cybernetic Heart";

    public override string ItemLangTokenName => "SECONDMOON_CYBERNETIC_HEART";

    public override string ItemPickupDesc => "Gain a recharging shield. Your healing restores shields as well.";

    public override string ItemFullDesc => $"Gain a shield equal to <style=cIsHealing>{CyberneticHeartShieldInit * 100}%</style> <style=cStack>(+{CyberneticHeartShieldStack * 100}% per stack)</style> of your maximum health. " +
        $"Your <style=cIsHealing>healing</style> restores an equal amount of <style=cIsHealing>shields</style>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

    public override ItemTag[] Category => [ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        GetStatCoefficients += CyberneticHeartBoostShields;
        On.RoR2.HealthComponent.SendHeal += CyberneticHeartRestoreShields;
    }

    private void CyberneticHeartRestoreShields(On.RoR2.HealthComponent.orig_SendHeal orig, GameObject target, float amount, bool isCrit)
    {
        var body = target.GetComponent<CharacterBody>();
        if (body)
        {
            var component = body.healthComponent;
            if (component)
            {
                var stackCount = GetCount(body);
                if (stackCount > 0)
                {
                    component.shield = Mathf.Min(component.shield + amount, component.fullShield);
                }
            }
        }
        orig(target, amount, isCrit);
    }

    private void CyberneticHeartBoostShields(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.baseShieldAdd += sender.maxHealth * (CyberneticHeartShieldInit + (stackCount - 1) * CyberneticHeartShieldStack);
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
