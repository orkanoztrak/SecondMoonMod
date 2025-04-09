using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Void.Lightbulb;

public class VoidInfusionOrb : Orb
{
    private const float speed = 30f;

    public int maxShieldValue;

    private Inventory targetInventory;

    public override void Begin()
    {
        duration = distanceToTarget / 30f;
        EffectData effectData = new EffectData
        {
            origin = origin,
            genericFloat = duration
        };
        effectData.SetHurtBoxReference(target);
        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OrbEffects/InfusionOrbEffect"), effectData, transmit: true);
        CharacterBody characterBody = target.GetComponent<HurtBox>()?.healthComponent.GetComponent<CharacterBody>();
        if ((bool)characterBody)
        {
            targetInventory = characterBody.inventory;
        }
    }

    public override void OnArrival()
    {
        if ((bool)targetInventory)
        {
            targetInventory.AddInfusionBonus((uint)maxShieldValue);
        }
    }
}
