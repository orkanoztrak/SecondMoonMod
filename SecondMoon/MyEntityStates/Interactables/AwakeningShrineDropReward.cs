using RoR2;
using SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineDropReward : AwakeningShrineBaseState
{
    public override void OnEnter()
    {
        base.OnEnter();
        var shrine = outer.gameObject;
        var dropPivot = shrine.transform.Find("DropPivot");
        AwakeningShrineManager.DropItem(manager, dropPivot, );
    }
}
