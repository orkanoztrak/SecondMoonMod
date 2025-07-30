using BepInEx.Configuration;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Hologram;
using SecondMoon.Equipment.AffixGuardian;
using SecondMoon.Items.Lunar.IrradiatedMoonstone;
using SecondMoon.Items.Lunar.Moonstone;
using SecondMoon.MyEntityStates.Interactables;
using SecondMoon.MyEntityStates.Items.Prototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.Windows.Speech;

namespace SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;

public class AwakeningShrine : Interactable<AwakeningShrine>
{
    public static Dictionary<ItemIndex, ItemIndex> PrototypeItemIndexPairs = new Dictionary<ItemIndex, ItemIndex>();
    public static Dictionary<ItemIndex, EquipmentIndex> PrototypeEquipmentIndexPairs = new Dictionary<ItemIndex, EquipmentIndex>();

    public override string InteractableName => "Awakening Shrine";

    public override string InteractableContext => "Awaken a Dormant item";

    public override string InteractableLangToken => "AWAKENING_SHRINE";

    public override string InteractableInspectDesc => "Once this is given a Dormant Protoype item, it will spawn a Guardian elite boss monster. " +
        "Defeating this boss will cause the corresponding Prototype item or equipment to drop.";

    public override GameObject InteractableModel => SecondMoonPlugin.SecondMoonAssets.LoadAsset<GameObject>("Assets/SecondMoonAssets/Models/Interactables/AwakeningShrine/AwakeningShrine.prefab");

    [SyncVar]
    public static GameObject InteractableBodyModelPrefab;

    public Sprite AwakeningShrineSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texShrineIconOutlined.png").WaitForCompletion();

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

    private void CreateConfig(ConfigFile config)
    {
        
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += ConstructInteractable;
        On.RoR2.PickupPickerController.GetInteractability += PickupPickerController_GetInteractability;
        BossGroup.onBossGroupDefeatedServer += ReturnToMain;
        On.RoR2.BossGroup.DropRewards += GuardianEliteDropPrototype;
        //On.RoR2.BossGroup.UpdateObservations += ChangeSubtitle;
        On.RoR2.CharacterBody.GetSubtitle += ChangeSubtitle;
    }

    private string ChangeSubtitle(On.RoR2.CharacterBody.orig_GetSubtitle orig, CharacterBody self)
    {
        if (self.HasBuff(AffixGuardian.instance.EliteBuffDef))
        {
            return AffixGuardian.AffixGuardianSubtitle;
        }
        return orig(self);
    }

    private void ChangeSubtitle(On.RoR2.BossGroup.orig_UpdateObservations orig, BossGroup self, ValueType memory)
    {
        orig(self,memory);
        var manager = AwakeningShrineManager.FindForBossGroup(self);
        if (manager)
        {
            self.bestObservedSubtitle = "<sprite name=\"CloudLeft\" tint=1> " + "Watchful Gaze of the Bulwark" + "<sprite name=\"CloudRight\" tint=1>";
        }
    }

