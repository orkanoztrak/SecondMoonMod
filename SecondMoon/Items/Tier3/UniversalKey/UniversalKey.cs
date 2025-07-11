﻿using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Stats;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SecondMoon.Items.Lunar.OfferingBowl.OfferingBowl;

namespace SecondMoon.Items.Tier3.UniversalKey;

public class UniversalKey : Item<UniversalKey>
{
    public static Dictionary<CharacterMaster, (int, GameObject, bool)> UniversalKeyPurchaseCounters = new Dictionary<CharacterMaster, (int, GameObject, bool)>();

    public static ConfigOption<int> UniversalKeyInteractableCountInit;
    public static ConfigOption<int> UniversalKeyInteractableCountStack;
    public override string ItemName => "Universal Key";

    public override string ItemLangTokenName => "UNIVERSAL_KEY";

    public override string ItemPickupDesc => "The cost of the first interactable in a stage is fully refunded and it\'s used twice!";

    public override string ItemFullDesc => $"The cost of the first <style=cIsUtility>{UniversalKeyInteractableCountInit}</style> <style=cStack>(+{UniversalKeyInteractableCountStack} per stack)</style> <style=cIsUtility>interactables</style> in a stage " +
        $"are fully refunded and trigger twice. Not limited to chests!";

    public override string ItemLore => "Order: Weird [REDACTED] Key\r\nTracking Number: 01******\r\nEstimated Delivery: 01/01/2101\r\nShipping Method: High Priority/Fragile\r\nShipping Address: California, North America, Earth\r\nShipping Details:\r\n\r\n" +
        "Don't be fooled by its appearance, man. Looks like a toy, I know, but it certainly isn't. Changes size and shape, unlocks any door, hacks into any computer, hell, Carlos swore he woke up better the next day after he, uh, stuck it in his mouth. Puked out a bucket afterwards, don't know if it was because of the booze or this thing.\r\n\r\n" +
        "Martha gave me hell after last time. So, I leave the figuring-out of whatever this is to you nutjobs.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.InteractableRelated];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        Stage.onServerStageBegin += UpdateUniversalKeyDictionary;
        On.RoR2.PurchaseInteraction.OnInteractionBegin += UniversalKeyPurchaseBehavior;
        On.RoR2.RouletteChestController.EjectPickupServer += UniversalKeyDuplicateDropFromAdaptiveChest;
        On.RoR2.ShopTerminalBehavior.DropPickup += UniversalKeyDuplicateDropFromDelayedShop;
        On.RoR2.BarrelInteraction.OnInteractionBegin += UniversalKeyDuplicateRewardFromBarrels;
        On.RoR2.OptionChestBehavior.ItemDrop += UniversalKeyDuplicateRewardsFromPotential;
    }

    private void UniversalKeyDuplicateRewardsFromPotential(On.RoR2.OptionChestBehavior.orig_ItemDrop orig, OptionChestBehavior self)
    {
        orig(self);
        var interactable = self.gameObject;
        CharacterMaster masterCache = null;
        (int, GameObject, bool) tupleCache = (0, null, false);
        var check = false;
        foreach (var pcmc in PlayerCharacterMasterController.instances)
        {
            UniversalKeyPurchaseCounters.TryGetValue(pcmc.master, out var tuple);
            if (tuple.Item2 == interactable && tuple.Item3)
            {
                tupleCache = tuple;
                masterCache = pcmc.master;
                check = true;
                break;
            }
        }
        if (check)
        {
            self.Roll();
            orig(self);
            UniversalKeyPurchaseCounters[masterCache] = (tupleCache.Item1, tupleCache.Item2, false);
        }
    }

    private void UniversalKeyDuplicateDropFromDelayedShop(On.RoR2.ShopTerminalBehavior.orig_DropPickup orig, ShopTerminalBehavior self)
    {
        orig(self);
        if (!NetworkServer.active) return;
        var interactable = self.gameObject;
        if (interactable.GetComponent<PurchaseInteraction>())
        {
            if (interactable.name.Contains("Duplicator"))
            {
                CharacterMaster masterCache = null;
                (int, GameObject, bool) tupleCache = (0, null, false);
                var check = false;
                foreach (var pcmc in PlayerCharacterMasterController.instances)
                {
                    UniversalKeyPurchaseCounters.TryGetValue(pcmc.master, out var tuple);
                    if (tuple.Item2 == interactable && tuple.Item3)
                    {
                        tupleCache = tuple;
                        masterCache = pcmc.master;
                        check = true;
                        break;
                    }
                }
                if (check)
                {
                    self.SetHasBeenPurchased(false);
                    if (self.Networkhidden)
                    {
                        self.GenerateNewPickupServer();
                    }
                    orig(self);
                    UniversalKeyPurchaseCounters[masterCache] = (tupleCache.Item1, tupleCache.Item2, false);
                }
            }
        }
    }

    private void UniversalKeyDuplicateDropFromAdaptiveChest(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController self, PickupIndex pickupIndex)
    {
        orig(self, pickupIndex);
        var interactable = self.gameObject;
        var check = false;
        CharacterMaster masterCache = null;
        (int, GameObject, bool) tupleCache = (0, null, false);
        foreach (var pcmc in PlayerCharacterMasterController.instances)
        {
            UniversalKeyPurchaseCounters.TryGetValue(pcmc.master, out var tuple);
            if (tuple.Item2 == interactable && tuple.Item3)
            {
                tupleCache = tuple;
                masterCache = pcmc.master;
                check = true;
                break;
            }
        }
        if (check)
        {
            orig(self, pickupIndex);
            UniversalKeyPurchaseCounters[masterCache] = (tupleCache.Item1, tupleCache.Item2, false);
        }
    }

    private void UpdateUniversalKeyDictionary(Stage stage)
    {
        UniversalKeyPurchaseCounters.Clear();
        foreach (var pcmc in PlayerCharacterMasterController.instances)
        {
            if (Stage.instance.sceneDef.cachedName == "bazaar")
            {
                UniversalKeyPurchaseCounters[pcmc.master] = (0, null, false);
            }
            else
            {
                UniversalKeyPurchaseCounters[pcmc.master] = (pcmc.master.inventory.GetItemCount(ItemDef), null, false);
            }
        }
    }

    private void UniversalKeyDuplicateRewardFromBarrels(On.RoR2.BarrelInteraction.orig_OnInteractionBegin orig, BarrelInteraction self, Interactor activator)
    {
        if (NetworkServer.active)
        {
            UniversalKeyPurchaseCounters.TryGetValue(activator.GetComponent<CharacterBody>().master, out var tuple);
            var counter = tuple.Item1;
            if (counter > 0)
            {
                var interactable = self.gameObject;
                if (self.GetInteractability(activator) == Interactability.Available)
                {
                    var body = activator.GetComponent<CharacterBody>();
                    var stackCount = body.inventory.GetItemCount(ItemDef.itemIndex);
                    if (stackCount > 0)
                    {
                        counter--;
                        UniversalKeyPurchaseCounters[body.master] = (counter, interactable, true);
                        orig(self, activator);
                        self.Networkopened = false;
                    }
                }
            }
        }
        orig(self, activator);
    }


    private void UniversalKeyPurchaseBehavior(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        UniversalKeyPurchaseCounters.TryGetValue(activator.GetComponent<CharacterBody>().master, out var tuple);
        var counter = tuple.Item1;
        if (counter > 0)
        {
            if (self.GetInteractability(activator) == Interactability.Available)
            {
                var body = activator.GetComponent<CharacterBody>();
                bool activated = false;
                var cachedCost = self.cost;
                var stackCount = GetCount(body);
                var interactableType = self.gameObject;
                PickupIndex cachedTerminalPickup = PickupIndex.none;
                bool cachedHiddenTerminal = false;
                ChestBehavior isChest = interactableType.GetComponent<ChestBehavior>();
                RouletteChestController isAdaptiveChest = interactableType.GetComponent<RouletteChestController>();
                ShrineChanceBehavior isChanceShrine = interactableType.GetComponent<ShrineChanceBehavior>();
                ShopTerminalBehavior isShop = interactableType.GetComponent<ShopTerminalBehavior>();
                SummonMasterBehavior isSummon = interactableType.GetComponent<SummonMasterBehavior>();
                OptionChestBehavior isPotential = interactableType.GetComponent<OptionChestBehavior>();
                float cachedFailure = 0f;
                float cachedFailureWeight = 0f;
                if (stackCount > 0)
                {
                    if (isChest)
                    {
                        if (!interactableType.name.Contains("Fan"))
                        {
                            activated = true;
                            isChest.dropCount *= 2;
                        }
                    }
                    if (isAdaptiveChest)
                    {
                        if (cachedCost > 0)
                        {
                            activated = true;
                        }
                    }
                    if (isChanceShrine)
                    {
                        activated = true;
                        cachedFailure = isChanceShrine.failureChance;
                        cachedFailureWeight = isChanceShrine.failureWeight;
                        isChanceShrine.failureChance = 0f;
                        isChanceShrine.failureWeight = 0f;
                        isChanceShrine.maxPurchaseCount++;
                    }
                    if (isShop)
                    {
                        if (isShop.serverMultiShopController)
                        {
                            cachedTerminalPickup = isShop.NetworkpickupIndex;
                            cachedHiddenTerminal = isShop.pickupIndexIsHidden;
                            isShop.serverMultiShopController.SetCloseOnTerminalPurchase(self, false);
                        }
                        activated = true;
                    }
                    if (isSummon)
                    {
                        activated = true;
                    }
                    if (isPotential)
                    {
                        activated = true;
                    }
                    if (activated)
                    {
                        counter--;
                        self.cost = 0;
                        UniversalKeyPurchaseCounters[body.master] = (counter, interactableType, true);
                    }
                }
                orig(self, activator);
                if (activated)
                {
                    if (isShop)
                    {
                        if (isShop.serverMultiShopController)
                        {
                            isShop.serverMultiShopController.SetCloseOnTerminalPurchase(self, true);
                            if (cachedHiddenTerminal)
                            {
                                isShop.GenerateNewPickupServer(self);
                            }
                            else
                            {
                                isShop.NetworkpickupIndex = cachedTerminalPickup;
                            }
                        }
                    }
                    orig(self, activator);
                    self.cost = cachedCost;
                    if (isChanceShrine)
                    {
                        isChanceShrine.failureChance = cachedFailure;
                        isChanceShrine.failureWeight = cachedFailureWeight;
                    }
                }
            }
            else
            {
                orig(self, activator);
            }
        }
        else
        {
            orig(self, activator);
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
        UniversalKeyInteractableCountInit = config.ActiveBind("Item: " + ItemName, "Free interactable count with one " + ItemName, 1, "The first how many interactables in a map should be free and trigger twice with one " + ItemName + "?");
        UniversalKeyInteractableCountStack = config.ActiveBind("Item: " + ItemName, "Free interactable count per stack after one " + ItemName, 1, "How many additional interactables in a map after the first should be free and trigger twice per stack of " + ItemName + " after one?");
    }
}
