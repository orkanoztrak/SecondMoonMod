using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.ItemTiers.TierPrototypeDormant;

public class TierPrototypeDormant : Tier<TierPrototypeDormant>
{
    public override string Name => "TierPrototypeDormantDef";

    public override Texture BgIconTexture => SecondMoonPlugin.SecondMoonAssets.LoadAsset<Texture>("Assets/SecondMoonAssets/Textures/Icons/Items/Prototype/texPrototypeBGIcon.png");

    public override ColorCatalog.ColorIndex ColorIndex => SecondMoonModColors.PrototypeColorIndex;

    public override ColorCatalog.ColorIndex DarkColorIndex => SecondMoonModColors.PrototypeDarkColorIndex;

    public override bool IsDroppable => true;

    public override bool CanScrap => false;

    public override bool CanRestack => true;

    public override ItemTierDef.PickupRules PickupRules => ItemTierDef.PickupRules.Default;

    public override GameObject HighlightPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();

    public override void Init()
    {
        base.Init();
        CreateDropletPrefab();
        CreateVFX();
        CreateTier();
    }

    private void CreateVFX()
    {
        Color colorLight = ColorCatalog.GetColor(ColorIndex);
        Color colorDark = ColorCatalog.GetColor(DarkColorIndex);

        GameObject Temp = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
        GameObject VFX = Temp.transform.Find("Tier1System").gameObject.InstantiateClone("DormantPrototypeVFX", false);

        ParticleSystem.MainModule color00 = VFX.transform.Find("Loops").Find("DistantSoftGlow").GetComponent<ParticleSystem>().main;
        color00.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);

        VFX.transform.Find("Loops").Find("Point Light").gameObject.GetComponent<Light>().color = colorLight;
        VFX.transform.Find("Loops").Find("Point Light").gameObject.GetComponent<Light>().set_color_Injected(ref colorLight);

        ParticleSystem.MainModule color04 = VFX.transform.Find("Loops").Find("Glowies").GetComponent<ParticleSystem>().main;
        color04.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);
        PickupDisplayVFX = VFX;
    }

    private void CreateDropletPrefab()
    {
        GameObject droplet = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Tier1Orb.prefab").WaitForCompletion().InstantiateClone("DormantPrototypeOrb", false);
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
