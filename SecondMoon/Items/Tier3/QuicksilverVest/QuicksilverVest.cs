using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier3.QuicksilverVest;

public class QuicksilverVest : Item<QuicksilverVest>
{
    public static ConfigOption<float> QuicksilverVestHealthInit;
    public static ConfigOption<float> QuicksilverVestHealthStack;

    public static ConfigOption<float> QuicksilverVestConversionRate;

    public override string ItemName => "Quicksilver Vest";

    public override string ItemLangTokenName => "QUICKSILVER_VEST";

    public override string ItemPickupDesc => "Increase your maximum health. Increasing health increases speed.";

    public override string ItemFullDesc => $"Gain <style=cIsHealing>{QuicksilverVestHealthInit * 100}%</style> <style=cStack>(+{QuicksilverVestHealthStack * 100}% per stack)</style> <style=cIsHealing>maximum health</style>" +
        $" and increase <style=cIsUtility>movement speed</style> by <style=cIsHealing>{QuicksilverVestConversionRate * 100}%</style> of the increase percentage to your <style=cIsHealing>health bar</style>, excluding barrier.";

    public override string ItemLore => "The cemetary was cold and damp with the rain of the previous day. The dark clouds in the sky prophet of more to come. An old man stood in front of a grave, removed from the rest at a far corner of the cemetary, under a tree.\r\n\r\n" +
        "\"Hello old friend. How is it down there? Is it as cold as it is up here? Or is it colder?\"\r\n\r\n" +
        "\"I miss you so much. Even though the rest of the world doesn't, they haven't known you like I have. We were renegades once. Now one of us is an old man, the other dead.\"\r\n\r\n" +
        "\"You should have let me take the fall with you. Being the only remainder of our efforts hurts in a way I can't describe to you.\"\r\n\r\n" +
        "\"I hope to join you soon. The quiet depths and the measured steps don't echo like the shriek of riot did.\"";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += QuicksilverVestStatBoosts;
    }

    private void QuicksilverVestStatBoosts(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.healthMultAdd += QuicksilverVestHealthInit + (stackCount - 1) * QuicksilverVestHealthStack;
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
        QuicksilverVestHealthInit = config.ActiveBind("Item: " + ItemName, "Maximum health and movement speed increase with one " + ItemName, 0.2f, "How much should maximum health and movement speed be increased by with one " + ItemName + "? (0.2 = 20%)");
        QuicksilverVestHealthStack = config.ActiveBind("Item: " + ItemName, "Maximum health and movement speed increase per stack after one " + ItemName, 0.2f, "How much should maximum health and movement speed be increased by per stack of " + ItemName + " after one? (0.2 = 20%)");
        QuicksilverVestConversionRate = config.ActiveBind("Item: " + ItemName, "Health and speed conversion rate", 0.75f, "This item calculates the percentage increase on your health bar (excluding barrier), multiplies it by this number, and increases movement speed by that %.");
    }
}
