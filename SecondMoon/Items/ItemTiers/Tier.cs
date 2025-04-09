using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using static RoR2.ItemTierDef;
using UnityEngine;
using R2API;
using SecondMoon.Items.ItemTiers.VoidTierPrototype;
using SecondMoon.Items.ItemTiers.TierPrototype;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.ItemTiers;

public abstract class Tier<T> : Tier where T : Tier<T>
{
    public static T instance { get; private set; }

    public Tier()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Tier was instantiated twice");
        instance = this as T;
    }
}

public abstract class Tier
{
    public abstract string Name { get; }
    public abstract Texture BgIconTexture { get; }

    public abstract ColorCatalog.ColorIndex ColorIndex { get; }

    public abstract ColorCatalog.ColorIndex DarkColorIndex { get; }

    public abstract bool IsDroppable { get; }

    public abstract bool CanScrap { get; }

    public abstract bool CanRestack { get; }

    public abstract PickupRules PickupRules { get; }

    public  abstract GameObject HighlightPrefab { get; }

    public GameObject DropletDisplayPrefab;

    public GameObject PickupDisplayVFX;

    public ItemTierDef ItemTierDef;

    public virtual void Init() { }

    protected void CreateTier()
    {
        ItemTierDef = ScriptableObject.CreateInstance<ItemTierDef>();
        ItemTierDef.name = Name;
        ItemTierDef.bgIconTexture = BgIconTexture;
        ItemTierDef.colorIndex = ColorIndex;
        ItemTierDef.darkColorIndex = DarkColorIndex;
        ItemTierDef.isDroppable = IsDroppable;
        ItemTierDef.canScrap = CanScrap;
        ItemTierDef.canRestack = CanRestack;
        ItemTierDef.pickupRules = PickupRules;
        ItemTierDef.highlightPrefab = HighlightPrefab;
        ItemTierDef.dropletDisplayPrefab = DropletDisplayPrefab;
        ItemTierDef.tier = ItemTier.AssignedAtRuntime;
        ContentAddition.AddItemTierDef(ItemTierDef);
    }
    public virtual void Hooks() { }
}
