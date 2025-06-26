using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineIdle : AwakeningShrineBaseState
{
    public override Interactability GetInteractability(Interactor activator)
    {
        return Interactability.Available;
    }
}
