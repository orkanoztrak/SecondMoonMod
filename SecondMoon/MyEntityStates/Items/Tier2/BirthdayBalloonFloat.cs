using RoR2;
using SecondMoon.Items.Tier2.BirthdayBalloon;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SecondMoon.Items.Tier2.BirthdayBalloon.BirthdayBalloon;

namespace SecondMoon.MyEntityStates.Items.Tier2;

public class BirthdayBalloonFloat : BirthdayBalloonBase
{
    public static float accelerationY = 25f;

    public static float maxFallSpeed = 0.2f;

    private float healTimer;

    public override void OnEnter()
    {
        base.OnEnter();
        healTimer = 0f;
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (isAuthority)
        {
            FixedUpdateAuthority();
        }
    }

    private void FixedUpdateAuthority()
    {
        if (ReturnToIdleIfGroundedAuthority())
        {
            return;
        }
        if (bodyMotor && bodyInputBank)
        {
            bool num = !jumpButtonDown || bodyMotor.velocity.y >= 0f || bodyMotor.isGrounded;
            bool flag = outer.state.GetType() == typeof(BirthdayBalloonFloat);
            if (num && flag)
            {
                outer.SetNextState(new BirthdayBalloonIdle());
                return;
            }
            Vector3 velocity = bodyMotor.velocity;
            if (velocity.y < 0f - maxFallSpeed)
            {
                velocity.y = Mathf.MoveTowards(velocity.y, maxFallSpeed, Time.deltaTime * accelerationY);
            }
            bodyMotor.velocity = velocity;

            healTimer += Time.fixedDeltaTime;
            if (healTimer > BirthdayBalloonHealInterval)
            {
                if (networkedBodyAttachment.attachedBody)
                {
                    var stackCount = BirthdayBalloon.instance.GetCount(networkedBodyAttachment.attachedBody);
                    if (networkedBodyAttachment.attachedBody.healthComponent)
                    {
                        networkedBodyAttachment.attachedBody.healthComponent.Heal(BirthdayBalloon.BirthdayBalloonHealInit + ((stackCount - 1) * BirthdayBalloon.BirthdayBalloonHealStack), default);
                        healTimer -= BirthdayBalloonHealInterval;
                    }
                }
            }
        }
    }
}
