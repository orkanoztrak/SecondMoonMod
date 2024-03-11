using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
namespace SecondMoon.BuffsAndDebuffs;

public abstract class DOT<T> : DOT where T : DOT<T>
{
    public static T instance { get; private set; }

    public DOT()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting BuffBase was instantiated twice");
        instance = this as T;
    }
}

public abstract class DOT
{
    public abstract float Interval { get; }
    public abstract float DamageCoefficient { get; }
    public abstract DamageColorIndex DamageColorIndex { get; }
    public virtual bool ResetTimerOnAdd { get; } = true;
    public abstract string AssociatedBuffName { get; }
    public BuffDef AssociatedBuff { get; protected set; }
    public DotController.DotIndex DotIndex { get; private set; }
    public virtual DotAPI.CustomDotBehaviour CustomDotBehaviour { get; } = null;
    public virtual DotAPI.CustomDotVisual CustomDotVisual { get; } = null;

    protected void CreateDOT()
    {
        DotIndex = DotAPI.RegisterDotDef(Interval, DamageCoefficient, DamageColorIndex, AssociatedBuff, CustomDotBehaviour, CustomDotVisual);
    }
    public abstract void Init();
    public abstract void Hooks();
    public abstract void SetAssociatedBuff();
    public virtual void CreateCustomDotBehaviour() { }
    public virtual void CreateCustomDotVisual() { }
}
