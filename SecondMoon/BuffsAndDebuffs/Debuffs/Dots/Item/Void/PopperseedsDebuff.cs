using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Void;

public class PopperseedsDebuff : Buff<PopperseedsDebuff>
{
    public override string Name => "PopperseedsDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Treebot/texBuffEntangleIcon.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(255, 0, 100, 255);

    public override void Init()
    {
        CreateBuff();
    }
}
