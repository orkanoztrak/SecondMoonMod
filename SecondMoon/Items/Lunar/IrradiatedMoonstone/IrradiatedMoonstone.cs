using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Projectile;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Lunar.IrradiatedMoonstone;

public class IrradiatedMoonstone : Item<IrradiatedMoonstone>
{
    public static ConfigOption<float> IrradiatedMoonstoneCooldownReductionInit;
    public static ConfigOption<float> IrradiatedMoonstoneCooldownReductionStack;

    public static ConfigOption<float> IrradiatedMoonstoneMovementInit;
    public static ConfigOption<float> IrradiatedMoonstoneMovementStack;

    public static ConfigOption<float> IrradiatedMoonstoneSprintInit;
    public static ConfigOption<float> IrradiatedMoonstoneSprintStack;

    public static ConfigOption<float> IrradiatedMoonstoneAttackSpeedInit;
    public static ConfigOption<float> IrradiatedMoonstoneAttackSpeedStack;

    public static ConfigOption<float> IrradiatedMoonstoneRegenInit;
    public static ConfigOption<float> IrradiatedMoonstoneRegenStack;

    public static ConfigOption<float> IrradiatedMoonstoneSelfDebuffDurationReductionInit;
    public static ConfigOption<float> IrradiatedMoonstoneSelfDebuffDurationReductionStack;

    public static ConfigOption<float> IrradiatedMoonstoneProjectileSpeedInit;
    public static ConfigOption<float> IrradiatedMoonstoneProjectileSpeedStack;

    public static ConfigOption<float> IrradiatedMoonstoneTeleporterChargeRateIncreaseInit;
    public static ConfigOption<float> IrradiatedMoonstoneTeleporterChargeRateIncreaseStack;
    public static ConfigOption<int> IrradiatedMoonstoneTeleporterChargeRateIncreaseCap;

    public static int IrradiatedMoonstonePlayersStackTracker = 0;
    public static int IrradiatedMoonstoneMonsterStackTracker = 0;


    public override string ItemName => "Irradiated Moonstone";

    public override string ItemLangTokenName => "SHINYPEARL_LUNAR";

    public override string ItemPickupDesc => $"Become speed itself. <color=#FF7F7F>Your enemies also benefit from this item.</color>\n";

    public override string ItemFullDesc => $"Gain the following boosts:\r\n\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneCooldownReductionInit * 100}% <style=cStack>(+{IrradiatedMoonstoneCooldownReductionStack * 100}% per stack)</style> cooldown reduction</style> <color=#FF7F7F>(enemies also benefit).</color>\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneMovementInit * 100}% <style=cStack>(+{IrradiatedMoonstoneMovementStack * 100}% per stack)</style> movement speed</style>.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneSprintInit * 100}% <style=cStack>(+{IrradiatedMoonstoneSprintStack * 100}% per stack)</style> sprint speed</style>.\r\n" +
        $"• Gain <style=cIsDamage>{IrradiatedMoonstoneAttackSpeedInit * 100}% <style=cStack>(+{IrradiatedMoonstoneAttackSpeedStack * 100}% per stack)</style> attack speed</style> <color=#FF7F7F>(enemies also benefit).</color>\r\n" +
        $"• Gain <style=cIsHealing>{IrradiatedMoonstoneRegenInit * 100}% <style=cStack>(+{IrradiatedMoonstoneRegenStack * 100}% per stack)</style> health regeneration</style>.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneSelfDebuffDurationReductionInit * 100}% <style=cStack>(+{IrradiatedMoonstoneSelfDebuffDurationReductionStack * 100}% per stack)</style> debuff duration reduction on self</style>." +
        $" <style=cIsDamage>Collapse</style> instead does <style=cIsDamage>{IrradiatedMoonstoneSelfDebuffDurationReductionInit * 100}%</style> <style=cStack>(+{IrradiatedMoonstoneSelfDebuffDurationReductionStack * 100}% per stack)</style> less damage to you.\r\n" +
        $"• Gain <style=cIsDamage>{IrradiatedMoonstoneProjectileSpeedInit * 100}% <style=cStack>(+{IrradiatedMoonstoneProjectileSpeedStack * 100}% per stack)</style> projectile speed</style> for projectiles without targets.\r\n" +
        $"• Gain <style=cIsUtility>{IrradiatedMoonstoneTeleporterChargeRateIncreaseInit * 100}% <style=cStack>(+{IrradiatedMoonstoneTeleporterChargeRateIncreaseStack * 100}% per stack)</style> teleporter charge rate</style>.";

    public override string ItemLore => "Ah... I have just the idea for this one piece.\r\n\r\n" +
        "You may not see it, but what I wield is Soul. Something that's in all of us. Something I know HE loves.\r\n\r\n" +
        "Now, I will shape it with my emotion.\r\n\r\n" +
        "I will shape it with the pain of being betrayed by the one you love the most.\r\n\r\n" +
        "I will shape it with the fury of seeing what *I* made being misused by some other that knows no better.\r\n\r\n" +
        "I will shape it with the HUMILIATION of being DISCARDED over FILTHY, LESSER VERMIN.\r\n\r\n" +
        "I WILL SHAPE IT WITH MY BURNING DESIRE FOR VENGEANCE.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.Healing, ItemTag.WorldUnique, ItemTag.HoldoutZoneRelated];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += IrradiatedMoonstoneBoostStats;
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

