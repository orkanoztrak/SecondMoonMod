using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;

internal class Gash : DOT<Gash>
{
    public override float Interval => 0.2f;

    public override float DamageCoefficient => 0.1f;

    public override DamageColorIndex DamageColorIndex => DamageColorIndex.SuperBleed;

    public override string AssociatedBuffName => "GashDebuff";

    public override void Hooks()
    {

    }

    public override void Init()
    {
        SetAssociatedBuff();
        CreateDOT();
    }

    public override void SetAssociatedBuff()
    {
        this.AssociatedBuff = GashDebuff.instance.BuffDef;
    }
}
