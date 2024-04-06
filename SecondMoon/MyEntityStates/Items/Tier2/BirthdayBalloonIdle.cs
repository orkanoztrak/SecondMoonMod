using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Items.Tier2;

public class BirthdayBalloonIdle : BirthdayBalloonBase
{
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
        if (bodyMotor && bodyInputBank)
        {
            bool num = jumpButtonDown && bodyMotor.velocity.y < 0f && !bodyMotor.isGrounded;
            bool flag = outer.state.GetType() == typeof(BirthdayBalloonFloat);
            if (num && !flag)
            {
                outer.SetNextState(new BirthdayBalloonFloat());
            }
        }
    }
}
