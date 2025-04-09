using BepInEx.Configuration;
using EntityStates;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Hologram;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;
using SecondMoon.Items.Void.TwistedRegrets;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UIElements;

namespace SecondMoon.Interactables.Purchase.TakesMoney;

public class PrototypeVault : Interactable
{
    public static ConfigOption<int> PrototypeVaultBaseMoneyCost;
    public static ConfigOption<int> PrototypeVaultBaseSoulCost;
    public static ConfigOption<float> PrototypeVaultCorruptionChance;
    public override string InteractableName => "Prototype Vault";

    public override string InteractableContext => "Open chest..?";

    public override string InteractableLangToken => "PROTOTYPE_VAULT";

    public override string InteractableInspectDesc => "Costs gold and an offering of Soul to open. Always contains a Dormant Prototype item.";

    public override GameObject InteractableModel => SecondMoonPlugin.SecondMoonAssets.LoadAsset<GameObject>("Assets/SecondMoonAssets/Models/Interactables/PrototypeVault/PrototypeVault.prefab");

    public Sprite PrototypeVaultSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();

    [SyncVar]
    public static GameObject InteractableBodyModelPrefab;

    public GameObject PrefabInstance;

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            Hooks();
        }
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += ConstructInteractable;
        On.RoR2.PurchaseInteraction.OnInteractionBegin += AddSoulCost;
        IL.RoR2.PurchaseInteraction.GetContextString += AddSoulCostString;
        IL.RoR2.CostHologramContent.FixedUpdate += AddSoulCostString2;
    }

    private void AddSoulCost(On.RoR2.PurchaseInteraction.orig_OnInteractionBegin orig, PurchaseInteraction self, Interactor activator)
    {
        if (self)
        {
            if (self.gameObject.name.Contains("PrototypeVault"))
            {
                var healthComponent = activator.GetComponent<HealthComponent>();
                if (healthComponent)
                {
                    if (healthComponent.body)
                    {
                        for (int i = 0; i < PrototypeVaultBaseSoulCost / 10; i++)
                        {
                            healthComponent.body.AddBuff(DLC2Content.Buffs.SoulCost);
                        }
                    }
                }
            }
        }
        orig(self, activator);
    }

    private void AddSoulCostString2(ILContext il)
    {
        var cursor = new ILCursor(il);
        var colorIndex = 0;
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld<CostHologramContent>("targetTextMesh"),
                               x => x.MatchLdloc(colorIndex)))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<CostHologramContent>>((component) =>
            {
                if (component)
                {
                    var interactable = component.gameObject.transform.root.gameObject;
                    if (interactable.name.Contains("PrototypeVault"))
                    {
                        component.targetTextMesh.SetText(component.targetTextMesh.text + " & " + PrototypeVaultBaseSoulCost + "% Soul");
                    }
                }
            });
        }
    }


    private void AddSoulCostString(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdstr(")</nobr>")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<PurchaseInteraction>>((purchaseInteraction) => 
            {
                if (purchaseInteraction)
                {
                    if (purchaseInteraction.gameObject.name.Contains("PrototypeVault"))
                    {
                        PurchaseInteraction.sharedStringBuilder.Append(" & <color=#EFEB1A>" + PrototypeVaultBaseSoulCost + "% Soul</color>");
                    }
                }
            });
        }
    }

    private void ConstructInteractable()
    {
        InteractableBodyModelPrefab = InteractableModel;
        var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
        pingInfoProvider.pingIconOverride = PrototypeVaultSprite;

        var inspect = ScriptableObject.CreateInstance<InspectDef>();
        var info = new RoR2.UI.InspectInfo();
        info.Visual = PrototypeVaultSprite;
        info.DescriptionToken = $"INTERACTABLE_{InteractableLangToken}_INSPECT";
        info.TitleToken = $"INTERACTABLE_{InteractableLangToken}_TITLE";
        inspect.Info = info;

        var chestBehavior = InteractableBodyModelPrefab.GetComponent<ChestBehavior>();
        chestBehavior.dropTable = CreateDropTable();

        var purchaseInteraction = InteractableBodyModelPrefab.GetComponent<PurchaseInteraction>();
        purchaseInteraction.displayNameToken = $"INTERACTABLE_{InteractableLangToken}_NAME";
        purchaseInteraction.contextToken = $"INTERACTABLE_{InteractableLangToken}_CONTEXT";
        purchaseInteraction.automaticallyScaleCostWithDifficulty = true;

        var genericInspectInfoProvider = InteractableBodyModelPrefab.gameObject.AddComponent<GenericInspectInfoProvider>();
        genericInspectInfoProvider.InspectInfo = inspect;

        var entityStateMachine = InteractableBodyModelPrefab.AddComponent<EntityStateMachine>();
        entityStateMachine.customName = "PrototypeVault";
        entityStateMachine.initialStateType.stateType = typeof(Idle);
        entityStateMachine.mainStateType.stateType = typeof(Idle);

        var networkStateMachine = InteractableBodyModelPrefab.AddComponent<NetworkStateMachine>();
        networkStateMachine.stateMachines = [entityStateMachine];

        InteractableSpawnCard card = ScriptableObject.CreateInstance<InteractableSpawnCard>();
        card.name = "iscProtoypeVault";
        card.prefab = InteractableBodyModelPrefab;
        card.sendOverNetwork = true;
        card.hullSize = HullClassification.Human;
        card.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
        card.requiredFlags = RoR2.Navigation.NodeFlags.None;
        card.forbiddenFlags = RoR2.Navigation.NodeFlags.NoChestSpawn;
        card.directorCreditCost = 20;
        card.occupyPosition = true;
        card.orientToFloor = true;
        card.skipSpawnWhenSacrificeArtifactEnabled = false;

        DirectorCard directorCard = new DirectorCard
        {
            selectionWeight = 4,
            spawnCard = card,
        };

        DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
        {
            Card = directorCard,
            InteractableCategory = DirectorAPI.InteractableCategory.Chests,
        };

        DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);
        PrefabAPI.RegisterNetworkPrefab(InteractableBodyModelPrefab);

        PickupDropTable CreateDropTable()
        {
            List<ExplicitPickupDropTable.PickupDefEntry> list = [];
            foreach (var item in SecondMoonPlugin.SecondMoonItems)
            {
                if (item.ItemTierDef)
                {
                    if (item.ItemTierDef.Equals(TierPrototypeDormant.instance.ItemTierDef))
                    {
                        list.Add(new ExplicitPickupDropTable.PickupDefEntry
                        {
                            pickupDef = item.ItemDef,
                            pickupWeight = 1
                        });
                    }
                }
            }
            ExplicitPickupDropTable table = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            table.pickupEntries = list.ToArray();
            return table;
        }
    }


    private void CreateConfig(ConfigFile config)
    {
        PrototypeVaultBaseMoneyCost = config.ActiveBind("Interactable: " + InteractableName, "Base money cost", 75, "The cost this would have if it spawned on the first stage. For reference, the default value is triple that of a regular chest.");
        PrototypeVaultBaseSoulCost = config.ActiveBind("Interactable: " + InteractableName, "Base Soul cost", 10, "The purchase will take this % of maximum health as cost.");
        PrototypeVaultCorruptionChance = config.ActiveBind("Interactable: " + InteractableName, "Chance to spawn corrupted", 25f, "There is a this % chance that this will instead contain " + CoreOfCorruption.instance.ItemName + ".");
    }
}
