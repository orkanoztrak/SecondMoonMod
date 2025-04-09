using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.ItemTiers.TierPrototype;

public class TierPrototype : Tier<TierPrototype>
{
    public override string Name => "TierPrototypeDef";

    public override Texture BgIconTexture => SecondMoonPlugin.SecondMoonAssets.LoadAsset<Texture>("Assets/SecondMoonAssets/Textures/Icons/Items/Prototype/texPrototypeBGIcon.png");

    public override ColorCatalog.ColorIndex ColorIndex => SecondMoonModColors.PrototypeColorIndex;

    public override ColorCatalog.ColorIndex DarkColorIndex => SecondMoonModColors.PrototypeDarkColorIndex;

    public override bool IsDroppable => true;

    public override bool CanScrap => false;

    public override bool CanRestack => true;

    public override ItemTierDef.PickupRules PickupRules => ItemTierDef.PickupRules.Default;

    public override GameObject HighlightPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier3Item.prefab").WaitForCompletion();

    public override void Init()
    {
        base.Init();
        CreateDropletPrefab();
        CreateVFX();
        CreateTier();
        On.RoR2.EquipmentSlot.UpdateTargets += InvalidateRecyclingForEquipment;
    }

    private void InvalidateRecyclingForEquipment(On.RoR2.EquipmentSlot.orig_UpdateTargets orig, EquipmentSlot self, EquipmentIndex targetingEquipmentIndex, bool userShouldAnticipateTarget)
    {
        orig(self, targetingEquipmentIndex, userShouldAnticipateTarget);
        if (targetingEquipmentIndex == RoR2Content.Equipment.Recycle.equipmentIndex)
        {
            var controller = self.currentTarget.pickupController;
            if (controller)
            {
                var equipmentIndex = (EquipmentIndex)Array.IndexOf(PickupCatalog.equipmentIndexToPickupIndex, controller.pickupIndex);
                var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (GeneralUtils.IsSecondMoonPrototypeEquipment(equipmentDef, out _))
                {
                    self.currentTarget = default;
                    self.targetIndicator.active = false;
                }
            }
        }
    }

    private void CreateVFX()
    {
        Color colorLight = ColorCatalog.GetColor(ColorIndex);
        Color colorDark = ColorCatalog.GetColor(DarkColorIndex);

        GameObject Temp = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
        GameObject VFX = Temp.transform.Find("Tier3System").gameObject.InstantiateClone("PrototypeVFX", false);

        ParticleSystem.MainModule color00 = VFX.transform.Find("Loops").Find("DistantSoftGlow").GetComponent<ParticleSystem>().main;
        color00.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);

        VFX.transform.Find("Loops").Find("Point Light").gameObject.GetComponent<Light>().color = colorLight;
        VFX.transform.Find("Loops").Find("Point Light").gameObject.GetComponent<Light>().set_color_Injected(ref colorLight);

        ParticleSystem.MainModule color04 = VFX.transform.Find("Loops").Find("Glowies").GetComponent<ParticleSystem>().main;
        color04.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);
        UnityEngine.Object.Destroy(VFX.transform.Find("Loops").Find("Lightning").gameObject);
        UnityEngine.Object.Destroy(VFX.transform.Find("Loops").Find("Swirls").gameObject);
        GameObject borrowedSwirls = Temp.transform.Find("LunarSystem").Find("Loops").Find("Swirls").gameObject.InstantiateClone("Swirls", false);
        ParticleSystem.MainModule color05 = borrowedSwirls.GetComponent<ParticleSystem>().main;
        color05.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);
        color05.startLifetime = new ParticleSystem.MinMaxCurve(1, 3);
        color05.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.18f);
        ParticleSystem.EmissionModule emission = borrowedSwirls.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 2f;
        borrowedSwirls.transform.SetParent(VFX.transform.Find("Loops"));

        PickupDisplayVFX = VFX;
    }

    private void CreateDropletPrefab()
    {
        GameObject droplet = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Tier3Orb.prefab").WaitForCompletion().InstantiateClone("PrototypeOrb", false);
        Color colorLight = ColorCatalog.GetColor(ColorIndex);
        Color colorDark = ColorCatalog.GetColor(DarkColorIndex);

        droplet.transform.Find("VFX").gameObject.GetComponent<TrailRenderer>().startColor = colorLight;
        droplet.transform.Find("VFX").gameObject.GetComponent<TrailRenderer>().set_startColor_Injected(ref colorLight);

        Light[] lights = droplet.GetComponentsInChildren<Light>();
        foreach (Light thisLight in lights)
        {
            thisLight.color = colorLight;
        }

        ParticleSystem[] array = droplet.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem obj in array)
        {
            ParticleSystem.MainModule main = obj.main;
            ParticleSystem.ColorOverLifetimeModule COL = obj.colorOverLifetime;
            main.startColor = new ParticleSystem.MinMaxGradient(colorLight);
            COL.color = colorLight;
        }
        DropletDisplayPrefab = droplet;
    }
}
