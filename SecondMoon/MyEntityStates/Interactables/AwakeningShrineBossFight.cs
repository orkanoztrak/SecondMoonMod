using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineBossFight : AwakeningShrineBaseState
{
    public override void OnEnter()
    {
        base.OnEnter();
        manager.SetupBossSpawn(manager.interactor);
    }
}
