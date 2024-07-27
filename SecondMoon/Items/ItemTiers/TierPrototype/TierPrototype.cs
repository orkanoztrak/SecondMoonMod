using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.ItemTiers.TierPrototype;

public class TierPrototype : Tier<TierPrototype>
{
    public override string Name => "TierPrototypeDef";

    public override Texture BgIconTexture => Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/texTier3BGIcon.png").WaitForCompletion();

    public override ColorCatalog.ColorIndex ColorIndex => ColorCatalog.ColorIndex.Tier3Item;

    public override ColorCatalog.ColorIndex DarkColorIndex => ColorCatalog.ColorIndex.Tier3ItemDark;

    public override bool IsDroppable => true;

    public override bool CanScrap => false;

    public override bool CanRestack => true;

    public override ItemTierDef.PickupRules PickupRules => ItemTierDef.PickupRules.Default;

    public override GameObject HighlightPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HighlightTier3Item.prefab").WaitForCompletion();

    public override GameObject DropletDisplayPrefab => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Tier3Orb.prefab").WaitForCompletion();

    public override void Init()
    {
        base.Init();
        CreateTier();
    }
}
