using SecondMoon.BuffsAndDebuffs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Void;

public class LostBarrierCooldown : Buff<LostBarrierCooldown>
{
    public override string Name => "Lost Barrier Cooldown";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/BearVoid/texBuffBearVoidReady.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(124, 253, 234, 128);

    public override bool IsCooldown => true;

    public override bool IgnoreGrowthNectar => true;

    public override void Init()
    {
        CreateBuff();
    }
}
