using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RoR2.CharacterBody;

namespace SecondMoon.Items.Lunar.IrradiatedMoonstone;

internal class IrradiatedMoonstone : Item<IrradiatedMoonstone>
{
    public static float IrradiatedMoonstoneCooldownReductionInit = 0.15f;
    public static float IrradiatedMoonstoneCooldownReductionStack = 0.15f;

    public static float IrradiatedMoonstoneMovementInit = 0.15f;
    public static float IrradiatedMoonstoneMovementStack = 0.15f;

    public static float IrradiatedMoonstoneSprintInit = 0.15f;
    public static float IrradiatedMoonstoneSprintStack = 0.15f;

    public static float IrradiatedMoonstoneAttackSpeedInit = 0.15f;
    public static float IrradiatedMoonstoneAttackSpeedStack = 0.15f;

    public static float IrradiatedMoonstoneRegenInit = 0.15f;
    public static float IrradiatedMoonstoneRegenStack = 0.15f;

    public static float IrradiatedMoonstoneSelfDebuffDurationReductionInit = 0.15f;
    public static float IrradiatedMoonstoneSelfDebuffDurationReductionStack = 0.15f;

    public static float IrradiatedMoonstoneProjectileSpeedInit = 0.15f;
    public static float IrradiatedMoonstoneProjectileSpeedStack = 0.15f;

    public static float IrradiatedMoonstoneTeleporterChargeRateIncreaseInit = 0.15f;
    public static float IrradiatedMoonstoneTeleporterChargeRateIncreaseStack = 0.15f;
    public static int IrradiatedMoonstoneTeleporterChargeRateIncreaseCap = 8;

    public static int IrradiatedMoonstonePlayersStackTracker = 0;
    public static int IrradiatedMoonstoneMonsterStackTracker = 0;

    public override string ItemName => "Irradiated Moonstone";

    public override string ItemLangTokenName => "SECONDMOON_SHINYPEARL_LUNAR";

    public override string ItemPickupDesc => $"You are faster (in almost all aspects)... <color=#FF7F7F>BUT your enemies also benefit from this item.</color>\n";

