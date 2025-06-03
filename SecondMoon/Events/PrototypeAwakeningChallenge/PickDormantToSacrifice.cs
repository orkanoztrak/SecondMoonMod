using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.Events.PrototypeAwakeningChallenge;

public class PickDormantToSacrifice : EntityState
{
    //called when shrine is interacted with
    //basically a scrapper menu appears and shows a list of prototypes that the player has
    //copy 99% of content from scrapper, except picking the item triggers shrine triggers
    //the cost is NOT avoidable!
    //picking the item makes a teleporter shake-like effect and sets next state to StartBossFight after delay, set its parameters here
    //pickup picker panel is made on unity


}
