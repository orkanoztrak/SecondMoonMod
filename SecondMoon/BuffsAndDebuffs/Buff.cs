using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace SecondMoon.BuffsAndDebuffs;

public abstract class Buff<T> : Buff where T : Buff<T>
{
    public static T instance { get; private set; }

    public Buff()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Buff was instantiated twice");
        instance = this as T;
    }
}

public abstract class Buff
{
    public abstract string Name { get; }
    public abstract Sprite IconSprite { get; }
    public abstract Color BuffColor { get; }
    public virtual bool CanStack { get; } = true;
    public virtual EliteDef EliteDef { get; } = null;
    public virtual bool IsDebuff { get; } = false;
    public virtual bool IsCooldown { get; } = false;
    public virtual bool IsHidden { get; } = false;
    public virtual NetworkSoundEventDef StartSfx { get; } = null;

    public BuffDef BuffDef;
    protected void CreateBuff()
    {
        BuffDef = ScriptableObject.CreateInstance<BuffDef>();
        BuffDef.name = Name;
        BuffDef.iconSprite = IconSprite;
        BuffDef.buffColor = BuffColor;
        BuffDef.canStack = CanStack;
        BuffDef.eliteDef = EliteDef;
        BuffDef.isDebuff = IsDebuff;
        BuffDef.isCooldown = IsCooldown;
        BuffDef.isHidden = IsHidden;
        BuffDef.startSfx = StartSfx;
        ContentAddition.AddBuffDef(BuffDef);
    }
    public abstract void Init();
    public virtual void Hooks() { }
}
