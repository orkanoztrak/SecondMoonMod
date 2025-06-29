using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototype;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrineIdle : AwakeningShrineBaseState
{
    public override Interactability GetInteractability(Interactor activator)
    {
        var body = activator.GetComponent<CharacterBody>();
        if (body)
        {
            if (body.master)
            {
                return body.master.inventory.HasAtLeastXTotalItemsOfTier(TierPrototype.instance.ItemTierDef.tier, 1) ? Interactability.Available : Interactability.ConditionsNotMet;
            }
        }
        return Interactability.ConditionsNotMet;
    }
}
