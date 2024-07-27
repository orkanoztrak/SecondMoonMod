using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Elites.Lost;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Items.ItemTiers.VoidTierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Rewired.ComponentControls.Effects.RotateAroundAxis;

namespace SecondMoon.Items.Void.TwistedRegrets;

public class TwistedRegrets : Item<TwistedRegrets>
{
    public static ConfigOption<float> TwistedRegretsNerfInit;
    public static ConfigOption<float> TwistedRegretsNerfStack;

    public static ConfigOption<float> TwistedRegretsLostHealthAndShieldBoost;

    public static ConfigOption<float> TwistedRegretsLostMovement;

    public static ConfigOption<float> TwistedRegretsLostBarrierDummyDamage;
    public static ConfigOption<float> TwistedRegretsLostBarrierCooldown;

    public static ConfigOption<float> TwistedRegretsLostOrbDamage;

    public override string ItemName => "Twisted Regrets";

    public override string ItemLangTokenName => "SECONDMOONMOD_PROTOTYPEVOID";

    public override string ItemPickupDesc => "Become a Lost elite. Reduce ALL stats on enemies. <style=cIsVoid>Corrupts all <color=#7CFDEA>Prototype</color> items</style>.";

    public override string ItemFullDesc => $"Reduce <style=cIsUtility>ALL stats</style> on enemies by <style=cIsUtility>{TwistedRegretsNerfInit * 100}%</style> <style=cStack>(+{TwistedRegretsNerfStack * 100}% per stack)</style>. Become a <color=#7CFDEA>Lost elite</color>. <style=cIsVoid>Corrupts all <color=#7CFDEA>Prototype</color> items</style>.\r\n\r\n" +
        $"<color=#7CFDEA>Lost elites</color> have the following effects applied to them:\r\n" +
        $"•Split your <style=cIsHealing>combined health pool</style> equally into <style=cIsHealing>health and shields</style>, then increase both by <style=cIsHealing>{TwistedRegretsLostHealthAndShieldBoost * 100}%</style>. Your <style=cIsHealth>one-shot protection</style> is now calculated for <style=cIsHealing>health and shields</style> separately.\r\n" +
        $"•Increase <style=cIsUtility>movement speed</style> by <style=cIsUtility>{TwistedRegretsLostMovement * 100}%</style> and gain the ability to <style=cIsUtility>sprint in all directions</style>. Your skills become <style=cIsUtility>[ Agile ]</style>. <style=cIsUtility>You are immune to fall damage</style>.\r\n " +
        $"•<style=cIsHealing>Block</style> incoming damage once. The attacker that triggered this has <style=cIsDamage>on hit effects</style> applied to them (this is treated as if they were hit by a <style=cIsDamage>{TwistedRegretsLostBarrierDummyDamage * 100}%</style> base damage attack with a proc coefficient of <style=cIsDamage>1</style>). Recharges every <style=cIsHealing>{TwistedRegretsLostBarrierCooldown}s</style> (<style=cIsHealing>{TwistedRegretsLostBarrierCooldown / 2}s</style> if disabled by no attacker).\r\n" +
        $"•Hits apply <style=cIsUtility>Cripple</style> and <style=cIsDamage>Collapse</style>, and fire a number of <style=cIsDamage>homing missiles</style> equal to the <style=cIsDamage>number of debuffs</style> on the target, with <style=cIsDamage>{TwistedRegretsLostOrbDamage * 100}%</style> base damage and <style=cIsDamage>0</style> proc coefficient each.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => VoidTierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.SprintRelated, ItemTag.BrotherBlacklist];

    public override ItemTierDef ItemTierToCorrupt => TierPrototype.instance.ItemTierDef;

