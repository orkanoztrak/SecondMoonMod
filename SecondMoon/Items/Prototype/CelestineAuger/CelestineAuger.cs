using BepInEx.Configuration;
using EntityStates.FalseSonBoss;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
namespace SecondMoon.Items.Prototype.CelestineAuger;
public class CelestineAuger : Item<CelestineAuger>
{
    public static ConfigOption<float> CelestineAugerCriticalChanceInit;
    public static ConfigOption<float> CelestineAugerCriticalChanceStack;
    public static ConfigOption<float> CelestineAugerCriticalDamageInit;
    public static ConfigOption<float> CelestineAugerCriticalDamageStack;
    public static ConfigOption<float> CelestineAugerInitialCritChance;

    public override string ItemName => "Celestine Auger";

    public override string ItemLangTokenName => "CELESTINE_AUGER";

    public override string ItemPickupDesc => $"Increase critical stats. Your attacks pierce through terrain.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{CelestineAugerInitialCritChance}% critical chance</style>. " +
        $"Increases <style=cIsDamage>critical chance</style> by <style=cIsDamage>{CelestineAugerCriticalChanceInit * 100}%</style> <style=cStack>(+{CelestineAugerCriticalChanceStack * 100}% per stack)</style>. " +
        $"Increases <style=cIsDamage>critical damage</style> by <style=cIsDamage>{CelestineAugerCriticalDamageInit * 100}%</style> <style=cStack>(+{CelestineAugerCriticalDamageStack * 100}% per stack)</style>. " +
        $"<color=#7CFDEA>Your hitscan attacks, single target projectiles and targeted projectiles ignore terrain collision</color>.";

