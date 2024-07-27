using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Prototype.TotemOfDesign.TotemOfDesign;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;

public class FlawlessDesign : Buff<FlawlessDesign>
{
    public override string Name => "Flawless Design";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texGameResultUnknownIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(124, 253, 234, 255);

    public override void Init()
    {
        CreateBuff();
    }
}
