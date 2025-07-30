using EntityStates;
using RoR2;
using SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineBaseState : BaseState
{
    protected AwakeningShrineManager manager { get; private set; }

    public override void OnEnter()
    {
        base.OnEnter();
        manager = outer.gameObject.GetComponent<AwakeningShrineManager>();
    }

    public virtual string GetContextString(Interactor activator)
    {
        return null;
    }
}
