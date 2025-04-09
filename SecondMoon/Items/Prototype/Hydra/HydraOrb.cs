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
        duration = 0.5f;
        if (orbVariant == OrbVariant.Fire)
        {
            effectPrefab = Hydra.instance.HydraOrbEffectFireVariant;
        }
        else
        {
            effectPrefab = Hydra.instance.HydraOrbEffectLightningVariant;
        }
        Strike();
    }

    public override void OnArrival()
    {
    }

    private void Strike()
    {
        if (!target)
        {
            return;
        }
        HealthComponent healthComponent = target.healthComponent;
        if ((bool)healthComponent)
        {
            DamageInfo damageInfo = new()
            {
                damage = damageValue,
                attacker = attacker,
                inflictor = inflictor,
                force = Vector3.zero,
                crit = isCrit,
                procChainMask = procChainMask,
                procCoefficient = procCoefficient,
                position = target.transform.position,
                damageColorIndex = damageColorIndex,
                damageType = damageType
            };
            healthComponent.TakeDamage(damageInfo);
            GlobalEventManager.instance.OnHitEnemy(damageInfo, healthComponent.gameObject);
            GlobalEventManager.instance.OnHitAll(damageInfo, healthComponent.gameObject);
            if ((bool)target.hurtBoxGroup)
            {
                target = target.hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, target.hurtBoxGroup.hurtBoxes.Length)];
            }
            EffectData effectData = new EffectData
            {
                origin = origin,
                genericFloat = 0.1f
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
