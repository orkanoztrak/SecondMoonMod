using BepInEx.Configuration;
using Facepunch.Steamworks;
using R2API;
using RoR2;
using RoR2.Projectile;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.Thunderbolt;

public class Thunderbolt : Item<Thunderbolt>
{
    public static ConfigOption<float> ThunderboltASInit;
    public static ConfigOption<float> ThunderboltASStack;
    public static ConfigOption<float> ThunderboltASToMS;
    public static float ThunderboltASToCD = 1.125f;
    public static ConfigOption<float> ThunderboltCDMultiplier;
    public static ConfigOption<float> ThunderboltASToProjectileSpeed;

    public override string ItemName => "Thunderbolt";

    public override string ItemLangTokenName => "SECONDMOONMOD_THUNDERBOLT";

    public override string ItemPickupDesc => "Increase attack speed. Your attack speed increases also translate to movement speed, cooldown reduction and projectile speed.";

    public override string ItemFullDesc => $"Increases <style=cIsDamage>attack speed</style> by <style=cIsDamage>{ThunderboltASInit * 100}%</style> <style=cStack>(+{ThunderboltASStack * 100}% per stack)</style>. " +
        $"<color=#7CFDEA>Your attack speed increase percentage also translates into the following</color>:\r\n\r\n" +
        $"• Increase <style=cIsUtility>movement speed</style> by <color=#7CFDEA>{ThunderboltASToMS * 100}%</color> of it. \r\n" +
        $"• Increase <style=cIsUtility>cooldown reduction</style> by <color=#7CFDEA>{(1f - 1f / (1f + (0.25f * 0.5f))) * ThunderboltASToCD * 400 * ThunderboltCDMultiplier}%</color> of it. \r\n" +
        $"• Increase <style=cIsDamage>projectile speed</style> by <color=#7CFDEA>{ThunderboltASToProjectileSpeed * 100}%</color> of it for projectiles without targets.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;
    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RecalculateStats += ThunderboltAttackSpeedAndTranslate;
        On.RoR2.Projectile.ProjectileController.Start += ThunderboltFasterProjectiles;
    }

    private void ThunderboltFasterProjectiles(On.RoR2.Projectile.ProjectileController.orig_Start orig, RoR2.Projectile.ProjectileController self)
    {
        orig(self);
        if (self.owner)
        {
            var ownerBody = self.owner.GetComponent<CharacterBody>();
            if (ownerBody)
            {
                var stackCount = GetCount(ownerBody);
                if (stackCount > 0)
                {
                    var unchanged = ownerBody.baseAttackSpeed + ownerBody.levelAttackSpeed * (ownerBody.level - 1);
                    float increase = ownerBody.attackSpeed / unchanged - 1;
                    var projectileSimple = self.gameObject.GetComponent<ProjectileSimple>();
                    if (projectileSimple)
                    {
                        projectileSimple.desiredForwardSpeed *= 1 + increase;
                    }
                }
            }
        }
    }

    private void ThunderboltAttackSpeedAndTranslate(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.attackSpeed *= 1 + (ThunderboltASInit + (stackCount - 1) * ThunderboltASStack);
            var unchanged = self.baseAttackSpeed + self.levelAttackSpeed * (self.level - 1);
            float increase = self.attackSpeed / unchanged - 1;
            self.moveSpeed *= 1 + increase * ThunderboltASToMS;
            float cdr = (1 - 1 / (1 + (increase * 0.5f))) * ThunderboltASToCD * ThunderboltCDMultiplier;
            if (self.skillLocator)
            {
                if (self.skillLocator.primaryBonusStockSkill)
                {
                    self.skillLocator.primaryBonusStockSkill.cooldownScale *= 1 - cdr;
                }
                if (self.skillLocator.secondaryBonusStockSkill)
                {
                    self.skillLocator.secondaryBonusStockSkill.cooldownScale *= 1 - cdr;
                }
                if (self.skillLocator.utilityBonusStockSkill)
                {
                    self.skillLocator.utilityBonusStockSkill.cooldownScale *= 1 - cdr;
                }
                if (self.skillLocator.specialBonusStockSkill)
                {
                    self.skillLocator.specialBonusStockSkill.cooldownScale *= 1 - cdr;
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
        ThunderboltASInit = config.ActiveBind("Item: " + ItemName, "Multiplicative attack speed with one " + ItemName, 0.25f, "How much should attack speed be increased multiplicatively with one " + ItemName + "? (0.25 = 25%, meaning 1.25x attack speed.)");
        ThunderboltASStack = config.ActiveBind("Item: " + ItemName, "Multiplicative attack speed per stack after one " + ItemName, 0.25f, "How much should attack speed be increased multiplicatively per stack of " + ItemName + " after one? (0.25 = 25%, meaning 1.25x attack speed.)");
        ThunderboltASToMS = config.ActiveBind("Item: " + ItemName, "Movement speed translation rate", 0.75f, "What % of attack speed increase % should movement speed be increased by? (0.75 = 75%)");
        ThunderboltCDMultiplier = config.ActiveBind("Item: " + ItemName, "Cooldown reduction translation rate", 1f, "What % of attack speed increase % should cooldowns be reduced by? This has a hyperbolic calculation method, and by default cooldowns are reduced by 12.5% at +25% attack speed (for reference). Multiply cooldown reduction by this config value.");
        ThunderboltASToProjectileSpeed = config.ActiveBind("Item: " + ItemName, "Projectile speed translation rate", 1f, "What % of attack speed increase % should untargeted projectile speed be increased by? (1 = 100%)");
    }
}
