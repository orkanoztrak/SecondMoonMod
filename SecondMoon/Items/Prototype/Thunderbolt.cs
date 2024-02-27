using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.Items.Prototype;

public class Thunderbolt : Item
{
    public static float ThunderboltAS = 0.25f;
    public static float ThunderboltASStack = 0.25f;
    public static float ThunderboltASToMS = 1f;
    public static float ThunderboltASToDmg = 1f;
    public static float ThunderboltASToCD = .625f;
    public static float ThunderboltASToProjectileSpeed = .5f;
    public RoR2.Projectile.SlowDownProjectiles faster;
    public override string ItemName => "Thunderbolt";

    public override string ItemLangTokenName => "GO_BRR";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RecalculateStats += AttackSpeedThunderboltAndTranslate;
        On.RoR2.Projectile.SlowDownProjectiles.FixedUpdate += ZoomyProjectilesThunderbolt;
    }

    private void ZoomyProjectilesThunderbolt(On.RoR2.Projectile.SlowDownProjectiles.orig_FixedUpdate orig, RoR2.Projectile.SlowDownProjectiles self)
    {

    }

    private void AttackSpeedThunderboltAndTranslate(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.attackSpeed *= 1 + (ThunderboltAS + ((stackCount - 1) * ThunderboltASStack));
            var unchanged = self.baseAttackSpeed + (self.levelAttackSpeed * (self.level - 1));
            float increase = (self.attackSpeed / unchanged) - 1;
            self.moveSpeed *=  1 + (increase * ThunderboltASToMS);
            self.damage *= 1 + (increase * ThunderboltASToDmg);
            float cdr = (1 - (1 / (1 + increase))) *  ThunderboltASToCD;
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
