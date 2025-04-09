using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SecondMoon.EquipmentlessElites;

public abstract class EquipmentlessElite<T> : EquipmentlessElite where T : EquipmentlessElite<T>
{
    public static T instance { get; private set; }

    public EquipmentlessElite()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Elite was instantiated twice");
        instance = this as T;
    }
}

public abstract class EquipmentlessElite
{
    public abstract string EliteName { get; }
    public abstract string EliteToken { get; }
    public abstract float HealthBoostCoefficient { get; }
    public abstract float DamageBoostCoefficient { get; }
    public abstract Texture2D EliteRamp { get; }

    public EliteDef EliteDef;

    protected void CreateLang()
    {
        LanguageAPI.Add("SECONDMOONMOD_ELITE_" + EliteToken + "_MODIFIER", EliteName + " {0}");
    }

    protected void CreateElite()
    {
        EliteDef = ScriptableObject.CreateInstance<EliteDef>();
        EliteDef.name = "SECONDMOONMOD_ELITE_" + EliteToken;
        EliteDef.modifierToken = "SECONDMOONMOD_ELITE_" + EliteToken + "_MODIFIER";
        EliteDef.healthBoostCoefficient = HealthBoostCoefficient;
        EliteDef.damageBoostCoefficient = DamageBoostCoefficient;
        R2API.EliteRamp.AddRamp(EliteDef, EliteRamp);
        ContentAddition.AddEliteDef(EliteDef);
    }
    public abstract void Init();
    public virtual void Hooks() { }

}
