using BepInEx.Configuration;
using RoR2;
using System;
using System.Collections.Generic;
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

    public override GameObject InteractableModel => throw new NotImplementedException();

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
        throw new NotImplementedException();
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += ConstructInteractable;
        On.RoR2.PickupPickerController.GetInteractability += PickupPickerController_GetInteractability;
    }

    private Interactability PickupPickerController_GetInteractability(On.RoR2.PickupPickerController.orig_GetInteractability orig, PickupPickerController self, Interactor activator)
    {
        if (self.name.Contains("AwakeningShrine") && activator)
        {
            var body = activator.GetComponent<CharacterBody>();
            if (body)
            {
                if (body.master)
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

        var inspect = ScriptableObject.CreateInstance<InspectDef>();
        var info = new RoR2.UI.InspectInfo();
        info.Visual = AwakeningShrineSprite;
        info.DescriptionToken = $"INTERACTABLE_{InteractableLangToken}_INSPECT";
        info.TitleToken = $"INTERACTABLE_{InteractableLangToken}_TITLE";
        inspect.Info = info;



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