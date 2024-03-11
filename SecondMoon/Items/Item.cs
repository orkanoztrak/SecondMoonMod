﻿using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items;

//The Item base class was taken almost directly from the Aetherium mod by KomradeSpectre.
//https://github.com/KomradeSpectre/AetheriumMod
public abstract class Item<T> : Item where T : Item<T>
{
    public static T instance { get; private set; }

    public Item()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Item was instantiated twice");
        instance = this as T;
    }
}

public abstract class Item
{
    public Item() { }
    public abstract string ItemName { get; }
    public abstract string ItemLangTokenName { get; }
    public abstract string ItemPickupDesc { get; }
    public abstract string ItemFullDesc { get; }
    public abstract string ItemLore { get; }
    public abstract ItemTier ItemTier { get; }
    public virtual ItemTag[] Category { get; set; } = new ItemTag[] { };
    //public abstract GameObject ItemModel { get; }
    //public abstract Sprite ItemIcon { get; }

    public ItemDef ItemDef;

    public virtual string CorruptsItem { get; set; } = null;

    public virtual UnlockableDef ItemUnlockableDef { get; set; } = null;

    public virtual bool CanRemove { get; } = true;

    public virtual bool AIBlacklisted { get; set; } = false;

    public virtual bool PrinterBlacklisted { get; set; } = false;

    public virtual bool Unlockable { get; set; } = true;

    protected virtual ItemDisplayRuleDict displayRules { get; set; } = null;
    public abstract void Init();

    protected void CreateLang()
    {
        LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_NAME", ItemName);
        LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
        LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDesc);
        LanguageAPI.Add("ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
    }

    public abstract ItemDisplayRuleDict CreateItemDisplayRules();

    protected void CreateItem()
    {
        if (AIBlacklisted)
        {
            Category.AddItem(ItemTag.AIBlacklist);
        }

        ItemDef = ScriptableObject.CreateInstance<ItemDef>();
        ItemDef.name = "ITEM_" + ItemLangTokenName;
        ItemDef.nameToken = "ITEM_" + ItemLangTokenName + "_NAME";
        ItemDef.pickupToken = "ITEM_" + ItemLangTokenName + "_PICKUP";
        ItemDef.descriptionToken = "ITEM_" + ItemLangTokenName + "_DESCRIPTION";
        ItemDef.loreToken = "ITEM_" + ItemLangTokenName + "_LORE";
        //ItemDef.pickupModelPrefab = ItemModel;
        //ItemDef.pickupIconSprite = ItemIcon;
        ItemDef.hidden = false;
        ItemDef.canRemove = CanRemove;
        ItemDef.deprecatedTier = ItemTier;
        ItemDef.tags = Category;
        ItemDef.pickupIconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();
        ItemDef.pickupModelPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
        if (Category.Length > 0) { ItemDef.tags = Category; }

        if (PrinterBlacklisted)
        {
            SecondMoonPlugin.BlacklistedFromPrinter.Add(ItemDef);
        }

        if (ItemUnlockableDef)
        {
            ItemDef.unlockableDef = ItemUnlockableDef;
        }

        ItemAPI.Add(new CustomItem(ItemDef, CreateItemDisplayRules()));
    }

    public static void BlacklistFromPrinter(ILContext il)
    {
        var c = new ILCursor(il);

        int listIndex = -1;
        int thisIndex = -1;
        c.GotoNext(x => x.MatchSwitch(out _));
        var gotThisIndex = c.TryGotoNext(x => x.MatchLdarg(out thisIndex));
        var gotListIndex = c.TryGotoNext(x => x.MatchLdloc(out listIndex));
        c.GotoNext(MoveType.Before, x => x.MatchCall(out _));
        if (gotThisIndex && gotListIndex)
        {
            c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg, thisIndex);
            c.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, listIndex);
            c.EmitDelegate<Action<ShopTerminalBehavior, List<PickupIndex>>>((shopTerminalBehavior, list) =>
            {
                if (shopTerminalBehavior && shopTerminalBehavior.gameObject.name.Contains("Duplicator"))
                {
                    list.RemoveAll(x => SecondMoonPlugin.BlacklistedFromPrinter.Contains(ItemCatalog.GetItemDef(PickupCatalog.GetPickupDef(x).itemIndex)));
                }
            });
        }
    }

    public abstract void Hooks();

    public int GetCount(CharacterBody body)
    {
        if (!body || !body.inventory) { return 0; }

        return body.inventory.GetItemCount(ItemDef);
    }

    public int GetCount(CharacterMaster master)
    {
        if (!master || !master.inventory) { return 0; }

        return master.inventory.GetItemCount(ItemDef);
    }

    public int GetCountSpecific(CharacterBody body, ItemDef itemDef)
    {
        if (!body || !body.inventory) { return 0; }

        return body.inventory.GetItemCount(itemDef);
    }
}
