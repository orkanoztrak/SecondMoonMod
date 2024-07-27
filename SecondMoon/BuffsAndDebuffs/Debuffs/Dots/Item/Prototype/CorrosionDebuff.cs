using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;

public class CorrosionDebuff : Buff<CorrosionDebuff>
{
    public override string Name => "CorrosionDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffVoidFog.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(150, 150, 0, 255);

    public override void Init()
    {
        CreateBuff();
    }
}
