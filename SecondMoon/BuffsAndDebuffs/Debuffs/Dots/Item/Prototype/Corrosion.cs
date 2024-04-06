using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;

public class Corrosion : DOT<Corrosion>
{
    public override float Interval => 0.5f;

    public override float DamageCoefficient => 0.2f;

    public override DamageColorIndex DamageColorIndex => DamageColorIndex.Poison;

    public override string AssociatedBuffName => "CorrosionDebuff";

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
        this.AssociatedBuff = CorrosionDebuff.instance.BuffDef;
    }
}