    public static int TwistedRegretsPlayersStackTracker = 0;
    public static int TwistedRegretsMonsterStackTracker = 0;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += TwistedRegretsOnInventoryChanged;
        CharacterMaster.onCharacterMasterLost += TwistedRegretsUpdateCountOnDeaths;
        IL.RoR2.CharacterBody.RecalculateStats += TwistedRegretsNerfEnemies;
    }

    private void TwistedRegretsUpdateCountOnDeaths(CharacterMaster master)
    {
        switch (master.teamIndex)
        {
            case TeamIndex.Player:
                if (master.playerCharacterMasterController)
                {
                    TwistedRegretsPlayersStackTracker -= GetCount(master);
                }
                break;
            case TeamIndex.Monster:
                TwistedRegretsMonsterStackTracker -= GetCount(master);
                break;
        }
    }

    private void TwistedRegretsNerfEnemies(ILContext il)
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
                    if (body.teamComponent)
                    {
                        float decrease = 0;
                        switch (body.teamComponent.teamIndex)
                        {
                            case TeamIndex.Player:
                                if (TwistedRegretsMonsterStackTracker > 0)
                                {
                                    decrease = (float)((1 - TwistedRegretsNerfInit) * Math.Pow(1 - TwistedRegretsNerfStack, TwistedRegretsMonsterStackTracker - 1));
                                }
                                break;
                            case TeamIndex.Monster:
                                if (TwistedRegretsPlayersStackTracker > 0)
                                {
                                    decrease = (float)((1 - TwistedRegretsNerfInit) * Math.Pow(1 - TwistedRegretsNerfStack, TwistedRegretsPlayersStackTracker - 1));
                                }
                                break;
                        }
                        if (decrease > 0)
                        {
                            body.maxHealth *= decrease;
                            body.regen *= decrease;
                            body.moveSpeed *= decrease;
                            body.damage *= decrease;
                            body.attackSpeed *= decrease;
                            body.crit *= decrease;
                            body.armor *= decrease;
                        }
                    }
                }
            });
        }
    }

    private void TwistedRegretsOnInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        var stackCount = GetCount(self);
        if (stackCount > 0 && !self.HasBuff(LostBuff.instance.BuffDef))
        {
            self.AddBuff(LostBuff.instance.BuffDef);
        }
        else if (stackCount <= 0 && self.HasBuff(LostBuff.instance.BuffDef))
        {
            self.RemoveBuff(LostBuff.instance.BuffDef);
        }
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
        TwistedRegretsPlayersStackTracker = num;
        TwistedRegretsMonsterStackTracker = Util.GetItemCountForTeam(TeamIndex.Monster, ItemDef.itemIndex, requiresAlive: true, requiresConnected: false);
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
        TwistedRegretsNerfInit = config.ActiveBind("Item: " + ItemName, "Enemy stat nerf with one " + ItemName, 0.15f, "How much should ALL stats on enemies (refer to Irradiant Pearl on the wiki) be reduced with one " + ItemName + "? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");
        TwistedRegretsNerfStack = config.ActiveBind("Item: " + ItemName, "Enemy stat nerf per stack after one " + ItemName, 0.15f, "How much should ALL stats on enemies (refer to Irradiant Pearl on the wiki) be reduced per stack of " + ItemName + " after one? This scales exponentially (0.15 = 15%, refer to Alien Head on the wiki).");
        TwistedRegretsLostHealthAndShieldBoost = config.ActiveBind("Item: " + ItemName, "Health and shield boost for Lost Elites after the shield - health split", 0.5f, "Lost Elites split their combined health pool equally into shields and health, and boost both of the resulting pools. What should this boost be? (0.5 = 50%)");
        TwistedRegretsLostMovement = config.ActiveBind("Item: " + ItemName, "Movement speed for Lost Elites", 0.2f, "How much should movement speed be increased for Lost Elites? (0.2 = 20%)");
        TwistedRegretsLostBarrierDummyDamage = config.ActiveBind("Item: " + ItemName, "Damage value to be used for block proc calculation", 4f, "On-hit effects are applied to the attacker that breaks the barrier of a Lost Elite. What should the damage value to be used for the calculations of this proc be? (4 = 400% base damage)");
        TwistedRegretsLostBarrierCooldown = config.ActiveBind("Item: " + ItemName, "Block cooldown for Lost Elites", 12f, "The ability to block the next incoming hit has a cooldown of this many seconds. If the barrier didn't activate from an attack from an enemy, this cooldown is halved.");
        TwistedRegretsLostOrbDamage = config.ActiveBind("Item: " + ItemName, "Damage of the missiles launched on hit for Lost Elites", 0.1f, "What % of base damage should the missiles launched on hit from Lost Elites do? (0.1 = 10%)");
    }
}
