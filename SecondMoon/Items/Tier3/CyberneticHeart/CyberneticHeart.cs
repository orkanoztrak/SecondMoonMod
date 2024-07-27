using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier3.CyberneticHeart;

public class CyberneticHeart : Item<CyberneticHeart>
{
    public static ConfigOption<float> CyberneticHeartShieldInit;
    public static ConfigOption<float> CyberneticHeartShieldStack;

    public override string ItemName => "Cybernetic Heart";

    public override string ItemLangTokenName => "SECONDMOONMOD_CYBERNETIC_HEART";

    public override string ItemPickupDesc => "Gain a recharging shield. Your healing restores shields as well.";

    public override string ItemFullDesc => $"Gain a shield equal to <style=cIsHealing>{CyberneticHeartShieldInit * 100}%</style> <style=cStack>(+{CyberneticHeartShieldStack * 100}% per stack)</style> of your maximum health. " +
        $"Your <style=cIsHealing>healing</style> restores an equal amount of <style=cIsHealing>shields</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += CyberneticHeartBoostShields;
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

    private void CyberneticHeartBoostShields(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            HealthComponent healthComponent = sender.healthComponent;
            args.baseShieldAdd += healthComponent.fullHealth * (CyberneticHeartShieldInit + (stackCount - 1) * CyberneticHeartShieldStack);
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
        CyberneticHeartShieldInit = config.ActiveBind("Item: " + ItemName, "Maximum shield increase with one " + ItemName, 0.2f, "Gain shield equal to what % of maximum health with one " + ItemName + "? (0.2 = 20%)");
        CyberneticHeartShieldStack = config.ActiveBind("Item: " + ItemName, "Maximum shield increase per stack after one " + ItemName, 0.2f, "Gain shield equal to what % of maximum health per stack of " + ItemName + " after one? (0.2 = 20%)");
    }
}
