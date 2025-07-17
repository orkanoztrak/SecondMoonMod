using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;

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

    public override void OnArrival()
    {
        base.OnArrival();
        if (target && attacker)
        {
            var attackerBody = attacker.GetComponent<CharacterBody>();
            if (attackerBody && target.healthComponent)
            {
                if (attackerBody.master)
                {
                    if (Util.CheckRoll(TwistedRegrets.TwistedRegretsLostOrbCollapseChance, attackerBody.master))
                    {
                        DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                        DotController.InflictDot(target.healthComponent.gameObject, attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f);
                    }
                }
            }
        }
    }
}
