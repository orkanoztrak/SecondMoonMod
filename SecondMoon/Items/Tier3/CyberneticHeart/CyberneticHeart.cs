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

    public override string ItemLangTokenName => "CYBERNETIC_HEART";

    public override string ItemPickupDesc => "Gain a recharging shield. Healing restores shields.";

    public override string ItemFullDesc => $"Gain a shield equal to <style=cIsHealing>{CyberneticHeartShieldInit * 100}%</style> <style=cStack>(+{CyberneticHeartShieldStack * 100}% per stack)</style> of your maximum health. " +
        $"Your <style=cIsHealing>healing</style> restores an equal amount of <style=cIsHealing>shields</style>.";

    public override string ItemLore => "-What more must I do for you to trust me!?\r\n\r\n" +
        "-I want nothing from you. Trust, once broken, can't be put back together. Even if you gave me your entire heart, bared open for all to see, I would not trust you.\r\n\r\n" +
        "-You hold that heart in your palm, and yet you do not trust that it beats. I have given you my all. What more can I possibly do?\r\n\r\n" +
        "-It is a deceptive heart that you have given me. It is not a real thing. All that you have \"given\" me is your selfish efforts to win mine.\r\n\r\n" +
        "-What, so all my efforts will go to waste? All that I have done? Is causing me pain all that you want?\r\n\r\n" +
        "-I do not owe you forgiveness. All that you have done, you have done in the delusion of me forgiving you for them. I have never made such a promise.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
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
