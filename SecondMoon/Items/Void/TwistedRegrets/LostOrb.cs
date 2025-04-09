using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;

namespace SecondMoon.Items.Void.TwistedRegrets;

public class LostOrb : GenericDamageOrb
{
    public override void Begin()
    {
        speed = 75f;
        base.Begin();
    }

    public override GameObject GetOrbEffect()
    {
        return Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/MissileVoid/MissileVoidOrbEffect.prefab").WaitForCompletion();
    }
}
