using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using UnityEngine;
using static SecondMoon.Items.Prototype.FlailOfMass.FlailOfMass;
namespace SecondMoon.AttackTypes.Orbs.Item.Prototype.FlailOfMass;

public class FlailOrb : GenericDamageOrb
{
    private GameObject effectPrefab;

    public GameObject inflictor;
    public override void Begin()
    {
        duration = distanceToTarget / 20f;
        EffectData effectData = new EffectData
        {
            origin = origin,
            genericFloat = duration
        };
        effectData.SetHurtBoxReference(target);
        effectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/DevilOrbEffect");
        EffectManager.SpawnEffect(effectPrefab, effectData, true);
    }

    public override void OnArrival()
    {
        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"), new EffectData
        {
            origin = target.transform.position,
            scale = FlailOfMassRadius,
        }, true);
        new BlastAttack
        {
            procChainMask = procChainMask,
            procCoefficient = procCoefficient,
            attacker = attacker,
            inflictor = inflictor,
            teamIndex = teamIndex,
            baseDamage = damageValue,
            baseForce = 2500f,
            falloffModel = BlastAttack.FalloffModel.None,
            crit = isCrit,
            radius = FlailOfMassRadius,
            position = target.transform.position,
            damageColorIndex = damageColorIndex,
            attackerFiltering = AttackerFiltering.NeverHitSelf
        }.Fire();
    }
}
