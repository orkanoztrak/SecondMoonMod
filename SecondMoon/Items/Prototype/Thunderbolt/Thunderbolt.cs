using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Prototype.Thunderbolt;

public class Thunderbolt : Item<Thunderbolt>
{
    public static float ThunderboltAS = 0.25f;
    public static float ThunderboltASStack = 0.25f;
    public static float ThunderboltASToMS = 1f;
    public static float ThunderboltASToDmg = 1f;
    public static float ThunderboltASToCD = .625f;
    public static float ThunderboltASToProjectileSpeed = .5f;
    public RoR2.Projectile.SlowDownProjectiles faster;
    public override string ItemName => "Thunderbolt";

    public override string ItemLangTokenName => "THUNDERBOLT";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;
    public override ItemTag[] Category => [ItemTag.Damage];
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RecalculateStats += ThunderboltAttackSpeedAndTranslate;
        //On.RoR2.Projectile.ProjectileController.Start += ThunderboltZoomyProjectiles;
    }

    private void ThunderboltZoomyProjectiles(On.RoR2.Projectile.ProjectileController.orig_Start orig, RoR2.Projectile.ProjectileController self)
    {
        var attacker = self.owner.GetComponent<CharacterBody>();
        if (attacker)
        {
            var stackCount = GetCount(attacker);
            if (stackCount > 0)
            {
                var unchanged = attacker.baseAttackSpeed + attacker.levelAttackSpeed * (attacker.level - 1);
                float increase = attacker.attackSpeed / unchanged - 1;
                var force = self.transform.forward * (1 + increase);
                self.rigidbody.AddForce(force);
            }
        }
        orig(self);
    }

    private void ThunderboltAttackSpeedAndTranslate(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.attackSpeed *= 1 + (ThunderboltAS + (stackCount - 1) * ThunderboltASStack);
            var unchanged = self.baseAttackSpeed + self.levelAttackSpeed * (self.level - 1);
            float increase = self.attackSpeed / unchanged - 1;
            self.moveSpeed *= 1 + increase * ThunderboltASToMS;
            self.damage *= 1 + increase * ThunderboltASToDmg;
            float cdr = (1 - 1 / (1 + increase)) * ThunderboltASToCD;
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
