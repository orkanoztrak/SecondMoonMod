using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Prototype.Hydra;

public class HydraOrb : GenericDamageOrb
{
    private GameObject effectPrefab;

    public GameObject inflictor;

    public OrbVariant orbVariant;
    public override void Begin()
    {
        duration = 0.1f;
        DamageAPI.AddModdedDamageType(ref damageType, Hydra.HydraOrbLoopPrevention);
        if (orbVariant == OrbVariant.Fire)
        {
            effectPrefab = Hydra.instance.HydraOrbEffectFireVariant;
        }
        else
        {
            effectPrefab = Hydra.instance.HydraOrbEffectLightningVariant;
        }
        if (target)
        {
            if ((bool)target.hurtBoxGroup)
            {
                target = target.hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, target.hurtBoxGroup.hurtBoxes.Length)];
            }
            EffectData effectData = new EffectData
            {
                origin = origin,
                genericFloat = duration
            };
            effectData.SetHurtBoxReference(target);
            EffectManager.SpawnEffect(effectPrefab, effectData, transmit: true);
        }
    }

    public enum OrbVariant
    {
        Fire = 0,
        Lightning = 1
    }
}
