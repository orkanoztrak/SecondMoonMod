using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using MonoMod.Cil;
using UnityEngine.AddressableAssets;
using MonoMod.RuntimeDetour;
using R2API.Utils;
using System.Linq;
using System.Runtime.CompilerServices;

namespace SecondMoon.Items.Lunar.OfferingBowl;

public class OfferingBowl : Item<OfferingBowl>
{
    public static ConfigOption<float> OfferingBowlOutgoingDamageReductionInit;
    public static ConfigOption<float> OfferingBowlOutgoingDamageReductionStack;

    public static ConfigOption<float> OfferingBowlIncomingDamageIncreaseInit;
    public static ConfigOption<float> OfferingBowlIncomingDamageIncreaseStack;

    public Xoroshiro128Plus OfferingBowlRNG;

    public override string ItemName => "Offering Bowl";

    public override string ItemLangTokenName => "SECONDMOONMOD_OFFERING_BOWL";

    public override string ItemPickupDesc => $"Break this at the end of the Teleporter event to get a free legendary and boss item... <color=#FF7F7F>BUT take more and deal less damage while you carry it.</color>\n";

    public override string ItemFullDesc => $"After completing the Teleporter event, lose all stacks of this item and get <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> random <style=cIsHealth>legendary</style> and <style=cIsTierBoss>boss</style> items in return. " +
        $"<color=#FF7F7F>Set damage dealt to <style=cIsDamage>{OfferingBowlOutgoingDamageReductionInit}×</style> <style=cStack>({OfferingBowlOutgoingDamageReductionStack}× per stack)</style></color> " +
        $"<color=#FF7F7F>and damage received to <style=cIsDamage>{OfferingBowlIncomingDamageIncreaseInit}×</style> <style=cStack>({OfferingBowlIncomingDamageIncreaseStack}× per stack)</style>.</color> " +
        $"Cannot appear outside the Bazaar Between Time.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.HoldoutZoneRelated, ItemTag.WorldUnique];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += OfferingBowlSetDamageDealt;
        On.RoR2.HealthComponent.TakeDamage += OfferingBowlSetDamageTaken;
        TeleporterInteraction.onTeleporterChargedGlobal += OfferingBowlRewards;
    }

    private void OfferingBowlSetDamageDealt(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchLdarg(0),
                        x => x.MatchLdloc(78),
                        x => x.MatchCall<CharacterBody>("set_damage"));

        cursor.GotoNext(MoveType.After, x => x.MatchCall<CharacterBody>("set_damage"));

        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<CharacterBody>>((body) =>
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                body.damage *= OfferingBowlOutgoingDamageReductionInit * (float)Math.Pow(OfferingBowlOutgoingDamageReductionStack, stackCount - 1);
            }
        });
    }

    [Server]
    private void OfferingBowlRewards(TeleporterInteraction ınteraction)
    {
        ReadOnlyCollection<TeamComponent> teamMembers = TeamComponent.GetTeamMembers(TeamIndex.Player);
        OfferingBowlRNG = new Xoroshiro128Plus(Run.instance.seed);
        for (int i = 0; i < teamMembers.Count; i++)
        {
            TeamComponent teamComponent = teamMembers[i];
            CharacterBody body = teamComponent.body;
            if (!body)
            {
                continue;
            }
            CharacterMaster master = teamComponent.body.master;
            if (master)
            {
                int stackCount = master.inventory.GetItemCount(ItemDef);
                if (stackCount > 0)
                {
                    for (int j = 0; j < stackCount; j++)
                    {
                        List<PickupIndex> reds = new List<PickupIndex>(Run.instance.availableTier3DropList);
                        List<PickupIndex> yellows = new List<PickupIndex>(Run.instance.availableBossDropList);

                        Util.ShuffleList(reds, OfferingBowlRNG);
                        Util.ShuffleList(yellows, OfferingBowlRNG);

                        PickupIndex red = reds[0];
                        PickupIndex yellow = yellows[0];

                        master.inventory.GiveItem(red.itemIndex, 1);
                        GenericPickupController.SendPickupMessage(master, red);

                        master.inventory.GiveItem(yellow.itemIndex, 1);
                        GenericPickupController.SendPickupMessage(master, yellow);

                        master.inventory.RemoveItem(ItemDef);
                    }
                }
            }
        }
    }

    private void OfferingBowlSetDamageTaken(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        var newDamageInfo = damageInfo;
        var body = self.body;
        if (body)
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                newDamageInfo.damage *= OfferingBowlIncomingDamageIncreaseInit * (float)Math.Pow(OfferingBowlIncomingDamageIncreaseStack, stackCount - 1);
            }
        }
        orig(self, newDamageInfo);
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            Droptable.Init();
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        OfferingBowlIncomingDamageIncreaseInit = config.ActiveBind("Item: " + ItemName, "Increased damage taken with one " + ItemName, 2f, "What multipler should be applied to damage taken with one " + ItemName + "? This scales exponentially (Refer to Shaped Glass on the wiki).");
        OfferingBowlIncomingDamageIncreaseStack = config.ActiveBind("Item: " + ItemName, "Increased damage taken per stack after one " + ItemName, 2f, "What multipler should be applied to damage taken per stack of " + ItemName + " after one? This scales exponentially (Refer to Shaped Glass on the wiki).");

        OfferingBowlOutgoingDamageReductionInit = config.ActiveBind("Item: " + ItemName, "Reduced damage dealt with one " + ItemName, 0.5f, "What multipler should be applied to damage taken with one " + ItemName + "? This scales exponentially (Refer to Shaped Glass on the wiki).");
        OfferingBowlOutgoingDamageReductionStack = config.ActiveBind("Item: " + ItemName, "Reduced damage dealt per stack after one " + ItemName, 0.5f, "What multipler should be applied to damage taken per stack of " + ItemName + " after one? This scales exponentially (Refer to Shaped Glass on the wiki).");
    }

    internal class Droptable
    {
        internal static Hook OnServerChangeSceneHook;
        internal delegate void d_ServerChangeScene(NetworkManager instance, string newSceneName);
        internal static d_ServerChangeScene origServerChangeScene;

        internal static PickupIndex bowlPickupIndex;

        internal static void Init()
        {
            var hookConfig = new HookConfig { ManualApply = true };
            OnServerChangeSceneHook = new Hook(typeof(NetworkManager).GetMethod("ServerChangeScene"),
                 typeof(Droptable).GetMethodCached("ModifyRunLunarDroplists"), hookConfig);
            origServerChangeScene = OnServerChangeSceneHook.GenerateTrampoline<d_ServerChangeScene>();
            Run.onRunStartGlobal += ApplyHook;
            Run.onRunDestroyGlobal += UndoHook;
        }

        private static void ApplyHook(Run run)
        {
            bowlPickupIndex = PickupCatalog.GetPickupDef(PickupCatalog.FindPickupIndex(instance.ItemDef.itemIndex)).pickupIndex;
            if (NetworkServer.active)
            {
                OnServerChangeSceneHook.Apply();
            }
        }

        private static void UndoHook(Run run)
        {
            if (NetworkServer.active)
            {
                OnServerChangeSceneHook.Undo();
            }
        }

        private static void ModifyRunLunarDroplists(NetworkManager instance, string newSceneName)
        {
            if (Run.instance && Stage.instance)
            {
                if (Stage.instance.sceneDef.cachedName != "bazaar" && newSceneName == "bazaar")
                {
                    Run.instance.availableLunarItemDropList.Add(bowlPickupIndex);
                    Run.instance.availableLunarCombinedDropList.Add(bowlPickupIndex);

                    PickupTransmutationManager.availablePickupGroupMap[bowlPickupIndex.value] = PickupTransmutationManager.itemTierLunarGroup;
                    PickupTransmutationManager.pickupGroupMap[bowlPickupIndex.value] = PickupTransmutationManager.itemTierLunarGroup;

                    foreach (var pickupIndex in PickupTransmutationManager.itemTierLunarGroup)
                    {
                        AddItemToMap(PickupTransmutationManager.availablePickupGroupMap, pickupIndex.value, bowlPickupIndex);
                        AddItemToMap(PickupTransmutationManager.pickupGroupMap, pickupIndex.value, bowlPickupIndex);
                    }

                    AddItemToMap(PickupTransmutationManager.availablePickupGroupMap, bowlPickupIndex.value, bowlPickupIndex);
                    AddItemToMap(PickupTransmutationManager.pickupGroupMap, bowlPickupIndex.value, bowlPickupIndex);

                    PickupDropTable.RegenerateAll(Run.instance);
                }
                else if (Stage.instance.sceneDef.cachedName == "bazaar" && newSceneName != "bazaar")
                {
                    Run.instance.availableLunarItemDropList.Remove(bowlPickupIndex);
                    Run.instance.availableLunarCombinedDropList.Remove(bowlPickupIndex);

                    foreach (var pickupIndex in PickupTransmutationManager.itemTierLunarGroup)
                    {
                        RemoveItemFromMap(PickupTransmutationManager.availablePickupGroupMap, pickupIndex.value, bowlPickupIndex);
                        RemoveItemFromMap(PickupTransmutationManager.pickupGroupMap, pickupIndex.value, bowlPickupIndex);
                    }

                    PickupTransmutationManager.availablePickupGroupMap[bowlPickupIndex.value] = null;
                    PickupTransmutationManager.pickupGroupMap[bowlPickupIndex.value] = null;

                    PickupDropTable.RegenerateAll(Run.instance);
                }
            }
            origServerChangeScene(instance, newSceneName);

            void AddItemToMap(PickupIndex[][] map, int index, PickupIndex item)
            {
                var group = map[index];
                Array.Resize(ref group, group.Length + 1);
                group[group.Length - 1] = item;
                map[index] = group;
            }

            void RemoveItemFromMap(PickupIndex[][] map, int index, PickupIndex item)
            {
                var group = map[index];
                int indexFound = -1;
                for (int i = group.Length - 1; i >= 0; i--)
                {
                    if (group[i] == item)
                    {
                        indexFound = i;
                        break;
                    }
                }
                if (indexFound == group.Length - 1)
                {
                    Array.Resize(ref group, group.Length - 1);
                }
                else if (indexFound > -1)
                {
                    for (int i = indexFound + 1; i < group.Length; i++)
                    {
                        group[i - 1] = group[i];
                        Array.Resize(ref group, group.Length - 1);
                    }
                }
                map[index] = group;
            }
        }
    }
}