    public override string ItemFullDesc => $"Gain the following boosts:\r\n\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneCooldownReductionInit * 100}% <style=cStack>(+{IrradiatedMoonstoneCooldownReductionStack * 100}% per stack)</style> cooldown reduction</style> <color=#FF7F7F>(enemies also benefit)</color>.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneMovementInit * 100}% <style=cStack>(+{IrradiatedMoonstoneMovementStack * 100}% per stack)</style> movement speed</style>.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneSprintInit * 100}% <style=cStack>(+{IrradiatedMoonstoneSprintStack * 100}% per stack)</style> sprint speed</style>.\r\n" +
        $"• Gain <style=cIsDamage>{IrradiatedMoonstoneAttackSpeedInit * 100}% <style=cStack>(+{IrradiatedMoonstoneAttackSpeedStack * 100}% per stack)</style> attack speed</style> <color=#FF7F7F>(enemies also benefit)</color>.\r\n" +
        $"• Gain <style=cIsHealing>{IrradiatedMoonstoneRegenInit * 100}% <style=cStack>(+{IrradiatedMoonstoneRegenStack * 100}% per stack)</style> health regeneration</style>.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneSelfDebuffDurationReductionInit * 100}% <style=cStack>(+{IrradiatedMoonstoneSelfDebuffDurationReductionStack * 100}% per stack)</style> debuff duration reduction on self</style>." +
        $" <style=cIsDamage>Collapse</style> instead does <style=cIsDamage>{IrradiatedMoonstoneSelfDebuffDurationReductionInit * 100}%</style> <style=cStack>(+{IrradiatedMoonstoneSelfDebuffDurationReductionStack * 100}% per stack)</style> less damage to you.\r\n" +
        $"• Gain <style=cIsDamage>{IrradiatedMoonstoneProjectileSpeedInit * 100}% <style=cStack>(+{IrradiatedMoonstoneProjectileSpeedStack * 100}% per stack)</style> projectile speed</style> for projectiles without targets.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneTeleporterChargeRateIncreaseInit * 100}% <style=cStack>(+{IrradiatedMoonstoneTeleporterChargeRateIncreaseStack * 100}% per stack)</style> teleporter charge rate</style>.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Lunar;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.Healing, ItemTag.WorldUnique, ItemTag.HoldoutZoneRelated];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        GetStatCoefficients += IrradiatedMoonstoneBoostStats;
        On.RoR2.CharacterBody.OnInventoryChanged += IrradiatedMoonstoneSetupTeamStackCounts;
        On.RoR2.HoldoutZoneController.Start += IrradiatedMoonstoneBoostTPRate;
        On.RoR2.Projectile.ProjectileController.Start += IrradiatedMoonstoneFasterProjectiles;
        On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += IrradiatedMoonstoneShorterDebuffs;
        On.RoR2.DotController.AddDot += IrradiatedMoonstoneShorterDots;
    }

    private void IrradiatedMoonstoneShorterDots(On.RoR2.DotController.orig_AddDot orig, DotController self, GameObject attackerObject, float duration, DotController.DotIndex dotIndex, float damageMultiplier, uint? maxStacksFromAttacker, float? totalDamage, DotController.DotIndex? preUpgradeDotIndex)
    {
        float newDuration = duration;
        float? newTotalDamage = totalDamage;
        float newDamageMultiplier = damageMultiplier;

        if (self.victimBody)
        {
            var victim = self.victimBody;
            if (victim)
            {
                var stackCount = GetCount(victim);
                if (stackCount > 0)
                {
                    float decrease = (float)(1 - ((1 - IrradiatedMoonstoneSelfDebuffDurationReductionInit) * Math.Pow(1 - IrradiatedMoonstoneSelfDebuffDurationReductionStack, stackCount - 1)));
                    if (!(dotIndex == DotController.DotIndex.Fracture))
                    {
                        if (totalDamage.HasValue)
                        {
                            newTotalDamage = totalDamage * (1 - decrease);
                        }
                        else
                        {
                            newDuration = duration * (1 - decrease);
                        }
                    }
                    else
                    {
                        newDamageMultiplier = damageMultiplier * (1 - decrease);
                    }
                }
            }
        }
        orig(self, attackerObject, newDuration, dotIndex, newDamageMultiplier, maxStacksFromAttacker, newTotalDamage, preUpgradeDotIndex);
    }

    private void IrradiatedMoonstoneShorterDebuffs(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        float newDuration = duration;
        if (buffDef.isDebuff)
        {
            var stackCount = GetCount(self);
            if (stackCount > 0)
            {
                float decrease = (float)(1 - ((1 - IrradiatedMoonstoneSelfDebuffDurationReductionInit) * Math.Pow(1 - IrradiatedMoonstoneSelfDebuffDurationReductionStack, stackCount - 1)));
                newDuration = duration * (1 - decrease);
            }
        }
        orig(self, buffDef, newDuration);
    }

    private void IrradiatedMoonstoneFasterProjectiles(On.RoR2.Projectile.ProjectileController.orig_Start orig, RoR2.Projectile.ProjectileController self)
    {
        orig(self);
        var ownerBody = self.owner.GetComponent<CharacterBody>();
        if (ownerBody)
        {
            var stackCount = GetCount(ownerBody);
            if (stackCount > 0)
            {
                var projectileSimple = self.gameObject.GetComponent<ProjectileSimple>();
                if (projectileSimple)
                {
                    projectileSimple.desiredForwardSpeed *= 1 + (IrradiatedMoonstoneProjectileSpeedInit + ((stackCount - 1) * IrradiatedMoonstoneProjectileSpeedStack));
                }
            }
        }
    }

    private void IrradiatedMoonstoneBoostTPRate(On.RoR2.HoldoutZoneController.orig_Start orig, HoldoutZoneController self)
    {
        orig(self);
        var component = self.GetComponent<IrradiatedMoonstoneTPBoosterController>();
        if (!component && IrradiatedMoonstonePlayersStackTracker > 0)
        {
            self.gameObject.AddComponent<IrradiatedMoonstoneTPBoosterController>();
        }
    }

    private void IrradiatedMoonstoneSetupTeamStackCounts(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        int num = 0;
        ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
        int i = 0;
        for (int count = readOnlyInstancesList.Count; i < count; i++)
        {
            CharacterMaster characterMaster = readOnlyInstancesList[i];
            if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.hasBody && characterMaster.playerCharacterMasterController)
            {
                num += characterMaster.inventory.GetItemCount(ItemDef);
            }
        }
        IrradiatedMoonstonePlayersStackTracker = num;
        IrradiatedMoonstoneMonsterStackTracker = Util.GetItemCountForTeam(TeamIndex.Monster, ItemDef.itemIndex, requiresAlive: true, requiresConnected: false);
        orig(self);
    }

    private void IrradiatedMoonstoneBoostStats(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            float decrease = (float)(1 - ((1 - IrradiatedMoonstoneCooldownReductionInit) * Math.Pow(1 - IrradiatedMoonstoneCooldownReductionStack, stackCount - 1)));
            args.cooldownMultAdd -= decrease;
            args.moveSpeedMultAdd += IrradiatedMoonstoneMovementInit + (stackCount - 1) * IrradiatedMoonstoneMovementStack;
            args.sprintSpeedAdd += IrradiatedMoonstoneSprintInit + (stackCount - 1) * IrradiatedMoonstoneSprintStack;
            args.attackSpeedMultAdd += IrradiatedMoonstoneAttackSpeedInit + (stackCount - 1) * IrradiatedMoonstoneAttackSpeedStack;
            args.baseRegenAdd += IrradiatedMoonstoneRegenInit + (stackCount - 1) * IrradiatedMoonstoneRegenStack;
            args.levelRegenAdd += (IrradiatedMoonstoneRegenInit + (stackCount - 1) * IrradiatedMoonstoneRegenStack) / 5;
        }
        if (sender.teamComponent)
        {
            float decrease;
            switch (sender.teamComponent.teamIndex) 
            { 
                case TeamIndex.Player:
                    args.attackSpeedMultAdd += IrradiatedMoonstoneMonsterStackTracker > 0 ? IrradiatedMoonstoneAttackSpeedInit + (IrradiatedMoonstoneMonsterStackTracker - 1) * IrradiatedMoonstoneAttackSpeedStack : 0;
                    decrease = (float)(1 - ((1 - IrradiatedMoonstoneCooldownReductionInit) * Math.Pow(1 - IrradiatedMoonstoneCooldownReductionStack, IrradiatedMoonstoneMonsterStackTracker - 1)));
                    args.cooldownMultAdd -= IrradiatedMoonstoneMonsterStackTracker > 0 ? decrease : 0;
                    break;
                case TeamIndex.Monster:
                    args.attackSpeedMultAdd += IrradiatedMoonstonePlayersStackTracker > 0 ? IrradiatedMoonstoneAttackSpeedInit + (IrradiatedMoonstonePlayersStackTracker - 1) * IrradiatedMoonstoneAttackSpeedStack : 0;
                    decrease = (float)(1 - ((1 - IrradiatedMoonstoneCooldownReductionInit) * Math.Pow(1 - IrradiatedMoonstoneCooldownReductionStack, IrradiatedMoonstonePlayersStackTracker - 1)));
                    args.cooldownMultAdd -= IrradiatedMoonstonePlayersStackTracker > 0 ? decrease : 0;
                    break;
            }
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }

    public class IrradiatedMoonstoneTPBoosterController : MonoBehaviour
    {
        private HoldoutZoneController holdoutZoneController;
        private int stackCount;
        private float stopwatch;

        private void Awake()
        {
            holdoutZoneController = GetComponent<HoldoutZoneController>();
            stackCount = Mathf.Min(IrradiatedMoonstonePlayersStackTracker, IrradiatedMoonstoneTeleporterChargeRateIncreaseCap);
        }

        private void OnEnable()
        {
            holdoutZoneController.calcChargeRate += FasterTP;
        }

        private void OnDisable()
        {
            holdoutZoneController.calcChargeRate -= FasterTP;
        }

        private void FasterTP(ref float rate)
        {
            if (stackCount > 0)
            {
                rate *= 1 + (IrradiatedMoonstoneTeleporterChargeRateIncreaseInit + (stackCount - 1) * IrradiatedMoonstoneTeleporterChargeRateIncreaseStack);
            }
        }
    }
}
