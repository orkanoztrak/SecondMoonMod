using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;
using R2API;

namespace SecondMoon.Items.ItemTiers.VoidTierPrototypeDormant;

public class VoidTierPrototypeDormant : Tier<VoidTierPrototypeDormant>
{
    public override string Name => "VoidTierPrototypeDormantDef";

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
        GameObject VFX = Temp.transform.Find("VoidSystem").gameObject.InstantiateClone("VoidPrototypeDormantVFX", false);

        PickupDisplayVFX = VFX;
    }
}
