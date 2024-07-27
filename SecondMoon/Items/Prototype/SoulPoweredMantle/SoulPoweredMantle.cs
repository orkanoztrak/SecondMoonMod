using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.UIElements.StyleSheets;
using static SecondMoon.Items.Void.BlissfulVisage.BlissfulVisageSuicideComponent;

namespace SecondMoon.Items.Prototype.SoulPoweredMantle;

public class SoulPoweredMantle : Item<SoulPoweredMantle>
{
    public static ConfigOption<float> SoulPoweredMantleLuckInit;
    public static ConfigOption<float> SoulPoweredMantleLuckStack;
    public static ConfigOption<float> SoulPoweredMantleMaxInvis;
    public static ConfigOption<float> SoulPoweredMantleInvisPercent;
    public static ConfigOption<float> SoulPoweredMantleActivationLowerThreshold;

    public override string ItemName => "Soul-Powered Mantle";

    public override string ItemLangTokenName => "SECONDMOONMOD_SOUL_POWERED_MANTLE";

    public override string ItemPickupDesc => "Using the last charge of a skill makes you invisible, scaling with cooldown. You are very lucky while invisible.";

    public override string ItemFullDesc => $"Using the last charge of a <style=cIsUtility>skill</style> grants you <style=cIsUtility>invisibility</style> and <style=cIsUtility>40% movement speed</style> for <style=cIsUtility>{SoulPoweredMantleInvisPercent * 10}%</style> of its cooldown. " +
        $"<color=#7CFDEA>While invisible, gain {SoulPoweredMantleLuckInit} <style=cStack>(+{SoulPoweredMantleLuckStack} per stack)</style> luck</color>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        On.RoR2.GenericSkill.OnExecute += SoulPoweredMantleInvisibility;
    }

    private void SoulPoweredMantleInvisibility(On.RoR2.GenericSkill.orig_OnExecute orig, GenericSkill self)
    {
        orig(self);
        if (self.stock < 1 && self.cooldownRemaining >= SoulPoweredMantleActivationLowerThreshold)
        {
            if (self.characterBody)
            {
                var stackCount = GetCount(self.characterBody);
                if (stackCount > 0)
                {
                    var duration = self.cooldownRemaining * SoulPoweredMantleInvisPercent < SoulPoweredMantleMaxInvis ? self.cooldownRemaining * SoulPoweredMantleInvisPercent : SoulPoweredMantleMaxInvis;
                    self.characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.Cloak.buffIndex, duration);
                    self.characterBody.AddTimedBuffAuthority(RoR2Content.Buffs.CloakSpeed.buffIndex, duration);
                }
            }
        }
    }

    [Server]
    private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (self.master)
        {
            self.AddItemBehavior<SoulPoweredMantleAddLuckBehavior>(self.inventory.GetItemCount(instance.ItemDef));
        }
        orig(self);
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
        SoulPoweredMantleLuckInit = config.ActiveBind("Item: " + ItemName, "Luck with one " + ItemName, 2f, "How much should luck be increased by with one " + ItemName + "? Luck modifiers in the base game increase/decrease by 1.");
        SoulPoweredMantleLuckStack = config.ActiveBind("Item: " + ItemName, "Luck per stack after one " + ItemName, 2f, "How much should luck be increased by per stack of " + ItemName + " after one? Luck modifiers in the base game increase/decrease by 1.");
        SoulPoweredMantleMaxInvis = config.ActiveBind("Item: " + ItemName, "Maximum invisibility duration granted by " + ItemName, 7f, "The invisibility granted by " + ItemName + " is no longer than this many seconds.");
        SoulPoweredMantleInvisPercent = config.ActiveBind("Item: " + ItemName, "Cooldown remaining to invisibility duration", 0.5f, "The cooldown remaining upon using the last stock on a skill will be multiplied by this, and that many seconds of invisibility will be granted.");
        SoulPoweredMantleActivationLowerThreshold = config.ActiveBind("Item: " + ItemName, "Minimum cooldown required for " + ItemName + " to activate", 4f, "Skills wilth cooldown below this many seconds will not activate " + ItemName + ".");
    }

    public class SoulPoweredMantleAddLuckBehavior : CharacterBody.ItemBehavior
    {
        private float cachedLuck;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                On.RoR2.CharacterMaster.OnInventoryChanged += UpdateCachedLuck;
                cachedLuck = body.master.luck;
            }
        }

        private void UpdateCachedLuck(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            if (body)
            {
                if (self.Equals(body.master))
                {
                    cachedLuck = self.luck;
                }
            }
        }

        private void FixedUpdate()
        {
            if (body)
            {
                var modelLocator = body.gameObject.GetComponent<ModelLocator>();
                if (modelLocator)
                {
                    var modelTransform = modelLocator.modelTransform;
                    if (modelTransform)
                    {
                        var model = modelTransform.gameObject.GetComponent<CharacterModel>();
                        if (model)
                        {
                            if (model.visibility == VisibilityLevel.Cloaked || model.visibility == VisibilityLevel.Revealed)
                            {
                                body.master.luck = cachedLuck + SoulPoweredMantleLuckInit + (stack - 1) * SoulPoweredMantleLuckStack;
                            }
                            else
                            {
                                body.master.luck = cachedLuck;
                            }
                        }
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                body.master.luck = cachedLuck;
                On.RoR2.CharacterMaster.OnInventoryChanged -= UpdateCachedLuck;
            }
        }
    }
}
