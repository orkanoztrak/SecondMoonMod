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

    public override GameObject DropletDisplayPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/VoidOrb.prefab").WaitForCompletion();

    public override void Init()
    {
        base.Init();
        CreateTier();
    }
}
