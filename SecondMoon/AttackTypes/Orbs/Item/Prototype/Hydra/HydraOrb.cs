using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.AttackTypes.Orbs.Item.Prototype.Hydra;

public class HydraOrb : GenericDamageOrb, IOrbFixedUpdateBehavior
{
    private float accumulatedTime;

    public int totalStrikes;

    public float secondsPerStrike = 0.5f;

    private GameObject effectPrefab;

    public GameObject inflictor;

    public override void Begin()
    {
        accumulatedTime = 0f;
        base.duration = (float)(totalStrikes - 1) * secondsPerStrike;
        effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/VoidLightningOrbEffect");
        Strike();
    }

    public override void OnArrival()
    {
    }

    public void FixedUpdate()
    {
        accumulatedTime += Time.fixedDeltaTime;
        while (accumulatedTime > secondsPerStrike)
        {
            accumulatedTime -= secondsPerStrike;
            if ((bool)target)
            {
                origin = target.transform.position;
                Strike();
            }
        }
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
            DamageInfo damageInfo = new DamageInfo();
            damageInfo.damage = damageValue;
            damageInfo.attacker = attacker;
            damageInfo.inflictor = inflictor;
            damageInfo.force = Vector3.zero;
            damageInfo.crit = isCrit;
            damageInfo.procChainMask = procChainMask;
            damageInfo.procCoefficient = procCoefficient;
            damageInfo.position = target.transform.position;
            damageInfo.damageColorIndex = damageColorIndex;
            damageInfo.damageType = damageType;
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
}
