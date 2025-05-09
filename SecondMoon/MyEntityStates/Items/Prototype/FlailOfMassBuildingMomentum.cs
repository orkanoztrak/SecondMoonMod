﻿using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SecondMoon.Items.Prototype.FlailOfMass.FlailOfMass;

namespace SecondMoon.MyEntityStates.Items.Prototype;

public class FlailOfMassBuildingMomentum : FlailOfMassBase
{
    private float sprintTimer;
    private readonly float buildRate = 10.15f / FlailOfMassMomentumBuildRate;
    private float decayTimer;
    private readonly float decayRate = 1f / FlailOfMassMomentumDecayRate;
    public override void OnEnter()
    {
        base.OnEnter();
        sprintTimer = 0f;
        decayTimer = 0f;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (bodyMotor)
        {
            int num = body.GetBuffCount(Momentum.instance.BuffDef.buffIndex);
            bool flag = outer.state.GetType() == typeof(FlailOfMassBuildingMomentum);
            if (num >= FlailOfMassMomentumLimit && flag)
            {
                outer.SetNextState(new FlailOfMassAttackReady());
                return;
            }
            if (body.isSprinting)
            {
                float unchangedCombinedHealth = body.baseMaxHealth + body.levelMaxHealth * (body.level - 1) + (body.baseMaxShield + body.levelMaxShield * (body.level - 1));
                float finalBuildRate = buildRate / (body.moveSpeed * body.healthComponent.fullCombinedHealth / unchangedCombinedHealth);
                sprintTimer += Time.fixedDeltaTime;
                if (sprintTimer >= finalBuildRate && body.moveSpeed > 0f)
                {
                    sprintTimer = 0f;
                    decayTimer = 0f;
                    body.AddBuff(Momentum.instance.BuffDef);
                }
            }
            else
            {
                sprintTimer = 0f;
                decayTimer += Time.fixedDeltaTime;
                if (decayTimer >= decayRate)
                {
                    decayTimer = 0f;
                    if (body.HasBuff(Momentum.instance.BuffDef))
                    {
                        body.SetBuffCount(Momentum.instance.BuffDef.buffIndex, body.GetBuffCount(Momentum.instance.BuffDef.buffIndex) - 1);
                    }
                }
            }
        }
    }
}
