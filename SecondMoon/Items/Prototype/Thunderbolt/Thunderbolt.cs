using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.Thunderbolt;

public class Thunderbolt : Item<Thunderbolt>
{
    public static float ThunderboltASInit = 0.25f;
    public static float ThunderboltASStack = 0.25f;
    public static float ThunderboltASToMS = 0.75f;
    public static float ThunderboltASToCD = 1.125f;
    public static float ThunderboltASToProjectileSpeed = 1f;
    public override string ItemName => "Thunderbolt";

    public override string ItemLangTokenName => "SECONDMOONMOD_THUNDERBOLT";

    public override string ItemPickupDesc => "Increase attack speed. Your attack speed increases also translate to movement speed, cooldown reduction and projectile speed.";

    public override string ItemFullDesc => $"Increases <style=cIsDamage>attack speed</style> by <style=cIsDamage>{ThunderboltASInit * 100}%</style> <style=cStack>(+{ThunderboltASStack * 100}% per stack)</style>. " +
        $"<color=#7CFDEA>Your <style=cIsDamage>attack speed</style> increase percentage also translates into the following</color>:\r\n\r\n" +
        $"• Gain <style=cIsUtility>movement speed</style> equal to <color=#7CFDEA>{ThunderboltASToMS * 100}%</color> of it. \r\n" +
        $"• Gain <style=cIsUtility>cooldown reduction</style> equal to <color=#7CFDEA>{((1f - 1f / (1f + (0.25f * 0.5f))) * ThunderboltASToCD) * 400}%</color> of it. \r\n" +
        $"• Gain <style=cIsDamage>projectile speed</style> equal to <color=#7CFDEA>{ThunderboltASToProjectileSpeed * 100}%</color> of it for projectiles without targets.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;
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
            float cdr = (1 - 1 / (1 + (increase * 0.5f))) * ThunderboltASToCD;
            self.skillLocator.primary.cooldownScale *= 1 - cdr;
            self.skillLocator.secondaryBonusStockSkill.cooldownScale *= 1 - cdr;
            self.skillLocator.utilityBonusStockSkill.cooldownScale *= 1 - cdr;
            self.skillLocator.specialBonusStockSkill.cooldownScale *= 1 - cdr;
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
