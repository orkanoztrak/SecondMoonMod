using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Items.Void.GazingSoulstealer;

public class GazingSoulstealer : Item<GazingSoulstealer>
{
    public static ConfigOption<float> GazingSoulstealerEquipmentCDRInit;
    public static ConfigOption<float> GazingSoulstealerEquipmentCDRStack;
    public static ConfigOption<string> GazingSoulstealerBlacklistedEquipmentNames;
    public static List<EquipmentIndex> GazingSoulstealerBlacklistedEquipment = new List<EquipmentIndex>();
    public override string ItemName => "Gazing Soulstealer";

    public override string ItemLangTokenName => "TALISMANVOID";

    public override string ItemPickupDesc => "Activating your Equipment resets skill cooldowns. <style=cIsVoid>Corrupts all Soulbound Catalysts</style>.";

    public override string ItemFullDesc => $"<style=cIsUtility>Activating your Equipment</style> resets <style=cIsUtility>skill cooldowns</style>. " +
        $" <style=cIsUtility>Reduce equipment cooldown</style> by <style=cIsUtility>{GazingSoulstealerEquipmentCDRInit * 100}%</style> <style=cStack>(+{GazingSoulstealerEquipmentCDRStack * 100}% per stack)</style>. " +
        $"<style=cIsVoid>Corrupts all Soulbound Catalysts</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.EquipmentRelated, ItemTag.Utility];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/Talisman/Talisman.asset").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += GazingSoulstealerSetUpBlacklistedEquipment;
        EquipmentSlot.onServerEquipmentActivated += GazingSoulstealerResetCooldowns;
        IL.RoR2.Inventory.CalculateEquipmentCooldownScale += GazingSoulstealerReduceEquipmentCooldown;
    }

    private void GazingSoulstealerSetUpBlacklistedEquipment()
    {
        string names = GazingSoulstealerBlacklistedEquipmentNames;
        var list = names.Split(',').ToList();
        foreach (var equipment in EquipmentCatalog.equipmentDefs)
        {
            if (list.Contains(equipment.name))
            {
                GazingSoulstealerBlacklistedEquipment.Add(equipment.equipmentIndex);
            }
        }
    }

    private void GazingSoulstealerReduceEquipmentCooldown(ILContext il)
    {
        var numIndex = 3;
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchRet());

        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<float, Inventory, float>>((total, inv) =>
        {
            var stackCount = inv.GetItemCount(ItemDef);
            if (stackCount > 0) 
            {
                return total * (1 - GazingSoulstealerEquipmentCDRInit) * (float)Math.Pow(1 - GazingSoulstealerEquipmentCDRStack, stackCount - 1);
            }
            return total;
        });
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, numIndex);
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, numIndex);
    }

    private void GazingSoulstealerResetCooldowns(EquipmentSlot slot, EquipmentIndex ındex)
    {
        if (!GazingSoulstealerBlacklistedEquipment.Contains(ındex))
        {
            if (slot)
            {
                if (slot.characterBody)
                {
                    var stackCount = GetCount(slot.characterBody);
                    if (stackCount > 0)
                    {
                        slot.characterBody.skillLocator.primary.rechargeStopwatch += slot.characterBody.skillLocator.primary.cooldownRemaining;
                        slot.characterBody.skillLocator.secondary.rechargeStopwatch += slot.characterBody.skillLocator.secondary.cooldownRemaining;
                        slot.characterBody.skillLocator.utility.rechargeStopwatch += slot.characterBody.skillLocator.utility.cooldownRemaining;
                        slot.characterBody.skillLocator.special.rechargeStopwatch += slot.characterBody.skillLocator.special.cooldownRemaining;
                    }
                }
            }
        }
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        GazingSoulstealerEquipmentCDRInit = config.ActiveBind("Item: " + ItemName, "Equipment cooldown reduction with one " + ItemName, 0.15f, "How much should equipment cooldown be reduced by with one " + ItemName + "? This scales exponentially (0.15 = 15%, refer to Fuel Cell on the wiki).");
        GazingSoulstealerEquipmentCDRStack = config.ActiveBind("Item: " + ItemName, "Equipment cooldown reduction per stack after one " + ItemName, 0.15f, "How much should equipment cooldown be reduced by per stack of " + ItemName + " after one? This scales exponentially (0.15 = 15%, refer to Fuel Cell on the wiki).");
        GazingSoulstealerBlacklistedEquipmentNames = config.ActiveBind("Item: " + ItemName, "Blacklisted equipment names", "MultiShopCard", "These equipment do not reset cooldowns upon activation. Uses case sensitive, comma separated values with no spaces in between (MultiShopCard,BFG for example disables Executive Card and Preon Accumulator).");
    }
}
