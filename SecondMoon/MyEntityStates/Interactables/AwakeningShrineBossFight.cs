using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineBossFight : AwakeningShrineBaseState
{
    public override void OnEnter()
    {
        base.OnEnter();
        manager.SetupBossSpawn(manager.interactor);
    }
}
