using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.ItemTiers.VoidTierPrototype;

public class VoidTierPrototype : Tier<VoidTierPrototype>
{
    public override string Name => "VoidTierPrototypeDef";

    public override Texture BgIconTexture => Addressables.LoadAssetAsync<Texture>("RoR2/DLC1/Common/IconBackgroundTextures/texVoidBGIcon.png").WaitForCompletion();

    public override ColorCatalog.ColorIndex ColorIndex => ColorCatalog.ColorIndex.VoidItem;

    public override ColorCatalog.ColorIndex DarkColorIndex => ColorCatalog.ColorIndex.VoidItemDark;

    public override bool IsDroppable => true;

    public override bool CanScrap => false;

    public override bool CanRestack => true;

    public override ItemTierDef.PickupRules PickupRules => ItemTierDef.PickupRules.ConfirmFirst;

    public override GameObject HighlightPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier1Item.prefab").WaitForCompletion();

    public override void Init()
    {
        base.Init();
        DropletDisplayPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/VoidOrb.prefab").WaitForCompletion();
        CreateVFX();
        CreateTier();
    }

    private void CreateVFX()
    {
        Color colorLight = ColorCatalog.GetColor(ColorIndex);
        Color colorDark = ColorCatalog.GetColor(DarkColorIndex);

        GameObject Temp = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/GenericPickup.prefab").WaitForCompletion();
        GameObject VFX = Temp.transform.Find("VoidSystem").gameObject.InstantiateClone("VoidPrototypeVFX", false);

        UnityEngine.Object.Destroy(VFX.transform.Find("Loops").Find("Swirls").gameObject);

        GameObject borrowedSwirls = Temp.transform.Find("LunarSystem").Find("Loops").Find("Swirls").gameObject.InstantiateClone("Swirls", false);
        ParticleSystem.MainModule color00 = borrowedSwirls.GetComponent<ParticleSystem>().main;
        color00.startColor = new ParticleSystem.MinMaxGradient(colorLight, colorDark);
        color00.startLifetime = new ParticleSystem.MinMaxCurve(1, 3);
        color00.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.18f);
        ParticleSystem.EmissionModule emission = borrowedSwirls.GetComponent<ParticleSystem>().emission;
        emission.rateOverTime = 2f;
        borrowedSwirls.transform.SetParent(VFX.transform.Find("Loops"));

        PickupDisplayVFX = VFX;
    }
}
