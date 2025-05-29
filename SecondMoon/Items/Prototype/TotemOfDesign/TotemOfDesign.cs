using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Prototype;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Items.Prototype.GravityFlask;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.TotemOfDesign;

public class TotemOfDesign : Item<TotemOfDesign>
{
    public static ConfigOption<float> TotemOfDesignRegenIncreaseInit;
    public static ConfigOption<float> TotemOfDesignRegenIncreaseStack;
    public static ConfigOption<float> TotemOfDesignAttackSpeedIncreaseInit;
    public static ConfigOption<float> TotemOfDesignAttackSpeedIncreaseStack;
    public static ConfigOption<float> TotemOfDesignDamageIncreaseInit;
    public static ConfigOption<float> TotemOfDesignDamageIncreaseStack;

    public static ConfigOption<float> TotemOfDesignMovementDecreaseInit;
    public static ConfigOption<float> TotemOfDesignMovementDecreaseStack;
    public static ConfigOption<float> TotemOfDesignCooldownIncreaseInit;
    public static ConfigOption<float> TotemOfDesignCooldownIncreaseStack;
    public static ConfigOption<float> TotemOfDesignDamageDecreaseInit;
    public static ConfigOption<float> TotemOfDesignDamageDecreaseStack;
    public override string ItemName => "Totem of Design";

    public override string ItemLangTokenName => "TOTEM_OF_DESIGN";

    public override string ItemPickupDesc => "Buffs on you increase health regeneration, attack speed and damage. Debuffs on enemies decrease movement speed, cooldown reduction and damage.";

    public override string ItemFullDesc => $"Gain <color=#7CFDEA>1</color> <style=cStack>(+1 per stack)</style> stacks of the <color=#7CFDEA>Flawless Design</color> buff, which grants the following:\r\n" +
        $"• Increase <style=cIsHealing>health regeneration</style> by <style=cIsHealing>{TotemOfDesignRegenIncreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignRegenIncreaseStack * 100}% per buff)</color>.\r\n" +
        $"• Increase <style=cIsDamage>attack speed</style> by <style=cIsDamage>{TotemOfDesignAttackSpeedIncreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignAttackSpeedIncreaseStack * 100}% per buff)</color>.\r\n" +
        $"• Increase <style=cIsDamage>damage</style> by <style=cIsDamage>{TotemOfDesignDamageIncreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignDamageIncreaseStack * 100}% per buff)</color>.\r\n\r\n" +
        $"Hitting enemies permanently applies the <color=#7CFDEA>Flawed Design</color> debuff to them (stacks up to <color=#7CFDEA>1</color> <style=cStack>(+1 per stack)</style>) which grants the following:\r\n" +
        $"• Decrease <style=cIsUtility>movement speed</style> by <style=cIsUtility>{TotemOfDesignMovementDecreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignMovementDecreaseStack * 100}% per debuff)</color>.\r\n" +
        $"• Increase <style=cIsUtility>cooldowns</style> by <style=cIsUtility>{TotemOfDesignCooldownIncreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignCooldownIncreaseStack * 100}% per debuff)</color>.\r\n" +
        $"• Decrease <style=cIsDamage>damage</style> by <style=cIsDamage>{TotemOfDesignDamageDecreaseInit * 100}%</style> <color=#7CFDEA>(+{TotemOfDesignDamageDecreaseStack * 100}% per debuff)</color>.";