    private void GuardianEliteDropPrototype(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
    {
        var manager = AwakeningShrineManager.FindForBossGroup(self);
        ExplicitPickupDropTable dropTable = null;
        if (manager)
        {
            dropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            var prototype = PickupCatalog.GetPickupDef(manager.FindCorrespondingPrototypeForDormant());
            if (prototype != null)
            {
                if (prototype.itemIndex != ItemIndex.None)
                {
                    dropTable.pickupEntries = [new ExplicitPickupDropTable.PickupDefEntry { pickupDef = ItemCatalog.GetItemDef(prototype.itemIndex), pickupWeight = 1 }];
                }
                else if (prototype.equipmentIndex != EquipmentIndex.None)
                {
                    dropTable.pickupEntries = [new ExplicitPickupDropTable.PickupDefEntry { pickupDef = EquipmentCatalog.GetEquipmentDef(prototype.equipmentIndex), pickupWeight = 1 }];
                }
                dropTable.Regenerate(Run.instance);
                self.dropTable = dropTable;
            }
        }
        orig(self);
        UnityEngine.Object.Destroy(dropTable);
    }

    private void ReturnToMain(BossGroup group)
    {
        var manager = AwakeningShrineManager.FindForBossGroup(group);
        if (manager)
        {
            var esm = manager.gameObject.GetComponent<EntityStateMachine>();
            if (esm.state.GetType() == typeof(AwakeningShrineBossFight))
            {
                manager.DisableBossDirector();
                esm.SetNextStateToMain();
            }
        }
    }

    private Interactability PickupPickerController_GetInteractability(On.RoR2.PickupPickerController.orig_GetInteractability orig, PickupPickerController self, Interactor activator)
    {
        if (self.gameObject.name.Contains("AwakeningShrine") && activator)
        {
            var body = activator.GetComponent<CharacterBody>();
            if (body)
            {
                var state = self.gameObject.GetComponent<EntityStateMachine>().state.GetType();
                if (body.master && state == typeof(AwakeningShrineIdle))
                {
                    var hasDormant = false;
                    foreach (var key in PrototypeEquipmentIndexPairs.Keys)
                    {
                        if (body.master.inventory.GetItemCount(key) > 0)
                        {
                            hasDormant = true;
                        }
                    }
                    if (!hasDormant)
                    {
                        foreach (var key in PrototypeItemIndexPairs.Keys)
                        {
                            if (body.master.inventory.GetItemCount(key) > 0)
                            {
                                hasDormant = true;
                            }
                        }
                    }
                    if (!hasDormant)
                    {
                        return Interactability.ConditionsNotMet;
                    }
                    return Interactability.Available;
                }
                else if (state != typeof(AwakeningShrineIdle))
                {
                    return Interactability.Disabled;
                }
            }
        }
        return orig(self, activator);
    }

    private void ConstructInteractable()
    {
        InitializePrototypeConversionPairs();

        InteractableBodyModelPrefab = InteractableModel;

        var pingInfoProvider = InteractableBodyModelPrefab.AddComponent<PingInfoProvider>();
        pingInfoProvider.pingIconOverride = AwakeningShrineSprite;

        var manager = InteractableBodyModelPrefab.AddComponent<AwakeningShrineManager>();
        manager.shrine = InteractableBodyModelPrefab;

        var inspect = ScriptableObject.CreateInstance<InspectDef>();
        var info = new RoR2.UI.InspectInfo();
        info.Visual = AwakeningShrineSprite;
        info.DescriptionToken = $"INTERACTABLE_{InteractableLangToken}_INSPECT";
        info.TitleToken = $"INTERACTABLE_{InteractableLangToken}_TITLE";
        inspect.Info = info;

        var ppc = InteractableBodyModelPrefab.AddComponent<PickupPickerController>();
        ppc.panelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/RebirthPickerPanel.prefab").WaitForCompletion().InstantiateClone("AwakeningPickerPanel");
        ppc.onPickupSelected = new PickupPickerController.PickupIndexUnityEvent();
        ppc.onPickupSelected.AddPersistentListener(manager.HandleSelection);
        ppc.onServerInteractionBegin = new GenericInteraction.InteractorUnityEvent();
        ppc.onServerInteractionBegin.AddPersistentListener(manager.HandleInteraction);
        ppc.cutoffDistance = 10f;

        var entityStateMachine = InteractableBodyModelPrefab.AddComponent<EntityStateMachine>();
        entityStateMachine.customName = "AwakeningShrine";
        entityStateMachine.mainStateType = entityStateMachine.initialStateType = new SerializableEntityStateType(typeof(AwakeningShrineIdle));

        var networkStateMachine = InteractableBodyModelPrefab.AddComponent<NetworkStateMachine>();
        networkStateMachine.stateMachines = [entityStateMachine];

        var genericNameDisplay = InteractableBodyModelPrefab.AddComponent<GenericDisplayNameProvider>();
        genericNameDisplay.displayToken = $"INTERACTABLE_{InteractableLangToken}_NAME";

        var genericInspectInfoProvider = InteractableBodyModelPrefab.gameObject.AddComponent<GenericInspectInfoProvider>();
        genericInspectInfoProvider.InspectInfo = inspect;

        var highlightController = InteractableBodyModelPrefab.GetComponent<Highlight>();
        highlightController.targetRenderer = InteractableBodyModelPrefab.GetComponentsInChildren<MeshRenderer>().Where(x => x.gameObject.name.Contains("mdlAwakeningShrine")).First();
        highlightController.strength = 1;
        highlightController.highlightColor = Highlight.HighlightColor.interactive;

        InteractableSpawnCard isc = ScriptableObject.CreateInstance<InteractableSpawnCard>();
        isc.name = "iscAwakeningShrine";
        isc.prefab = InteractableBodyModelPrefab;
        isc.sendOverNetwork = true;
        isc.hullSize = HullClassification.Golem;
        isc.nodeGraphType = RoR2.Navigation.MapNodeGroup.GraphType.Ground;
        isc.requiredFlags = RoR2.Navigation.NodeFlags.None;
        isc.forbiddenFlags = RoR2.Navigation.NodeFlags.NoShrineSpawn;
        isc.directorCreditCost = 20;
        isc.occupyPosition = true;
        isc.orientToFloor = true;
        isc.skipSpawnWhenSacrificeArtifactEnabled = false;

        DirectorCard directorCard = new DirectorCard
        {
            selectionWeight = 4,
            spawnCard = isc,
        };

        DirectorAPI.DirectorCardHolder directorCardHolder = new DirectorAPI.DirectorCardHolder
        {
            Card = directorCard,
            InteractableCategory = DirectorAPI.InteractableCategory.Shrines,
        };

        DirectorAPI.Helpers.AddNewInteractable(directorCardHolder);

        InteractableBodyModelPrefab.RegisterNetworkPrefab();

        static void InitializePrototypeConversionPairs()
        {
            foreach (var item in SecondMoonPlugin.SecondMoonItems)
            {
                if (item.ActivateIntoPrototypeItem != ItemIndex.None)
                {
                    PrototypeItemIndexPairs.Add(item.ItemDef.itemIndex, item.ActivateIntoPrototypeItem);
                }
                else if (item.ActivateIntoPrototypeEquipment != EquipmentIndex.None)
                {
                    PrototypeEquipmentIndexPairs.Add(item.ItemDef.itemIndex, item.ActivateIntoPrototypeEquipment);
                }
            }
        }
    }
}