using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Lunar;

internal class LunarScourgeDebuff : Buff<LunarScourgeDebuff>
{
    public override string Name => "LunarScourgeDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffVoidFog.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(0, 0, 255, 255);

    public override bool IgnoreGrowthNectar => true;

    public override bool IsDOT => true;

    public override void Init()
    {
        CreateBuff();
    }
}
