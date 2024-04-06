using EntityStates.BrotherMonster;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
namespace SecondMoon.Items.Prototype.CelestineAuger;
public class CelestineAuger : Item<CelestineAuger>
{
    public static float CelestineAugerCriticalChanceInit = 0.5f;
    public static float CelestineAugerCriticalChanceStack = 0.5f;
    public static float CelestineAugerCriticalDamageInit = 0.5f;
    public static float CelestineAugerCriticalDamageStack = 0.5f;
    public static float CelestineAugerInitialCritChance = 5f;

    public override string ItemName => "Celestine Auger";

    public override string ItemLangTokenName => "SECONDMOONMOD_CELESTINE_AUGER";

    public override string ItemPickupDesc => $"Increase critical stats. Your attacks pierce through terrain.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{CelestineAugerInitialCritChance}% critical chance</style>. " +
        $"Increases <style=cIsDamage>critical chance</style> by <style=cIsDamage>{CelestineAugerCriticalChanceInit * 100}%</style> <style=cStack>(+{CelestineAugerCriticalChanceStack * 100}% per stack)</style>. " +
        $"Increases <style=cIsDamage>critical damage</style> by <style=cIsDamage>{CelestineAugerCriticalDamageInit * 100}%</style> <style=cStack>(+{CelestineAugerCriticalDamageStack * 100}% per stack)</style>. " +
        $"<color=#7CFDEA>Your hitscan attacks, single target projectiles and targeted projectiles ignore terrain collision</color>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

    public override ItemTag[] Category => [ItemTag.Damage];

    public static List<int> CelestineAugerBlacklistedProjectiles = new List<int>();

    public static string[] CelestineAugerBlacklistedProjectileNames = ["SpiderMine", "LunarShardProjectile"];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        GetStatCoefficients += CelestineAugerInitBuffCrit;
        On.RoR2.CharacterBody.RecalculateStats += CelestineAugerBuffCCCD;
        IL.RoR2.BulletAttack.FireSingle += CelestineAugerIgnoreCollisionBullet;
        RoR2Application.onLoad += CelestineAugerSetBlacklistedProjectiles;
        On.RoR2.Projectile.ProjectileController.Start += SetCollidersToTrigger;
        On.RoR2.Projectile.ProjectileController.OnTriggerEnter += CelestineAugerIgnoreCollisionProjectile;
    }

    private void CelestineAugerSetBlacklistedProjectiles()
    {
        foreach (var projectilePrefab in ProjectileCatalog.projectilePrefabs)
        {
            if (CelestineAugerBlacklistedProjectileNames.Contains(projectilePrefab.name))
            {
                if (projectilePrefab.GetComponent<RoR2.Projectile.ProjectileController>())
                {
                    CelestineAugerBlacklistedProjectiles.Add(projectilePrefab.GetComponent<RoR2.Projectile.ProjectileController>().catalogIndex);
                }
            }
        }
    }

    private void CelestineAugerIgnoreCollisionProjectile(On.RoR2.Projectile.ProjectileController.orig_OnTriggerEnter orig, RoR2.Projectile.ProjectileController self, Collider collider)
    {
        if (self.gameObject.GetComponent<RoR2.Projectile.ProjectileTargetComponent>())
        {
            if (self.owner)
            {
                if (self.gameObject.GetComponent<RoR2.Projectile.ProjectileTargetComponent>().target)
                {
                    var ownerBody = self.owner.GetComponent<CharacterBody>();
                    if (ownerBody)
                    {
                        var stackCount = GetCount(ownerBody);
                        if (stackCount > 0)
                        {
                            if (!collider.GetComponent<HurtBox>())
                            {

                                return;
                            }
                            else
                            {
                                collider.isTrigger = false;
                            }
                        }
                    }
                }
                else
                {
                    collider.isTrigger = false;
                }
            }
        }
        else if (self.gameObject.GetComponent<RoR2.Projectile.ProjectileSingleTargetImpact>())
        {
            if (self.owner)
            {
                var ownerBody = self.owner.GetComponent<CharacterBody>();
                if (ownerBody)
                {
                    var stackCount = GetCount(ownerBody);
                    if (stackCount > 0)
                    {
                        if (!collider.GetComponent<HurtBox>())
                        {

                            return;
                        }
                        else
                        {
                            collider.isTrigger = false;
                        }
                    }
                }
            }
        }
        orig(self, collider);
    }
    
    private void SetCollidersToTrigger(On.RoR2.Projectile.ProjectileController.orig_Start orig, RoR2.Projectile.ProjectileController self)
    {
        orig(self);
        if (!CelestineAugerBlacklistedProjectiles.Contains(self.catalogIndex))
        {
            if (self.gameObject.GetComponent<RoR2.Projectile.ProjectileTargetComponent>() || self.gameObject.GetComponent<RoR2.Projectile.ProjectileSingleTargetImpact>())
            {
                if (self.owner)
                {
                    var ownerBody = self.owner.GetComponent<CharacterBody>();
                    if (ownerBody)
                    {
                        var stackCount = GetCount(ownerBody);
                        if (stackCount > 0)
                        {
                            for (int i = 0; i < self.myColliders.Length; i++)
                            {
                                self.myColliders[i].isTrigger = true;
                            }
                            self.canImpactOnTrigger = true;
                        }
                    }
                }
            }
        }
    }

    private void CelestineAugerIgnoreCollisionBullet(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(
            x => x.MatchLdnull(),
            x => x.MatchStloc(4));
        cursor.Index += 1;
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<BulletAttack>>((attack) =>
        {
            if (attack.owner)
            {
                var attacker = attack.owner.GetComponent<CharacterBody>();
                if (attacker)
                {
                    var stackCount = GetCount(attacker);
                    if (stackCount > 0)
                    {
                        attack.hitMask = (int)LayerIndex.entityPrecise.mask;
                    }
                }
            }
        });
    }
    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }

    public void CelestineAugerInitBuffCrit(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += CelestineAugerInitialCritChance;
        }
    }

    public void CelestineAugerBuffCCCD(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.crit *= 1 + (CelestineAugerCriticalChanceInit + (stackCount - 1) * CelestineAugerCriticalChanceStack);
            self.critMultiplier *= 1 + (CelestineAugerCriticalDamageInit + (stackCount - 1) * CelestineAugerCriticalDamageStack);
        }
    }
}