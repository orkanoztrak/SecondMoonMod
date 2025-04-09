using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.Prototype.GravityFlask;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier1.SneakySneakers;

public class SneakySneakers : Item<SneakySneakers>
{
    public static ConfigOption<float> SneakySneakersMovementInit;
    public static ConfigOption<float> SneakySneakersMovementStack;

    public static ConfigOption<float> SneakySneakersWalkMultiplier;

    public override string ItemName => "Sneaky Sneakers";

    public override string ItemLangTokenName => "SNEAKY_SNEAKERS";

    public override string ItemPickupDesc => "Increase movement speed. Greater bonus while not sprinting.";

    public override string ItemFullDesc => $"Gain <style=cIsUtility>{SneakySneakersMovementInit * 100}%</style> <style=cStack>(+{SneakySneakersMovementStack * 100}% per stack)</style> <style=cIsUtility>movement speed</style>. " +
        $"This bonus is increased by <style=cIsUtility>{SneakySneakersWalkMultiplier * 100}%</style> while not sprinting.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.SprintRelated];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += SneakySneakersIncreaseMovement;
    }

    private void SneakySneakersIncreaseMovement(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            var mult = sender.isSprinting ? 1 : SneakySneakersWalkMultiplier + 1;
            args.moveSpeedMultAdd += (SneakySneakersMovementInit + (stackCount - 1) * SneakySneakersMovementStack) * mult;
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
        SneakySneakersMovementInit = config.ActiveBind("Item: " + ItemName, "Movement speed increase with one " + ItemName, 0.1f, "How much should movement speed be increased by with one " + ItemName + "? (0.1 = 10%)");
        SneakySneakersMovementStack = config.ActiveBind("Item: " + ItemName, "Movement speed increase per stack after one " + ItemName, 0.1f, "How much should movement speed be increased by per stack of " + ItemName + " after one? (0.1 = 10%)");
        SneakySneakersWalkMultiplier = config.ActiveBind("Item: " + ItemName, "Increase multiplier while not sprinting", 1f, "Not sprinting increases the movement speed this item grants. (1 = 100% increase, meaning double)");
    }
}
