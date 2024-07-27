using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;

public class GashDebuff : Buff<GashDebuff>
{
    public override string Name => "GashDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Junk/Common/texBuffSuperBleedIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(90, 0, 0, 255);

    public override void Init()
    {
        CreateBuff();
    }
}
