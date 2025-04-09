using RoR2;
using SecondMoon.Items.ItemTiers;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items;

public class ItemTierPickupVFXHelper : MonoBehaviour
{
    [SystemInitializer([typeof(ItemTierCatalog), typeof(Tier)])]
    public static void Init()
    {
        On.RoR2.PickupDisplay.DestroyModel += PickupDisplay_DestroyModel;
        On.RoR2.PickupDisplay.RebuildModel += PickupDisplay_RebuildModel;
    }

    private static void PickupDisplay_RebuildModel(On.RoR2.PickupDisplay.orig_RebuildModel orig, PickupDisplay self, GameObject modelObjectOverride)
    {
        ItemTierPickupVFXHelper ITPVFXHelper = self.gameObject.GetComponent<ItemTierPickupVFXHelper>();
        if (!ITPVFXHelper)
        {
            self.gameObject.AddComponent<ItemTierPickupVFXHelper>();
            ITPVFXHelper = self.gameObject.GetComponent<ItemTierPickupVFXHelper>();
        }
        orig(self, modelObjectOverride);
        ITPVFXHelper.OnPickupDisplayRebuildModel();
    }

    private static void PickupDisplay_DestroyModel(On.RoR2.PickupDisplay.orig_DestroyModel orig, PickupDisplay self)
    {
        ItemTierPickupVFXHelper ITPVFXHelper = self.gameObject.GetComponent<ItemTierPickupVFXHelper>();
        if (!ITPVFXHelper)
        {
            self.gameObject.AddComponent<ItemTierPickupVFXHelper>();
            ITPVFXHelper = self.gameObject.GetComponent<ItemTierPickupVFXHelper>();
        }
        orig(self);
        ITPVFXHelper.OnPickupDisplayDestroyModel();
    }

    private PickupDisplay display;
    private GameObject effectInstance;
    private GameObject dropletInstance;

    private void Awake()
    {
        display = GetComponent<PickupDisplay>();
    }

    private void OnPickupDisplayRebuildModel()
    {
        if (!display || effectInstance)
            return;

        PickupDef pickupDef = PickupCatalog.GetPickupDef(display.pickupIndex);
        ItemIndex itemIndex = pickupDef?.itemIndex ?? ItemIndex.None;
        EquipmentIndex equipmentIndex = pickupDef?.equipmentIndex ?? EquipmentIndex.None;
        if (itemIndex != ItemIndex.None)
        {
            ItemTier tier = ItemCatalog.GetItemDef(itemIndex).tier;
            ItemTierDef tierDef = ItemTierCatalog.GetItemTierDef(tier);
            if (tierDef && GeneralUtils.IsSecondMoonCustomTier(tierDef, out var itemTier))
            {
                if (itemTier != null && itemTier?.PickupDisplayVFX)
                {
                    effectInstance = Instantiate(itemTier.PickupDisplayVFX, display.gameObject.transform);
                    effectInstance.transform.position -= Vector3.up * 1.5f;
                    effectInstance.SetActive(true);

                    Color32 color = ColorCatalog.GetColor(tierDef.colorIndex);

                    ParticleSystem[] array = effectInstance.GetComponentsInChildren<ParticleSystem>();
                    foreach (ParticleSystem obj in array)
                    {
                        ((Component)obj).gameObject.SetActive(true);
                        ParticleSystem.MainModule main = obj.main;
                        main.startColor = new ParticleSystem.MinMaxGradient(color);
                    }
                }
            }
        }
        if (equipmentIndex != EquipmentIndex.None)
        {
            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            if (equipmentDef && GeneralUtils.IsSecondMoonPrototypeEquipment(equipmentDef, out _))
            {
                effectInstance = Instantiate(ItemTiers.TierPrototype.TierPrototype.instance.PickupDisplayVFX, display.gameObject.transform);
                effectInstance.transform.position -= Vector3.up * 1.5f;
                effectInstance.SetActive(true);

                Color32 color = ColorCatalog.GetColor(SecondMoonModColors.PrototypeColorIndex);

                ParticleSystem[] array = effectInstance.GetComponentsInChildren<ParticleSystem>();
                foreach (ParticleSystem obj in array)
                {
                    ((Component)obj).gameObject.SetActive(true);
                    ParticleSystem.MainModule main = obj.main;
                    main.startColor = new ParticleSystem.MinMaxGradient(color);
                }
            }
        }
    }

    private void OnPickupDisplayDestroyModel()
    {
        if (effectInstance)
        {
            Destroy(effectInstance);
        }
    }
}
