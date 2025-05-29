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

namespace SecondMoon.Items.Prototype.SoulPoweredMantle;

public class SoulPoweredMantle : Item<SoulPoweredMantle>
{
    public static ConfigOption<float> SoulPoweredMantleLuckInit;
    public static ConfigOption<float> SoulPoweredMantleLuckStack;
    public static ConfigOption<float> SoulPoweredMantleMaxInvis;
    public static ConfigOption<float> SoulPoweredMantleInvisPercent;
    public static ConfigOption<float> SoulPoweredMantleActivationLowerThreshold;

    public override string ItemName => "Soul-Powered Mantle";

    public override string ItemLangTokenName => "SOUL_POWERED_MANTLE";

    public override string ItemPickupDesc => "Using a skill makes you invisible, scaling with cooldown. You are very lucky while invisible.";

    public override string ItemFullDesc => $"Using a <style=cIsUtility>skill</style> grants you <style=cIsUtility>invisibility</style> and <style=cIsUtility>40% movement speed</style> for <style=cIsUtility>{SoulPoweredMantleInvisPercent * 10}%</style> of its cooldown. " +
        $"<color=#7CFDEA>While invisible, gain {SoulPoweredMantleLuckInit} <style=cStack>(+{SoulPoweredMantleLuckStack} per stack)</style> luck</color>.";

    public override string ItemLore => "It brings me joy to see a contraption you have made on your own. Please allow me to inspect it.\r\n\r\n" +
        "This is..!\r\n\r\n" +
        "Brother, you have taken a big risk. There is a reason why our experiments excluded Blood and Soul - these components are finicky. They are difficult to make work in abundance.\r\n\r\n" +
        "Thankfully, it seems stable and frankly, of a level of power we can easily subdue. I see you have used Blood and Mass sparingly, this was a correct call.\r\n\r\n" +
        "While it will have little to work with, it also means the Soul can operate unfettered by the obstructions of the more material components.\n\nIt seems you recognized the Soul's self-preservation instincts. The hand I'm holding this with is turning invisible. This contraption serves to conceal its owner, doesn't it?\r\n\r\n" +
        "That is truly handy. This kind of ability is beyond even our strength.\r\n\r\n" +
        "It looks like a large piece of fabric, intended to be worn. This will maximize surface area so it can more easily conceal. I'm impressed with your attention to detail here, brother.\r\n\r\n" +
        "However, do remember to not take a gamble like this again in the future. Blood and Soul, especially the latter, carry risks. You cannot weave constructs with these in abundance. It is for our own good that you abide by this rule.";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += OnInventoryChanged;
        On.RoR2.GenericSkill.OnExecute += SoulPoweredMantleInvisibility;
    }

    private void SoulPoweredMantleInvisibility(On.RoR2.GenericSkill.orig_OnExecute orig, GenericSkill self)
    {
        orig(self);
        if (self.cooldownRemaining >= SoulPoweredMantleActivationLowerThreshold)
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

    private void OnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (NetworkServer.active)
        {
            if (self.master)
            {
                self.AddItemBehavior<SoulPoweredMantleAddLuckBehavior>(self.inventory.GetItemCount(instance.ItemDef));
                var behavior = self.gameObject.GetComponent<SoulPoweredMantleAddLuckBehavior>();
                if (behavior)
                {
                    behavior.cachedLuck = self.master.luck;
                }
            }
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
        SoulPoweredMantleActivationLowerThreshold = config.ActiveBind("Item: " + ItemName, "Minimum cooldown required for " + ItemName + " to activate", 4f, "Skills with cooldown below this many seconds will not activate " + ItemName + ".");
    }

    public class SoulPoweredMantleAddLuckBehavior : CharacterBody.ItemBehavior
    {
        public float cachedLuck;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                cachedLuck = body.master.luck;
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
            }
        }
    }
}