    public override string ItemLore => $"Have I ever told you why I love Design?\r\n\r\n" +
        $"Unlike the other compounds, Design does not have its own form. It is the creator's own skills and vision that give it form. In this regard, Design is the most abstract compound that also tells the most about the contraption and its maker.\r\n\r\n" +
        $"But how would an abundance of Design even work? After our last experiment, I got to thinking.\r\n\r\n" +
        $"This is exhilarating, brother. It is a challenge to our abilities.\r\n\r\n" +
        $"I gather a modest amount of Mass, Blood and Soul. After all, material is needed to build, and for Design to exist.\r\n\r\n" +
        $"I decided on a small likeness of a humanoid creature. It represents the apex of intelligence, and infinite possibility.\r\n\r\n" +
        $"Its purpose? To find faults and strengths in other Designs. Those will always exist, and this contraption will always detect them.\r\n\r\n" +
        $"This will be a reminder that greater heights always exist. Dormancy is death.\r\n\r\n" +
        $"The last contraption was a gift to you. This one... will be a gift to myself.";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Healing, ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += TotemOfDesignApplyDebuff;
        On.RoR2.CharacterBody.OnInventoryChanged += TotemOfDesignApplyBuff;
        IL.RoR2.CharacterBody.RecalculateStats += TotemOfDesignStatModifications;
        On.RoR2.GenericSkill.CalculateFinalRechargeInterval += TotemOfDesignIncreaseCooldownCeiling;
    }

    private float TotemOfDesignIncreaseCooldownCeiling(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
    {
        if (self.cooldownScale > 1f)
        {
            return Mathf.Min(self.baseRechargeInterval * self.cooldownScale, Mathf.Max(0.5f, self.baseRechargeInterval * self.cooldownScale - self.flatCooldownReduction));
        }
        else
        {
            return orig(self);
        }
    }

    private void TotemOfDesignStatModifications(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield"),
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_cursePenalty")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                if (body)
                {
                    int buffs = 0;
                    int debuffs = 0;
                    int flawless = body.GetBuffCount(FlawlessDesign.instance.BuffDef);
                    int flawed = body.GetBuffCount(FlawedDesign.instance.BuffDef);
                    if (flawless > 0 || flawed > 0)
                    {
                        BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
                        foreach (BuffIndex buffType in debuffBuffIndices)
                        {
                            if (body.HasBuff(buffType))
                            {
                                debuffs++;
                            }
                        }
                        DotController dotController = DotController.FindDotController(body.gameObject);
                        if ((bool)dotController)
                        {
                            for (DotController.DotIndex dotIndex = 0; dotIndex < (DotController.DotIndex)(DotAPI.VanillaDotCount + DotAPI.CustomDotCount); dotIndex++)
                            {
                                if (dotController.HasDotActive(dotIndex))
                                {
                                    debuffs++;
                                }
                            }
                        }
                        buffs = body.activeBuffsListCount - debuffs;
                        if (flawless > 0 && buffs > 0)
                        {
                            body.regen += Math.Abs(body.regen * (TotemOfDesignRegenIncreaseInit + ((flawless - 1) * TotemOfDesignRegenIncreaseStack)) * buffs);
                            body.attackSpeed *= 1 + ((TotemOfDesignAttackSpeedIncreaseInit + ((flawless - 1) * TotemOfDesignAttackSpeedIncreaseStack)) * buffs);
                            body.damage *= 1 + ((TotemOfDesignDamageIncreaseInit + ((flawless - 1) * TotemOfDesignDamageIncreaseStack)) * buffs);
                        }
                        if (flawed > 0 && debuffs > 0)
                        {
                            var decrease = (float)((1 - TotemOfDesignDamageDecreaseInit) * Math.Pow(1 - TotemOfDesignDamageDecreaseStack, debuffs - 1));
                            decrease = (float)(decrease * Math.Pow(decrease, flawed - 1));
                            if (body.skillLocator)
                            {
                                if (body.skillLocator.primaryBonusStockSkill)
                                {
                                    body.skillLocator.primaryBonusStockSkill.cooldownScale *= 1 + ((TotemOfDesignCooldownIncreaseInit + ((flawed - 1) * TotemOfDesignCooldownIncreaseStack)) * debuffs);
                                }
                                if (body.skillLocator.secondaryBonusStockSkill)
                                {
                                    body.skillLocator.secondaryBonusStockSkill.cooldownScale *= 1 + ((TotemOfDesignCooldownIncreaseInit + ((flawed - 1) * TotemOfDesignCooldownIncreaseStack)) * debuffs);
                                }
                                if (body.skillLocator.utilityBonusStockSkill)
                                {
                                    body.skillLocator.utilityBonusStockSkill.cooldownScale *= 1 + ((TotemOfDesignCooldownIncreaseInit + ((flawed - 1) * TotemOfDesignCooldownIncreaseStack)) * debuffs);
                                }
                                if (body.skillLocator.specialBonusStockSkill)
                                {
                                    body.skillLocator.specialBonusStockSkill.cooldownScale *= 1 + ((TotemOfDesignCooldownIncreaseInit + ((flawed - 1) * TotemOfDesignCooldownIncreaseStack)) * debuffs);
                                }
                            }
                            body.moveSpeed *= decrease;
                            body.damage *= decrease;
                        }
                    }
                }
            });
        }
    }

    private void TotemOfDesignApplyBuff(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        var stackCount = GetCount(self);
        if (stackCount > 0 && self.GetBuffCount(FlawlessDesign.instance.BuffDef.buffIndex) < stackCount)
        {
            while (self.GetBuffCount(FlawlessDesign.instance.BuffDef.buffIndex) < stackCount)
            {
                self.AddBuff(FlawlessDesign.instance.BuffDef);
            }
        }
        else if (stackCount <= 0 && self.GetBuffCount(FlawlessDesign.instance.BuffDef.buffIndex) > stackCount)
        {
            while (self.GetBuffCount(FlawlessDesign.instance.BuffDef.buffIndex) > stackCount)
            {
                self.RemoveBuff(FlawlessDesign.instance.BuffDef);
            }
        }
        orig(self);
    }

    private void TotemOfDesignApplyDebuff(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            if (damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                var victimBody = victim.GetComponent<CharacterBody>();
                if (attackerBody && victimBody)
                {
                    var stackCount = GetCount(attackerBody);
                    if (stackCount > 0 && victimBody.GetBuffCount(FlawedDesign.instance.BuffDef.buffIndex) < stackCount)
                    {
                        while (victimBody.GetBuffCount(FlawedDesign.instance.BuffDef.buffIndex) < stackCount)
                        {
                            victimBody.AddBuff(FlawedDesign.instance.BuffDef);
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
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
        TotemOfDesignRegenIncreaseInit = config.ActiveBind("Item: " + ItemName, "Health regeneration per buff with one " + ItemName, 0.15f, "How much should health regeneration be increased by per buff with one " + ItemName + "? (0.15 = 15%)");
        TotemOfDesignRegenIncreaseStack = config.ActiveBind("Item: " + ItemName, "Health regeneration per buff per stack after one " + ItemName, 0.15f, "How much should health regeneration be increased by per buff per stack of " + ItemName + " after one? (0.15 = 15%)");
        TotemOfDesignAttackSpeedIncreaseInit = config.ActiveBind("Item: " + ItemName, "Attack speed per buff with one " + ItemName, 0.15f, "How much should attack speed be increased by per buff with one " + ItemName + "? (0.15 = 15%)");
        TotemOfDesignAttackSpeedIncreaseStack = config.ActiveBind("Item: " + ItemName, "Attack speed per buff per stack after one " + ItemName, 0.15f, "How much should attack speed be increased by per buff per stack of " + ItemName + " after one? (0.15 = 15%)");
        TotemOfDesignDamageIncreaseInit = config.ActiveBind("Item: " + ItemName, "Damage per buff with one " + ItemName, 0.15f, "How much should damage be increased by per buff with one " + ItemName + "? (0.15 = 15%)");
        TotemOfDesignDamageIncreaseStack = config.ActiveBind("Item: " + ItemName, "Damage per buff per stack after one " + ItemName, 0.15f, "How much should damage be increased by per buff per stack of " + ItemName + " after one? (0.15 = 15%)");

        TotemOfDesignMovementDecreaseInit = config.ActiveBind("Item: " + ItemName, "Movement speed decrease per debuff with one " + ItemName, 0.15f, "How much should movement speed be decreased by per debuff with one " + ItemName + "? (0.15 = 15%)");
        TotemOfDesignMovementDecreaseStack = config.ActiveBind("Item: " + ItemName, "Movement speed decrease per debuff per stack after one " + ItemName, 0.15f, "How much should movement speed be decreased by per debuff per stack of " + ItemName + " after one? (0.15 = 15%)");
        TotemOfDesignCooldownIncreaseInit = config.ActiveBind("Item: " + ItemName, "Cooldown increase per debuff with one " + ItemName, 0.15f, "How much should cooldowns be increased by with one " + ItemName + "? (0.15 = 15%).");
        TotemOfDesignCooldownIncreaseStack = config.ActiveBind("Item: " + ItemName, "Cooldown increase per debuff per stack after one " + ItemName, 0.15f, "How much should cooldowns be increased by per debuff per stack of " + ItemName + " after one? (0.15 = 15%).");
        TotemOfDesignDamageDecreaseInit = config.ActiveBind("Item: " + ItemName, "Damage decrease per debuff with one " + ItemName, 0.15f, "How much should damage be decreased by per debuff with one " + ItemName + "? (0.15 = 15%)");
        TotemOfDesignDamageDecreaseStack = config.ActiveBind("Item: " + ItemName, "Damage decrease per debuff per stack after one " + ItemName, 0.15f, "How much should damage be decreased by per debuff per stack of " + ItemName + " after one? (0.15 = 15%)");
    }
}
