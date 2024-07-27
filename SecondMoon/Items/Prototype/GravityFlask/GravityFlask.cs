using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.AttackTypes.Orbs.Item.Prototype.GravityFlask;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.GravityFlask;

public class GravityFlask : Item<GravityFlask>
{
    public static ConfigOption<int> GravityFlaskFirstThreshold;
    public static ConfigOption<int> GravityFlaskSecondThreshold;
    public static ConfigOption<int> GravityFlaskThirdThreshold;
    public static ConfigOption<int> GravityFlaskFinalThreshold;

    public static ConfigOption<float> GravityFlaskAttackSpeedInit;
    public static ConfigOption<float> GravityFlaskAttackSpeedStack;
    public static ConfigOption<float> GravityFlaskDamageInit;
    public static ConfigOption<float> GravityFlaskDamageStack;
    public static ConfigOption<float> GravityFlaskCritInit;
    public static ConfigOption<float> GravityFlaskCritStack;
    public static ConfigOption<float> GravityFlaskProcDamageInit;
    public static ConfigOption<float> GravityFlaskProcDamageStack;
    public static ConfigOption<float> GravityFlaskProcCoefficient;

    public static ConfigOption<float> GravityFlaskHealthInit;
    public static ConfigOption<float> GravityFlaskHealthStack;
    public static ConfigOption<float> GravityFlaskHealInit;
    public static ConfigOption<float> GravityFlaskHealStack;
    public static ConfigOption<float> GravityFlaskHealBoostInit;
    public static ConfigOption<float> GravityFlaskHealBoostStack;
    public static ConfigOption<float> GravityFlaskDamageReductionInit;
    public static ConfigOption<float> GravityFlaskDamageReductionStack;

    public static ConfigOption<float> GravityFlaskMovementInit;
    public static ConfigOption<float> GravityFlaskMovementStack;
    public static ConfigOption<float> GravityFlaskGoldInit;
    public static ConfigOption<float> GravityFlaskGoldStack;
    public static ConfigOption<float> GravityFlaskCooldownReductionInit;
    public static ConfigOption<float> GravityFlaskCooldownReductionStack;
    public static ConfigOption<float> GravityFlaskBonusBoostInit;
    public static ConfigOption<float> GravityFlaskBonusBoostStack;
    public override string ItemName => "Gravity Flask";

    public override string ItemLangTokenName => "SECONDMOONMOD_GRAVITY_FLASK";

    public override string ItemPickupDesc => "Gather items of different categories to get boosts according to the category. Hover over the item in your inventory to see the currently active boosts.";

