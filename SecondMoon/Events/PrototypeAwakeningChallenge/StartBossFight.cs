using EntityStates;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.Events.PrototypeAwakeningChallenge;

public class StartBossFight : EntityState
{
    //called after prototype to give is chosen
    //spawn card is chosen during runtime
    //change it to always give it the guardian elite equipment
    //picking a void prototype will give the boss void affix as a buff
    //listens to whether the boss is alive or not, sets to idle only if boss is dead
}
