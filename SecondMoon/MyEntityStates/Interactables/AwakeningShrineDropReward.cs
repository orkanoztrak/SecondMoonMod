using SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;
using UnityEngine;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineDropReward : AwakeningShrineBaseState
{
    private Vector3 dropVelocity = new Vector3(0, 0, 1.307143f);
    public override void OnEnter()
    {
        base.OnEnter();
        var shrine = outer.gameObject;
        var dropPivot = shrine.transform.Find("DropPivot");
        if (dropPivot)
        {
            manager.DropPrototypeReward(dropPivot.position, dropVelocity);
        }
    }
}
