using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SecondMoon.Items.Tier3.LightningRing.LightningRing;

namespace SecondMoon.AttackTypes.Orbs.Item.Tier3.LightningRing;

public class LightningRingOrb : GenericDamageOrb
{
    private GameObject effectPrefab;
    public override void Begin()
    {
        duration = 0.15f;
        EffectData effectData = new EffectData
        {
            origin = origin,
            genericFloat = duration
        };
        effectData.SetHurtBoxReference(target);
        effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/LightningOrbEffect");
        EffectManager.SpawnEffect(effectPrefab, effectData, true);
    }
}
