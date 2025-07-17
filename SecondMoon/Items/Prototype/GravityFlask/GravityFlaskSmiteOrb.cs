using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Prototype.GravityFlask;

public class GravityFlaskSmiteOrb : GenericDamageOrb, IOrbFixedUpdateBehavior
{
    private Vector3 lastKnownTargetPosition;
    public override void Begin()
    {
        base.Begin();
        duration = 0f;
        if ((bool)target)
        {
            lastKnownTargetPosition = target.transform.position;
        }
    }

    public override GameObject GetOrbEffect()
    {
        return LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/SimpleLightningStrikeOrbEffect");
    }

    public override void OnArrival()
    {
        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/SimpleLightningStrikeImpact"), new EffectData
        {
            origin = lastKnownTargetPosition
        }, transmit: true);
        if ((bool)attacker)
        {
            new BlastAttack
            {
                attacker = attacker,
                baseDamage = damageValue,
                baseForce = 0f,
                bonusForce = Vector3.down * 1500f,
                crit = isCrit,
                damageColorIndex = DamageColorIndex.Item,
                falloffModel = BlastAttack.FalloffModel.None,
                inflictor = null,
                position = lastKnownTargetPosition,
                procChainMask = procChainMask,
                procCoefficient = 0,
                radius = 3f,
                teamIndex = TeamComponent.GetObjectTeam(attacker)
            }.Fire();
        }
    }

    public void FixedUpdate()
    {
        if ((bool)target)
        {
            lastKnownTargetPosition = target.transform.position;
        }
    }
}