    public override string ItemFullDesc => $"<color=#7CFDEA>For each item category, every certain number of items you have grant you the following boosts, with the {GravityFlaskFinalThreshold}-item bonuses only being applied once:</color>\r\n" +
        $"<style=cIsDamage>Damage:</style>\r\n" +
        $"•{GravityFlaskFirstThreshold}: Gain <style=cIsDamage>{GravityFlaskAttackSpeedInit * 100}%</style> <style=cStack>(+{GravityFlaskAttackSpeedStack * 100}% per stack)</style> attack speed.\r\n" +
        $"•{GravityFlaskSecondThreshold}: Gain <style=cIsDamage>{GravityFlaskDamageInit * 100}%</style> <style=cStack>(+{GravityFlaskDamageStack * 100}% per stack)</style> base damage.\r\n" +
        $"•{GravityFlaskThirdThreshold}: Gain <style=cIsDamage>{GravityFlaskCritInit}%</style> <style=cStack>(+{GravityFlaskCritStack}% per stack)</style> critical hit chance.\r\n" +
        $"•{GravityFlaskFinalThreshold}: Hits smite enemies for <style=cIsDamage>{GravityFlaskProcDamageInit * 100}%</style> <style=cStack>(+{GravityFlaskProcDamageStack * 100}% per stack)</style> TOTAL damage, with a proc coefficient of <style=cIsDamage>{GravityFlaskProcCoefficient}</style>.\r\n\r\n" +
        $"<style=cIsHealing>Healing:</style>\r\n" +
        $"•{GravityFlaskFirstThreshold}: Gain <style=cIsHealing>{GravityFlaskHealthInit * 100}%</style> <style=cStack>(+{GravityFlaskHealthStack * 100}% per stack)</style> maximum health.\r\n" +
        $"•{GravityFlaskSecondThreshold}: Heal from <style=cIsDamage>incoming damage</style> for <style=cIsHealing>{GravityFlaskHealInit}</style> <style=cStack>(+{GravityFlaskHealStack}% per stack)</style>.\r\n" +
        $"•{GravityFlaskThirdThreshold}: <style=cIsHealing>Heal +{GravityFlaskHealBoostInit * 100}%</style> <style=cStack>(+{GravityFlaskHealBoostStack * 100}% per stack)</style> more.\r\n" +
        $"•{GravityFlaskFinalThreshold}: <style=cIsDamage>Any damage you take</style> is reduced by <style=cIsDamage>{GravityFlaskDamageReductionInit * 100}%</style> <style=cStack>(+{GravityFlaskDamageReductionStack * 100}% per stack)</style>.\r\n\r\n" +
        $"<style=cIsUtility>Utility:</style>\r\n" +
        $"•{GravityFlaskFirstThreshold}: Gain <style=cIsUtility>{GravityFlaskMovementInit * 100}%</style> <style=cStack>(+{GravityFlaskMovementStack * 100}% per stack)</style> movement speed.\r\n" +
        $"•{GravityFlaskSecondThreshold}: Gain <style=cIsUtility>{GravityFlaskGoldInit * 100}%</style> <style=cStack>(+{GravityFlaskGoldStack * 100}% per stack)</style> more gold.\r\n" +
        $"•{GravityFlaskThirdThreshold}: <style=cIsUtility>Reduce skill cooldowns</style> by <style=cIsUtility>{GravityFlaskCooldownReductionInit * 100}%</style> <style=cStack>(+{GravityFlaskCooldownReductionStack * 100}% per stack)</style>.\r\n" +
        $"•{GravityFlaskFinalThreshold}: Increase <color=#7CFDEA>non-{GravityFlaskFinalThreshold}-item bonuses</color> granted by this by <color=#7CFDEA>{GravityFlaskBonusBoostInit * 100}%</color> <style=cStack>(+{GravityFlaskBonusBoostStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        RecalculateStatsAPI.GetStatCoefficients += GravityFlaskBuffStats;
        On.RoR2.HealthComponent.TakeDamage += GravityFlaskSetDamageTaken;
        On.RoR2.HealthComponent.TakeDamage += GravityFlaskHealOnDamaged;
        On.RoR2.HealthComponent.Heal += GravityFlaskIncreaseHeals;
        On.RoR2.GlobalEventManager.OnHitEnemy += GravityFlaskSmite;
        On.RoR2.CharacterMaster.GiveMoney += GravityFlaskMoreGold;
    }

    private void GravityFlaskMoreGold(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
    {
        uint newAmount = amount;
        var body = self.GetBody();
        if (body)
        {
            var stackCount = GetCount(self);
            if (stackCount > 0)
            {
                var tracker = body.gameObject.GetComponent<GravityFlaskBehavior>();
                if (tracker)
                {
                    var finalUtilityCoefficient = tracker.GravityFlaskUtilityTracker >= GravityFlaskFinalThreshold ? 1f + (GravityFlaskBonusBoostInit + (stackCount - 1) * GravityFlaskBonusBoostStack) : 1f;
                    newAmount *= (uint)(1 + ((GravityFlaskGoldInit + ((stackCount - 1) * GravityFlaskGoldStack)) * (tracker.GravityFlaskUtilityTracker / GravityFlaskSecondThreshold) * finalUtilityCoefficient));
                }
            }
        }
        orig(self, newAmount);
    }

    private void GravityFlaskHealOnDamaged(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        orig(self, damageInfo);
        if (damageInfo.damage > 0)
        {
            var body = self.body;
            if (body)
            {
                var stackCount = GetCount(body);
                if (stackCount > 0)
                {
                    var tracker = body.gameObject.GetComponent<GravityFlaskBehavior>();
                    if (tracker)
                    {
                        var finalUtilityCoefficient = tracker.GravityFlaskUtilityTracker >= GravityFlaskFinalThreshold ? 1f + (GravityFlaskBonusBoostInit + (stackCount - 1) * GravityFlaskBonusBoostStack) : 1f;
                        var healAmount = (GravityFlaskHealInit + ((stackCount - 1) * GravityFlaskHealStack)) * (tracker.GravityFlaskHealingTracker / GravityFlaskSecondThreshold) * finalUtilityCoefficient;
                        self.Heal(healAmount, default);
                    }
                }
            }
        }
    }

    private void GravityFlaskSmite(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var attacker = attackerBody.master;
            var victimBody = victim.GetComponent<CharacterBody>();
            if (attacker && attackerBody && victimBody)
            {
                var stackCount = GetCount(attacker);
                if (stackCount > 0)
                {
                    var tracker = attackerBody.gameObject.GetComponent<GravityFlaskBehavior>();
                    if (tracker)
                    {
                        if (tracker.GravityFlaskDamageTracker >= GravityFlaskFinalThreshold && Util.CheckRoll(100 * damageInfo.procCoefficient, attacker))
                        { 
                            var smiteDamage = GravityFlaskProcDamageInit + ((stackCount - 1) * GravityFlaskProcDamageStack);
                            var smiteOrb = new GravityFlaskSmiteOrb
                            {
                                attacker = attackerBody.gameObject,
                                damageColorIndex = DamageColorIndex.Item,
                                damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, smiteDamage),
                                isCrit = damageInfo.crit,
                                procChainMask = damageInfo.procChainMask,
                            };
                            HurtBox target = victimBody.mainHurtBox;
                            if (target)
                            {
                                if ((bool)victimBody.hurtBoxGroup)
                                {
                                    target = victimBody.hurtBoxGroup.hurtBoxes[UnityEngine.Random.Range(0, victimBody.hurtBoxGroup.hurtBoxes.Length)];
                                }
                                smiteOrb.target = target;
                                OrbManager.instance.AddOrb(smiteOrb);
                                DamageInfo newDamageInfo = new()
                                {
                                    damage = smiteOrb.damageValue,
                                    crit = smiteOrb.isCrit,
                                    inflictor = damageInfo.inflictor,
                                    attacker = smiteOrb.attacker,
                                    position = damageInfo.position,
                                    force = damageInfo.force,
                                    rejected = damageInfo.rejected,
                                    procChainMask = damageInfo.procChainMask,
                                    procCoefficient = GravityFlaskProcCoefficient,
                                    damageType = smiteOrb.damageType,
                                    damageColorIndex = smiteOrb.damageColorIndex
                                };
                                orig(self, newDamageInfo, victim);
                                GlobalEventManager.instance.OnHitAll(newDamageInfo, victim);
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private float GravityFlaskIncreaseHeals(On.RoR2.HealthComponent.orig_Heal orig, HealthComponent self, float amount, ProcChainMask procChainMask, bool nonRegen)
    {
        var boostedAmount = amount;
        var body = self.body;
        if (body)
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                var tracker = body.gameObject.GetComponent<GravityFlaskBehavior>();
                if (tracker)
                {
                    var finalUtilityCoefficient = tracker.GravityFlaskUtilityTracker >= GravityFlaskFinalThreshold ? 1f + (GravityFlaskBonusBoostInit + (stackCount - 1) * GravityFlaskBonusBoostStack) : 1f;
                    boostedAmount *= 1 + ((GravityFlaskHealBoostInit + ((stackCount - 1) * GravityFlaskHealBoostStack)) * (tracker.GravityFlaskHealingTracker / GravityFlaskThirdThreshold) * finalUtilityCoefficient);
                }
            }
        }
        return orig(self, boostedAmount, procChainMask, nonRegen);
    }

    private void GravityFlaskSetDamageTaken(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        var newDamageInfo = damageInfo;
        var body = self.body;
        if (body)
        {
            var stackCount = GetCount(body);
            if (stackCount > 0)
            {
                var tracker = body.gameObject.GetComponent<GravityFlaskBehavior>();
                if (tracker)
                {
                    var coeff = tracker.GravityFlaskHealingTracker >= GravityFlaskFinalThreshold ? GravityFlaskDamageReductionInit * (float)Math.Pow(GravityFlaskDamageReductionStack, stackCount - 1) : 1;
                    newDamageInfo.damage *= coeff;
                }
            }
        }
        orig(self, newDamageInfo);
    }

    private void GravityFlaskBuffStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            var tracker = sender.gameObject.GetComponent<GravityFlaskBehavior>();
            if (tracker)
            {
                var finalUtilityCoefficient = tracker.GravityFlaskUtilityTracker >= GravityFlaskFinalThreshold ? 1f + (GravityFlaskBonusBoostInit + (stackCount - 1) * GravityFlaskBonusBoostStack) : 1f;
                args.attackSpeedMultAdd += (GravityFlaskAttackSpeedInit + ((stackCount - 1) * GravityFlaskAttackSpeedStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskFirstThreshold) * finalUtilityCoefficient;
                args.damageMultAdd += (GravityFlaskDamageInit + ((stackCount - 1) * GravityFlaskDamageStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskSecondThreshold) * finalUtilityCoefficient;
                args.critAdd += (GravityFlaskCritInit + ((stackCount - 1) * GravityFlaskCritStack)) * (tracker.GravityFlaskDamageTracker / GravityFlaskThirdThreshold) * finalUtilityCoefficient;
                args.healthMultAdd += (GravityFlaskHealthInit + ((stackCount - 1) * GravityFlaskHealthStack)) * (tracker.GravityFlaskHealingTracker / GravityFlaskFirstThreshold) * finalUtilityCoefficient;
                args.moveSpeedMultAdd += (GravityFlaskMovementInit + ((stackCount - 1) * GravityFlaskMovementStack)) * (tracker.GravityFlaskUtilityTracker / GravityFlaskFirstThreshold) * finalUtilityCoefficient;
                float initCDR = (float)Math.Pow(1 - (GravityFlaskCooldownReductionInit * finalUtilityCoefficient), tracker.GravityFlaskUtilityTracker / GravityFlaskThirdThreshold);
                if (initCDR < 0)
                {
                    initCDR = 0;
                }
                float stackCDR = (float)Math.Pow(1 - (GravityFlaskCooldownReductionStack * finalUtilityCoefficient), tracker.GravityFlaskUtilityTracker / GravityFlaskThirdThreshold);
                if (stackCDR < 0)
                {
                    stackCDR = 0;
                }
                float decrease = (float)(1 - (initCDR * Math.Pow(stackCDR, stackCount - 1)));
                args.cooldownMultAdd -= decrease;
            }
        }
    }

    [Server]
    private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        self.AddItemBehavior<GravityFlaskBehavior>(self.inventory.GetItemCount(instance.ItemDef));
        orig(self);
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            OrbAPI.AddOrb(typeof(GravityFlaskSmiteOrb));
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        GravityFlaskFirstThreshold = config.ActiveBind("Item: " + ItemName, "First threshold", 1, "Every this many items the holder has of a category grants the first respective category boost.");
        GravityFlaskSecondThreshold = config.ActiveBind("Item: " + ItemName, "Second threshold", 5, "Every this many items the holder has of a category grants the second respective category boost.");
        GravityFlaskThirdThreshold = config.ActiveBind("Item: " + ItemName, "Third threshold", 10, "Every this many items the holder has of a category grants the third respective category boost.");
        GravityFlaskFinalThreshold = config.ActiveBind("Item: " + ItemName, "Final threshold", 20, "Having at least this many items of a category grants the final respective category boost.");

        GravityFlaskAttackSpeedInit = config.ActiveBind("Item: " + ItemName, "Attack speed with one " + ItemName, 0.015f, "How much should attack speed be increased by with one " + ItemName + "? (0.015 = 1.5%)");
        GravityFlaskAttackSpeedStack = config.ActiveBind("Item: " + ItemName, "Attack speed per stack after one " + ItemName, 0.015f, "How much should attack speed be increased by per stack of " + ItemName + " after one? (0.015 = 1.5%)");
        GravityFlaskDamageInit = config.ActiveBind("Item: " + ItemName, "Damage with one " + ItemName, 0.075f, "How much should damage be increased by with one " + ItemName + "? (0.075 = 7.5%)");
        GravityFlaskDamageStack = config.ActiveBind("Item: " + ItemName, "Damage per stack after one " + ItemName, 0.075f, "How much should damage be increased by per stack of " + ItemName + " after one? (0.075 = 7.5%)");
        GravityFlaskCritInit = config.ActiveBind("Item: " + ItemName, "Critical chance with one " + ItemName, 15f, "How much should critical chance be increased by with one " + ItemName + "?");
        GravityFlaskCritStack = config.ActiveBind("Item: " + ItemName, "Critical per stack after one " + ItemName, 15f, "How much should critical chance be increased by per stack of " + ItemName + " after one?");
        GravityFlaskProcDamageInit = config.ActiveBind("Item: " + ItemName, "Damage of the proc with one " + ItemName, 0.5f, "What % of TOTAL damage should the proc do with one " + ItemName + "? (0.5 = 50%)");
        GravityFlaskProcDamageStack = config.ActiveBind("Item: " + ItemName, "Damage of the proc per stack after one " + ItemName, 0.5f, "What % of TOTAL damage should be added to the proc per stack of " + ItemName + " after one? (0.5 = 50%)");
        GravityFlaskProcCoefficient = config.ActiveBind("Item: " + ItemName, "Proc coefficient of the proc", 1f, "What should the proc coefficient of the proc be?");

        GravityFlaskHealthInit = config.ActiveBind("Item: " + ItemName, "Maximum health increase with one " + ItemName, 0.015f, "How much should maximum health be increased by with one " + ItemName + "? (0.015 = 1.5%)");
        GravityFlaskHealthStack = config.ActiveBind("Item: " + ItemName, "Maximum health increase per stack after one " + ItemName, 0.015f, "How much should maximum health be increased by per stack of " + ItemName + " after one? (0.015 = 1.5%)");
        GravityFlaskHealInit = config.ActiveBind("Item " + ItemName, "Heal on getting damaged with one " + ItemName, 3f, "How much should the holder be healed by upon taking damage with one " + ItemName + "?");
        GravityFlaskHealStack = config.ActiveBind("Item " + ItemName, "Heal on getting damaged per stack after one " + ItemName, 3f, "How much should the holder be healed by upon taking damage per stack of " + ItemName + " after one?");
        GravityFlaskHealBoostInit = config.ActiveBind("Item: " + ItemName, "Heal increase with one " + ItemName, 0.5f, "How much should healing be increased by with one " + ItemName + "? (0.5 = 50%)");
        GravityFlaskHealBoostStack = config.ActiveBind("Item: " + ItemName, "Heal increase per stack after one " + ItemName, 0.5f, "How much should healing be increased by per stack of " + ItemName + " after one? (0.5 = 50%)");
        GravityFlaskDamageReductionInit = config.ActiveBind("Item: " + ItemName, "Multiplicative incoming damage reduction with one " + ItemName, 0.5f, "What should incoming damage be multiplicatively reduced to with one " + ItemName + "? This scales exponentially (0.5 = 50%, refer to Shaped Glass on the wiki).");
        GravityFlaskDamageReductionStack = config.ActiveBind("Item: " + ItemName, "Multiplicative incoming damage reduction per stack after one " + ItemName, 0.5f, "What should incoming damage be multiplicatively reduced to per stack of " + ItemName + " after one? This scales exponentially (0.5 = 50%, refer to Shaped Glass on the wiki).");

        GravityFlaskMovementInit = config.ActiveBind("Item: " + ItemName, "Movement speed with one " + ItemName, 0.015f, "How much should movement speed be increased by with one " + ItemName + "? (0.015 = 1.5%)");
        GravityFlaskMovementStack = config.ActiveBind("Item: " + ItemName, "Movement speed per stack after one " + ItemName, 0.015f, "How much should movement speed be increased by per stack of " + ItemName + " after one? (0.015 = 1.5%)");
        GravityFlaskGoldInit = config.ActiveBind("Item: " + ItemName, "Gold with one " + ItemName, 0.075f, "How much should gold gain be increased by with one " + ItemName + "? (0.075 = 7.5%)");
        GravityFlaskGoldStack = config.ActiveBind("Item: " + ItemName, "Gold per stack after one " + ItemName, 0.075f, "How much should gold gain be increased by per stack of " + ItemName + " after one? (0.075 = 7.5%)");
        GravityFlaskCooldownReductionInit = config.ActiveBind("Item: " + ItemName, "Cooldown reduction with one " + ItemName, 0.15f, "How much should cooldowns be reduced by with one " + ItemName + "? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");
        GravityFlaskCooldownReductionStack = config.ActiveBind("Item: " + ItemName, "Cooldown reduction per stack after one " + ItemName, 0.15f, "How much should cooldowns be reduced by per stack of " + ItemName + " after one? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");
        GravityFlaskBonusBoostInit = config.ActiveBind("Item: " + ItemName, "Bonus boost coefficient with one " + ItemName, 1f, "By what % should non-final category bonuses be boosted with one " + ItemName + "? (1 = 100%)");
        GravityFlaskBonusBoostStack = config.ActiveBind("Item: " + ItemName, "Bonus boost coefficient per stack after one " + ItemName, 1f, "By what % should non-final category bonuses be boosted per stack of " + ItemName + " after one? (1 = 100%)");
    }

    public class GravityFlaskBehavior : CharacterBody.ItemBehavior
    {
        public int GravityFlaskDamageTracker;
        public int GravityFlaskHealingTracker;
        public int GravityFlaskUtilityTracker;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                body.inventory.onInventoryChanged += UpdateTrackers;
                UpdateTrackers();
            }
        }

        private void UpdateTrackers()
        {
            GravityFlaskDamageTracker = 0;
            GravityFlaskHealingTracker = 0;
            GravityFlaskUtilityTracker = 0;
            if (body)
            {
                if (body.inventory)
                {
                    foreach (var item in body.inventory.itemAcquisitionOrder)
                    {
                        ItemDef itemDef = ItemCatalog.GetItemDef(item);
                        if (itemDef.tier != ItemTier.NoTier)
                        {
                            if (itemDef.ContainsTag(ItemTag.Damage))
                            {
                                GravityFlaskDamageTracker += body.inventory.itemStacks[(int)item];
                            }
                            if (itemDef.ContainsTag(ItemTag.Healing))
                            {
                                GravityFlaskHealingTracker += body.inventory.itemStacks[(int)item];
                            }
                            if (itemDef.ContainsTag(ItemTag.Utility))
                            {
                                GravityFlaskUtilityTracker += body.inventory.itemStacks[(int)item];
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
                if (body.inventory)
                {
                    body.inventory.onInventoryChanged -= UpdateTrackers;
                }
            }
        }
    }
}
