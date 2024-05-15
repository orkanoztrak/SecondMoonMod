using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.AttackTypes.Orbs.Item.Prototype.GravityFlask;

public class GravityFlaskSmiteOrb : GenericDamageOrb, IOrbFixedUpdateBehavior
{
    private Vector3 lastKnownTargetPosition;
    public override void Begin()
    {
        base.Begin();
        base.duration = 0f;
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
            BlastAttack blastAttack = new BlastAttack();
            blastAttack.attacker = attacker;
            blastAttack.baseDamage = damageValue;
            blastAttack.baseForce = 0f;
            blastAttack.bonusForce = Vector3.down * 1500f;
            blastAttack.crit = isCrit;
            blastAttack.damageColorIndex = DamageColorIndex.Item;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            blastAttack.inflictor = null;
            blastAttack.position = lastKnownTargetPosition;
            blastAttack.procChainMask = procChainMask;
            blastAttack.procCoefficient = 0;
            blastAttack.radius = 3f;
            blastAttack.teamIndex = TeamComponent.GetObjectTeam(attacker);
            blastAttack.Fire();
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
