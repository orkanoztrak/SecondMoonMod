using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.AttackTypes.Orbs.Item.Prototype.GravityFlask;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Prototype.GravityFlask.GravityFlask;

namespace SecondMoon.Items.Prototype.BloodInfusedCore;

public class BloodInfusedCore : Item<BloodInfusedCore>
{
    public static ConfigOption<float> BloodInfusedCoreHealOnHitInit;
    public static ConfigOption<float> BloodInfusedCoreHealOnHitStack;

    public static ConfigOption<float> BloodInfusedCoreBloodFrenzyThreshold;
    public static ConfigOption<float> BloodInfusedCoreBloodFrenzyDuration;
    public static ConfigOption<float> BloodInfusedCoreBloodFrenzyBoost;

    public override string ItemName => "Blood-Infused Core";

    public override string ItemLangTokenName => "SECONDMOONMOD_BLOOD_INFUSED_CORE";

    public override string ItemPickupDesc => "Heal on hit. Healing for a certain amount cleanses all debuffs and boosts movement speed, attack speed and damage.";

    public override string ItemFullDesc => $"Dealing damage <style=cIsHealing>heals</style> you for <style=cIsHealing>{BloodInfusedCoreHealOnHitInit * 100}%</style> <style=cStack>(+{BloodInfusedCoreHealOnHitStack * 100}% per stack)</style> of your <style=cIsHealing>maximum health</style>. " +
        $"100% of healing is stored as <color=#7CFDEA>Blood Frenzy</color> - when it reaches <style=cIsHealing>{BloodInfusedCoreBloodFrenzyThreshold * 100}%</style> of your <style=cIsHealing>maximum health</style>, <style=cIsUtility>cleanse</style> all negative effects " +
        $"and gain the <color=#7CFDEA>Blood Frenzy</color> buff which increases <style=cIsUtility>movement speed</style>, <style=cIsDamage>attack speed</style> and <style=cIsDamage>damage</style> by <color=#7CFDEA>{BloodInfusedCoreBloodFrenzyBoost * 100}%</color> " +
        $"for <color=#7CFDEA>{BloodInfusedCoreBloodFrenzyDuration}s</color>, during which you cannot store <color=#7CFDEA>Blood Frenzy</color>.";

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
        On.RoR2.GlobalEventManager.OnHitEnemy += BloodInfusedCoreHealOnHit;
    }

    private void BloodInfusedCoreHealOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        if (damageInfo.procCoefficient > 0 && damageInfo.attacker)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var attackerComponent = attackerBody.healthComponent;
                if (attackerComponent)
                {
                    var stackCount = GetCount(attackerBody);
                    if (stackCount > 0)
                    {
                        attackerComponent.HealFraction((BloodInfusedCoreHealOnHitInit + ((stackCount - 1) * BloodInfusedCoreHealOnHitStack)) * damageInfo.procCoefficient, default);
                    }
                }
            }
        }
    }

    private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        self.AddItemBehavior<BloodInfusedCoreBehavior>(self.inventory.GetItemCount(instance.ItemDef));
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
        BloodInfusedCoreHealOnHitInit = config.ActiveBind("Item " + ItemName, "Heal on hit with one " + ItemName, 0.025f, "What % of combined health should be healed on hit with one " + ItemName + "? (0.025 = 2.5%)");
        BloodInfusedCoreHealOnHitStack = config.ActiveBind("Item " + ItemName, "Heal on hit with per stack after one " + ItemName, 0.025f, "What % of combined health should be healed on hit per stack of " + ItemName + " after one? (0.025 = 2.5%)");
        BloodInfusedCoreBloodFrenzyThreshold = config.ActiveBind("Item " + ItemName, "Blood Frenzy proc health threshold", 0.25f, "After healing what % of combined health should Blood Frenzy activate? (0.25 = 25%)");
        BloodInfusedCoreBloodFrenzyDuration = config.ActiveBind("Item " + ItemName, "Blood Frenzy duration", 4f, "How many seconds should Blood Frenzy last?");
        BloodInfusedCoreBloodFrenzyBoost = config.ActiveBind("Item: " + ItemName, "Blood frenzy boost percent", 0.25f, "How much should Blood Frenzy increase movement speed, attack speed and damage by? (0.25 = 25%)");
    }

    public class BloodInfusedCoreBehavior : CharacterBody.ItemBehavior
    {
        public float BloodFrenzyPoolTracker;
        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            BloodFrenzyPoolTracker = 0;
            if (body)
            {
                if (body.healthComponent)
                {
                    On.RoR2.HealthComponent.SendHeal += UpdateBloodFrenzyPool;
                }
            }
        }

        private void UpdateBloodFrenzyPool(On.RoR2.HealthComponent.orig_SendHeal orig, GameObject target, float amount, bool isCrit)
        {
            var characterBody = target.GetComponent<CharacterBody>();
            if (characterBody)
            {
                if (characterBody.Equals(body) && !body.HasBuff(BloodFrenzy.instance.BuffDef))
                {
                    var component = body.healthComponent;
                    if (component && stack > 0)
                    {
                        BloodFrenzyPoolTracker += amount;
                        if (BloodFrenzyPoolTracker >= component.fullCombinedHealth * BloodInfusedCoreBloodFrenzyThreshold)
                        {
                            BloodFrenzyPoolTracker = 0;
                            Util.CleanseBody(body, true, false, true, true, true, true);
                            body.AddTimedBuffAuthority(BloodFrenzy.instance.BuffDef.buffIndex, BloodInfusedCoreBloodFrenzyDuration);
                        }
                    }
                }
            }
            orig(target, amount, isCrit);
        }

        private void OnDisable()
        {
            if (body && enabled)
            {
                On.RoR2.HealthComponent.SendHeal -= UpdateBloodFrenzyPool;
                if (body.HasBuff(BloodFrenzy.instance.BuffDef.buffIndex))
                {
                    body.RemoveBuff(BloodFrenzy.instance.BuffDef.buffIndex);
                }
            }
        }
    }
}