    public override string ItemLore => "We take walls for granted. They are an everyday occurrence, in all shapes, sizes and meanings. Walls of flesh to keep keep life in, and death out. Walls of mind to determine who you are, to keep those who would pry and shape you out. Walls of rock and iron to break wills, shatter weapons and keep your family safe. Walls keep what you want in, in, and what you want out, out.\r\n\r\n" +
        "That is the crucial part though. No wall is ever complete without a gate - an unpassable wall is meaningless, as you are bound to want to retrieve what lies within, or to put something in to preserve in the first place. A gateless wall is an obstacle; nothing more, nothing less.\r\n\r\n" +
        "It then also follows, that gates without walls are equally meaningless. After all, why bother with them when you can simply intrude wherever you wish? This appears pointless and absurd to even think about.\r\n\r\n" +
        "But friends, one day, you may be faced with situations where your walls are against a force that they are simply hopeless against. That is when we are weakest - as if we had no walls to guard us at all.\r\n\r\n" +
        "- Unknown";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage];

    public static List<int> CelestineAugerBlacklistedProjectiles = new List<int>();

    public static string[] CelestineAugerBlacklistedProjectileNames = ["SpiderMine", "LunarShardProjectile"];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += CelestineAugerInitBuffCrit;
        IL.RoR2.CharacterBody.RecalculateStats += CelestineAugerBuffCCCD;
        On.RoR2.BulletAttack.Fire += CelestineAugerIgnoreCollisionBullet;
        RoR2Application.onLoad += CelestineAugerSetBlacklistedProjectiles;
        On.RoR2.Projectile.ProjectileController.Start += AddCelestineComponent;
        CharacterSpecificFixes();
    }

    private void CharacterSpecificFixes()
    {
        On.EntityStates.FalseSon.LaserFatherCharged.FireBullet += FixAim;
        IL.EntityStates.FalseSon.LaserFatherBurst.FireBurstLaser += FixAim;
    }

    private void FixAim(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchLdsfld(typeof(LayerIndex.CommonMasks), nameof(LayerIndex.CommonMasks.laser))))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<LayerMask, EntityStates.FalseSon.LaserFatherBurst, LayerMask>>((mask, state) =>
            {
                return GetCount(state.characterBody) > 0 ? LayerIndex.entityPrecise.mask : mask;
            });
        }
    }

    private void FixAim(On.EntityStates.FalseSon.LaserFatherCharged.orig_FireBullet orig, EntityStates.FalseSon.LaserFatherCharged self, Transform modelTransform, Ray aimRay, string targetMuzzle, float maxDistance)
    {
        var body = self.characterBody;
        if (body)
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                aimRay = self.aimRay;
                maxDistance = EntityStates.FalseSon.LaserFatherCharged.maxDistance;
            }
        }
        orig(self, modelTransform, aimRay, targetMuzzle, maxDistance);
    }

    private void AddCelestineComponent(On.RoR2.Projectile.ProjectileController.orig_Start orig, ProjectileController self)
    {
        orig(self);
        if (!CelestineAugerBlacklistedProjectiles.Contains(self.catalogIndex))
        {
            if (self.gameObject.GetComponent<ProjectileTargetComponent>() || self.gameObject.GetComponent<ProjectileSingleTargetImpact>())
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
                            var component = self.gameObject.AddComponent<CelestineComponent>();
                            component.controller = self;
                        }
                    }
                }
            }
        }
    }

    public void CelestineAugerInitBuffCrit(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += CelestineAugerInitialCritChance;
        }
    }

    private void CelestineAugerBuffCCCD(ILContext il)
    {
        var critToCritDmgIndex = 47;
        var critIndex = 98;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdloc(critToCritDmgIndex)))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, critIndex);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, CharacterBody, float>>((crit, body) =>
            {
                if (body)
                {
                    var stackCount = GetCount(body);
                    if (stackCount > 0)
                    {
                        body.critMultiplier *= 1 + (CelestineAugerCriticalDamageInit + (stackCount - 1) * CelestineAugerCriticalDamageStack);
                        crit *= 1 + (CelestineAugerCriticalChanceInit + (stackCount - 1) * CelestineAugerCriticalChanceStack);
                    }
                }
                return crit;
            });
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, critIndex);
        }
    }

    private void CelestineAugerIgnoreCollisionBullet(On.RoR2.BulletAttack.orig_Fire orig, BulletAttack self)
    {
        var attacker = self.owner.GetComponent<CharacterBody>();
        if (attacker)
        {
            var stackCount = GetCount(attacker);
            if (stackCount > 0)
            {
                self.hitMask = (int)LayerIndex.entityPrecise.mask;
            }
        }
        orig(self);
    }

    private void CelestineAugerSetBlacklistedProjectiles()
    {
        foreach (var projectilePrefab in ProjectileCatalog.projectilePrefabs)
        {
            if (CelestineAugerBlacklistedProjectileNames.Contains(projectilePrefab.name))
            {
                if (projectilePrefab.GetComponent<ProjectileController>())
                {
                    CelestineAugerBlacklistedProjectiles.Add(projectilePrefab.GetComponent<ProjectileController>().catalogIndex);
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
        CelestineAugerCriticalChanceInit = config.ActiveBind("Item: " + ItemName, "Multiplicative critical chance with one " + ItemName, 0.5f, "How much should critical chance be increased by multiplicatively with one " + ItemName + "? (0.5 = 50%, meaning 1.50x critical chance.)");
        CelestineAugerCriticalChanceStack = config.ActiveBind("Item: " + ItemName, "Multiplicative critical chance per stack after one " + ItemName, 0.5f, "How much should critical chance be increased by multiplicatively per stack of " + ItemName + " after one? (0.5 = 50%, meaning 1.50x critical chance.)");
        CelestineAugerCriticalDamageInit = config.ActiveBind("Item: " + ItemName, "Multiplicative critical damage with one " + ItemName, 0.5f, "How much should critical damage be increased by multiplicatively with one " + ItemName + "? (0.5 = 50%, meaning 1.50x critical damage.)");
        CelestineAugerCriticalDamageStack = config.ActiveBind("Item: " + ItemName, "Multiplicative critical damage per stack after one " + ItemName, 0.5f, "How much should critical damage be increased by multiplicatively per stack of " + ItemName + " after one? (0.5 = 50%, meaning 1.50x critical damage.)");
        CelestineAugerInitialCritChance = config.ActiveBind("Item: " + ItemName, "Critical chance with at least one " + ItemName, 5f, "By what % should critical chance be increased by with at least one " + ItemName + "?");
    }

    public class CelestineComponent : MonoBehaviour
    {
        public ProjectileController controller;

        private void Awake()
        {
            enabled = false;
        }

        private void OnTriggerEnter (Collider collider)
        {
            if (controller)
            {
                if (NetworkServer.active)
                {
                    if (controller.gameObject.GetComponent<ProjectileTargetComponent>())
                    {
                        if (controller.owner)
                        {
                            if (controller.gameObject.GetComponent<ProjectileTargetComponent>().target)
                            {
                                var ownerBody = controller.owner.GetComponent<CharacterBody>();
                                if (ownerBody)
                                {
                                    var stackCount = instance.GetCount(ownerBody);
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
                    else if (controller.gameObject.GetComponent<ProjectileSingleTargetImpact>())
                    {
                        if (controller.owner)
                        {
                            var ownerBody = controller.owner.GetComponent<CharacterBody>();
                            if (ownerBody)
                            {
                                var stackCount = instance.GetCount(ownerBody);
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
                    if (controller.canImpactOnTrigger)
                    {
                        Vector3 vector = Vector3.zero;
                        if (controller.rigidbody)
                        {
                            vector = controller.rigidbody.velocity;
                        }
                        ProjectileImpactInfo projectileImpactInfo = default(ProjectileImpactInfo);
                        projectileImpactInfo.collider = collider;
                        projectileImpactInfo.estimatedPointOfImpact = transform.position;
                        projectileImpactInfo.estimatedImpactNormal = -vector.normalized;
                        ProjectileImpactInfo impactInfo = projectileImpactInfo;
                        IProjectileImpactBehavior[] components = GetComponents<IProjectileImpactBehavior>();
                        for (int i = 0; i < components.Length; i++)
                        {
                            components[i].OnProjectileImpact(impactInfo);
                        }
                    }
                }
            }
        }
    }
}