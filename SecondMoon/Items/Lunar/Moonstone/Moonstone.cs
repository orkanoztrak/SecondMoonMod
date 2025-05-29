using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Lunar.Moonstone;

public class Moonstone : Item<Moonstone>
{
    public static ConfigOption<float> MoonstoneCooldownReductionInit;
    public static ConfigOption<float> MoonstoneCooldownReductionStack;

    public static ConfigOption<float> MoonstoneAttackSpeedInit;
    public static ConfigOption<float> MoonstoneAttackSpeedStack;

    public static int MoonstonePlayersStackTracker = 0;
    public static int MoonstoneMonsterStackTracker = 0;

    public override string ItemName => "Moonstone";

    public override string ItemLangTokenName => "PEARL_LUNAR";

    public override string ItemPickupDesc => $"Grants cooldown reduction and attack speed to <color=#FF7F7F>EVERYONE.</color>";

    public override string ItemFullDesc => $"Gain <style=cIsUtility>{MoonstoneCooldownReductionInit * 100}% <style=cStack>(+{MoonstoneCooldownReductionStack * 100}% per stack)</style> cooldown reduction</style> " +
        $"and <style=cIsDamage>{MoonstoneAttackSpeedInit * 100}% <style=cStack>(+{MoonstoneAttackSpeedStack * 100}% per stack)</style> attack speed</style><color=#FF7F7F>(enemies also benefit from these).</color>";

    public override string ItemLore => "He would obviously not sit idly as I spread my influence.\r\n\r\n" +
        "What he fails to realize, however, is that I offer greater power and riches than he ever could. Observe, my servants, as I convert his paltry effort at a gift into something that can turn the tides.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.WorldUnique];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += MoonstoneSetupTeamStackCounts;
        RecalculateStatsAPI.GetStatCoefficients += MoonstoneBoostStats;
    }

    private void MoonstoneSetupTeamStackCounts(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        int num = 0;
        ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
        int i = 0;
        for (int count = readOnlyInstancesList.Count; i < count; i++)
        {
            CharacterMaster characterMaster = readOnlyInstancesList[i];
            if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.hasBody && characterMaster.playerCharacterMasterController)
            {
                num += characterMaster.inventory.GetItemCount(ItemDef);
            }
        }
        MoonstonePlayersStackTracker = num;
        MoonstoneMonsterStackTracker = Util.GetItemCountForTeam(TeamIndex.Monster, ItemDef.itemIndex, requiresAlive: true, requiresConnected: false);
        orig(self);

    }

    private void MoonstoneBoostStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            float decrease = (float)(1 - ((1 - MoonstoneCooldownReductionInit) * Math.Pow(1 - MoonstoneCooldownReductionStack, stackCount - 1)));
            args.cooldownMultAdd -= decrease;
            args.attackSpeedMultAdd += MoonstoneAttackSpeedInit + (stackCount - 1) * MoonstoneAttackSpeedStack;
        }
        if (sender.teamComponent)
        {
            float decrease;
            switch (sender.teamComponent.teamIndex)
            {
                case TeamIndex.Player:
                    args.attackSpeedMultAdd += MoonstoneMonsterStackTracker > 0 ? MoonstoneAttackSpeedInit + (MoonstoneMonsterStackTracker - 1) * MoonstoneAttackSpeedStack : 0;
                    decrease = (float)(1 - ((1 - MoonstoneCooldownReductionInit) * Math.Pow(1 - MoonstoneCooldownReductionStack, MoonstoneMonsterStackTracker - 1)));
                    args.cooldownMultAdd -= MoonstoneMonsterStackTracker > 0 ? decrease : 0;
                    break;
                case TeamIndex.Monster:
                    args.attackSpeedMultAdd += MoonstonePlayersStackTracker > 0 ? MoonstoneAttackSpeedInit + (MoonstonePlayersStackTracker - 1) * MoonstoneAttackSpeedStack : 0;
                    decrease = (float)(1 - ((1 - MoonstoneCooldownReductionInit) * Math.Pow(1 - MoonstoneCooldownReductionStack, MoonstonePlayersStackTracker - 1)));
                    args.cooldownMultAdd -= MoonstonePlayersStackTracker > 0 ? decrease : 0;
                    break;
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
        MoonstoneCooldownReductionInit = config.ActiveBind("Item: " + ItemName, "Cooldown reduction with one " + ItemName, 0.15f, "How much should cooldowns be reduced by with one " + ItemName + "? (0.15 = 15%)");
        MoonstoneCooldownReductionStack = config.ActiveBind("Item: " + ItemName, "Cooldown reduction per stack after one " + ItemName, 0.15f, "How much should cooldowns be reduced by per stack of " + ItemName + " after one? (0.15 = 15%)");

        MoonstoneAttackSpeedInit = config.ActiveBind("Item: " + ItemName, "Attack speed with one " + ItemName, 0.15f, "How much should attack speed be increased by with one " + ItemName + "? (0.15 = 15%)");
        MoonstoneAttackSpeedStack = config.ActiveBind("Item: " + ItemName, "Attack speed per stack after one " + ItemName, 0.15f, "How much should attack speed be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");
    }
}
