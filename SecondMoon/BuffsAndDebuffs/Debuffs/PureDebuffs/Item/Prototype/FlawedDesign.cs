using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Prototype.TotemOfDesign.TotemOfDesign;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Prototype;

internal class FlawedDesign : Buff<FlawedDesign>
{
    public override string Name => "Flawed Design";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texGameResultWonIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(124, 253, 234, 255);

    public override bool IsDebuff => true;

    public override void Init()
    {
        CreateBuff();
    }
}
