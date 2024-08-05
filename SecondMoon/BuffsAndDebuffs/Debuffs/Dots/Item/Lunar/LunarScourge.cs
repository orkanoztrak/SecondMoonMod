using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Lunar;

public class LunarScourge : DOT<LunarScourge>
{
    public override float Interval => 0.5f;

    public override float DamageCoefficient => 1/3f;

    public override DamageColorIndex DamageColorIndex => DamageColorIndex.Item;

    public override string AssociatedBuffName => "LunarScourgeDebuff";

    public override void Init()
    {
        SetAssociatedBuff();
        CreateDOT();
    }

    public override void SetAssociatedBuff()
    {
        AssociatedBuff = LunarScourgeDebuff.instance.BuffDef;
    }
}
