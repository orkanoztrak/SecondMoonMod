using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Interactables;

public abstract class Interactable<T> : Interactable where T : Interactable<T>
{
    public static T instance { get; private set; }

    public Interactable()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Item was instantiated twice");
        instance = this as T;
    }
}

public abstract class Interactable
{
    public abstract string InteractableName { get; }

    public abstract string InteractableContext { get; }

    public abstract string InteractableLangToken { get; }

    public abstract string InteractableInspectDesc { get; }

    public abstract GameObject InteractableModel { get; }

    public static ConfigOption<bool> IsEnabled;

    public bool EnableCheck;
    public virtual void Init(ConfigFile config)
    {
        IsEnabled = config.ActiveBind<bool>("Interactable: " + InteractableName, "Should this be enabled?", true, "If false, this interactable will not appear in the game.");
        EnableCheck = IsEnabled;
    }

    protected virtual void CreateLang()
    {
        LanguageAPI.Add("INTERACTABLE_" + InteractableLangToken + "_NAME", InteractableName);
        LanguageAPI.Add("INTERACTABLE_" + InteractableLangToken + "_CONTEXT", InteractableContext);
        LanguageAPI.Add("INTERACTABLE_" + InteractableLangToken + "_INSPECT", InteractableInspectDesc);
        LanguageAPI.Add("INTERACTABLE_" + InteractableLangToken + "_TITLE", InteractableName);
    }

    /*public void AddExpansionComponentToInteractable(GameObject interactable)
    {
        if (!interactable) { return; }

        var expansionComponent = interactable.AddComponent<RoR2.ExpansionManagement.ExpansionRequirementComponent>();
        expansionComponent.requiredExpansion = SecondMoonPlugin.def;

    }*/

    public virtual void Hooks() { }
}