using RoR2;
using SecondMoon.Items.Tier2.BirthdayBalloon;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.EntityStates.Items.Tier2;

public class BirthdayBalloonFloat : BirthdayBalloonBase
{
    public static float hoverVelocity = 0.1f;

    public static float hoverAcceleration = 0.1f;

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
            bool num = jumpButtonDown && bodyMotor.velocity.y < 0f && !bodyMotor.isGrounded;
            bool flag = outer.state.GetType() == typeof(BirthdayBalloonFloat);
            if (!num && flag)
            {
                outer.SetNextState(new BirthdayBalloonFloat());
            }
            return;
        }
        healTimer += Time.fixedDeltaTime;
        float y = bodyMotor.velocity.y;
        y = Mathf.MoveTowards(y, hoverVelocity, hoverAcceleration * Time.fixedDeltaTime);
        bodyMotor.velocity = new Vector3(bodyMotor.velocity.x, y, bodyMotor.velocity.z);
        while(healTimer > 0.5f)
        {
            if (networkedBodyAttachment.attachedBody)
            {
                var stackCount = BirthdayBalloon.instance.GetCount(networkedBodyAttachment.attachedBody);
                if (networkedBodyAttachment.attachedBody.healthComponent)
                {
                    networkedBodyAttachment.attachedBody.healthComponent.Heal(BirthdayBalloon.BirthdayBalloonHealInit + ((stackCount - 1) * BirthdayBalloon.BirthdayBalloonHealStack), default(ProcChainMask));
                    healTimer -= 0.5f;
                }
            }
        }
    }
}