    private void IrradiatedMoonstoneFasterProjectiles(On.RoR2.Projectile.ProjectileController.orig_Start orig, ProjectileController self)
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
                    var projectileSimple = self.gameObject.GetComponent<ProjectileSimple>();
                    if (projectileSimple)
                    {
                        projectileSimple.desiredForwardSpeed *= 1 + (IrradiatedMoonstoneProjectileSpeedInit + ((stackCount - 1) * IrradiatedMoonstoneProjectileSpeedStack));
                    }
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

    private void IrradiatedMoonstoneBoostStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            float decrease = (float)(1 - ((1 - IrradiatedMoonstoneCooldownReductionInit) * Math.Pow(1 - IrradiatedMoonstoneCooldownReductionStack, stackCount - 1)));
            args.cooldownMultAdd -= decrease;
            args.moveSpeedMultAdd += IrradiatedMoonstoneMovementInit + (stackCount - 1) * IrradiatedMoonstoneMovementStack;
            args.sprintSpeedAdd += IrradiatedMoonstoneSprintInit + (stackCount - 1) * IrradiatedMoonstoneSprintStack;
            args.attackSpeedMultAdd += IrradiatedMoonstoneAttackSpeedInit + (stackCount - 1) * IrradiatedMoonstoneAttackSpeedStack;
            args.regenMultAdd += IrradiatedMoonstoneRegenInit + (stackCount - 1) * IrradiatedMoonstoneRegenStack;
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
        IrradiatedMoonstoneCooldownReductionInit = config.ActiveBind("Item: " + ItemName, "Cooldown reduction with one " + ItemName, 0.15f, "How much should cooldowns be reduced by with one " + ItemName + "? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");
        IrradiatedMoonstoneCooldownReductionStack = config.ActiveBind("Item: " + ItemName, "Cooldown reduction per stack after one " + ItemName, 0.15f, "How much should cooldowns be reduced by per stack of " + ItemName + " after one? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");

        IrradiatedMoonstoneMovementInit = config.ActiveBind("Item: " + ItemName, "Movement speed with one " + ItemName, 0.15f, "How much should movement speed be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneMovementStack = config.ActiveBind("Item: " + ItemName, "Movement speed per stack after one " + ItemName, 0.15f, "How much should movement speed be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneSprintInit = config.ActiveBind("Item: " + ItemName, "Sprint speed with one " + ItemName, 0.15f, "How much should sprint speed be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneSprintStack = config.ActiveBind("Item: " + ItemName, "Sprint speed per stack after one " + ItemName, 0.15f, "How much should sprint speed be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneAttackSpeedInit = config.ActiveBind("Item: " + ItemName, "Attack speed with one " + ItemName, 0.15f, "How much should attack speed be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneAttackSpeedStack = config.ActiveBind("Item: " + ItemName, "Attack speed per stack after one " + ItemName, 0.15f, "How much should attack speed be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneRegenInit = config.ActiveBind("Item: " + ItemName, "Health regeneration with one " + ItemName, 0.15f, "How much should health regeneration be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneRegenStack = config.ActiveBind("Item: " + ItemName, "Health regeneration per stack after one " + ItemName, 0.15f, "How much should health regeneration be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneSelfDebuffDurationReductionInit = config.ActiveBind("Item: " + ItemName, "Reduced debuff duration on holder with one " + ItemName, 0.15f, "How much should the durations of debuffs inflicted on the holder be reduced by with one " + ItemName + "? Collapse's damage will be reduced by this value instead. (0.15 = 15%)");
        IrradiatedMoonstoneSelfDebuffDurationReductionStack = config.ActiveBind("Item: " + ItemName, "Reduced debuff duration on holder per stack after one " + ItemName, 0.15f, "How much should the durations of debuffs inflicted on the holder be reduced by per stack of " + ItemName + " after one? Collapse's damage will be reduced by this value instead. (0.15 = 15%)");

        IrradiatedMoonstoneProjectileSpeedInit = config.ActiveBind("Item: " + ItemName, "Projectile speed with one " + ItemName, 0.15f, "How much should untargeted projectile speed be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneProjectileSpeedStack = config.ActiveBind("Item: " + ItemName, "Projectile speed per stack after one " + ItemName, 0.15f, "How much should untargeted projectile speed be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneTeleporterChargeRateIncreaseInit = config.ActiveBind("Item: " + ItemName, "Teleporter charge rate with one " + ItemName, 0.15f, "How much should holdout zone charge rate be increased by with one " + ItemName + "? (0.15 = 15%)");
        IrradiatedMoonstoneTeleporterChargeRateIncreaseStack = config.ActiveBind("Item: " + ItemName, "Teleporter charge rate per stack after one " + ItemName, 0.15f, "How much should holdout zone charge rate be increased by per stack of " + ItemName + " after one? (0.15 = 15%)");

        IrradiatedMoonstoneTeleporterChargeRateIncreaseCap = config.ActiveBind("Item: " + ItemName, "Teleporter charge rate cap for " + ItemName, 8, "After how many " + ItemName + "s should holdout zone charge rate not be increased?");
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
