using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;

public class Momentum : Buff<Momentum>
{
    public override string Name => "Momentum";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(124, 253, 234, 255);

    public override void Hooks()
    {
        
    }

    public override void Init()
    {
        CreateBuff();
    }
}
