using BepInEx.Configuration;
using HarmonyLib;
using MonoMod.Cil;
using On.RoR2.Items;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SecondMoon.Utils;
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
    public abstract string ItemName { get; }
    public abstract string ItemLangTokenName { get; }
    public abstract string ItemPickupDesc { get; }
    public abstract string ItemFullDesc { get; }
    public abstract string ItemLore { get; }
    public abstract ItemTierDef ItemTierDef { get; }
    public abstract ItemTag[] Category { get; }
    public virtual GameObject ItemModel { get; } = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    public virtual Sprite ItemIcon { get; } = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();

    public static ConfigOption<bool> IsEnabled;

    public ItemDef ItemDef;

    public bool EnableCheck;

    public virtual ItemDef ItemToCorrupt { get; } = null;

    public virtual List<ItemDef> ItemsToCorrupt { get; } = null;

    public virtual ItemTierDef ItemTierToCorrupt { get; } = null;

    public virtual UnlockableDef ItemUnlockableDef { get; set; } = null;

    public virtual bool CanRemove { get; } = true;

    public virtual bool PrinterBlacklisted { get; set; } = false;

    public virtual bool Unlockable { get; set; } = true;
    public virtual ExpansionDef RequiredExpansion { get; set; } = null;
    protected virtual ItemDisplayRuleDict DisplayRules { get; set; } = null;

    public virtual ItemIndex ActivateIntoPrototypeItem { get; set; } = ItemIndex.None;

    public virtual EquipmentIndex ActivateIntoPrototypeEquipment { get; set; } = EquipmentIndex.None;

    public virtual void Init(ConfigFile config)
    {
        IsEnabled = config.ActiveBind<bool>("Item: " + ItemName, "Should this be enabled?", true, "If false, this item will not appear in the game.");
        EnableCheck = IsEnabled;
    }

    protected void CreateLang()
    {
        LanguageAPI.Add("SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_NAME", ItemName);
        LanguageAPI.Add("SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_PICKUP", ItemPickupDesc);
        LanguageAPI.Add("SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_DESCRIPTION", ItemFullDesc);
        LanguageAPI.Add("SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_LORE", ItemLore);
    }

    public abstract ItemDisplayRuleDict CreateItemDisplayRules();

    protected void CreateItem()
    {
        ItemDef = ScriptableObject.CreateInstance<ItemDef>();
        ItemDef.name = "SECONDMOONMOD_ITEM_" + ItemLangTokenName;
        ItemDef.nameToken = "SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_NAME";
        ItemDef.pickupToken = "SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_PICKUP";
        ItemDef.descriptionToken = "SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_DESCRIPTION";
        ItemDef.loreToken = "SECONDMOONMOD_ITEM_" + ItemLangTokenName + "_LORE";
        ItemDef.pickupModelPrefab = ItemModel;
        ItemDef.pickupIconSprite = ItemIcon;
        ItemDef.hidden = false;
        ItemDef.canRemove = CanRemove;
        if (!ItemTierDef)
        {
            ItemDef.deprecatedTier = ItemTier.NoTier;
        }
        else
        {
            ItemDef._itemTierDef = ItemTierDef;
        }
        ItemDef.tags = Category;
        if (Category.Length > 0) { ItemDef.tags = Category; }
        if (PrinterBlacklisted) { SecondMoonPlugin.BlacklistedFromPrinter.Add(ItemDef); }
        if (ItemUnlockableDef) { ItemDef.unlockableDef = ItemUnlockableDef; }
        if (RequiredExpansion) { ItemDef.requiredExpansion = RequiredExpansion; }
        if (ItemToCorrupt) 
        {
            if (!RequiredExpansion) { ItemDef.requiredExpansion = SecondMoonPlugin.DLC1; }
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
