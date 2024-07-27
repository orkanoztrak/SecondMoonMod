using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace SecondMoon.Elites;

public abstract class Elite<T> : Elite where T : Elite<T>
{
    public static T instance { get; private set; }

    public Elite()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Elite was instantiated twice");
        instance = this as T;
    }
}

public abstract class Elite
{
    public abstract string EliteName { get; }
    public abstract string EliteToken { get; }
    public virtual EquipmentDef EliteEquipmentDef { get; } = null;
    public abstract Color32 Color { get; }
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
        if (EliteEquipmentDef)
        {
            EliteDef.eliteEquipmentDef = EliteEquipmentDef;
        }
        EliteDef.color = Color;
        EliteDef.healthBoostCoefficient = HealthBoostCoefficient;
        EliteDef.damageBoostCoefficient = DamageBoostCoefficient;
        ContentAddition.AddEliteDef(EliteDef);
        R2API.EliteRamp.AddRamp(EliteDef, EliteRamp);
    }

    public abstract void Init();
    public virtual void Hooks() { }

}
