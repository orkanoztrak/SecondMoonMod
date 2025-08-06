using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Void;
using SecondMoon.EquipmentlessElites.Lost;
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
    public static ConfigOption<float> CoreOfCorruptionVoidCorruptionChanceInit;
    public static ConfigOption<float> CoreOfCorruptionVoidCorruptionChanceStack;

    public static ConfigOption<float> TwistedRegretsNerfInit;
    public static ConfigOption<float> TwistedRegretsNerfStack;

    public static ConfigOption<float> TwistedRegretsLostHealthAndShieldBoost;

    public static ConfigOption<float> TwistedRegretsLostMovement;

    public static ConfigOption<float> TwistedRegretsLostOrbDamage;
    public static ConfigOption<float> TwistedRegretsLostOrbCollapseChance;

    public static ConfigOption<int> TwistedRegretsLostOrbCountBase;
    public static ConfigOption<int> TwistedRegretsLostOrbCountScaling;
    public static ConfigOption<float> TwistedRegretsLostOrbCountIncreaseThreshold;

    public override string ItemName => "Twisted Regrets";

    public override string ItemLangTokenName => "PROTOTYPEVOID";

    public override string ItemPickupDesc => "Become a Lost elite. Reduce ALL stats on enemies. <style=cIsVoid>Corrupts all <color=#7CFDEA>Prototype</color> items</style>.";

    public override string ItemFullDesc => $"Reduce <style=cIsUtility>ALL stats</style> on enemies by <style=cIsUtility>{TwistedRegretsNerfInit * 100}%</style> <style=cStack>(+{TwistedRegretsNerfStack * 100}% per stack)</style>. Become a <color=#7CFDEA>Lost elite</color>. <style=cIsVoid>Corrupts all <color=#7CFDEA>Prototype</color> items and equipment</style>.\r\n\r\n" +
        $"<color=#7CFDEA>Lost elites</color> have the following effects applied to them:\r\n" +
        $"•Split your <style=cIsHealing>combined health pool</style> equally into <style=cIsHealing>health and shields</style>, then increase both by <style=cIsHealing>{TwistedRegretsLostHealthAndShieldBoost * 100}%</style>. Your <style=cIsHealth>one-shot protection</style> is now calculated for <style=cIsHealing>health and shields</style> separately.\r\n" +
        $"•Increase <style=cIsUtility>movement speed</style> by <style=cIsUtility>{TwistedRegretsLostMovement * 100}%</style> and gain the ability to <style=cIsUtility>sprint in all directions</style>. Your skills become <style=cIsUtility>agile</style>. <style=cIsUtility>You are immune to fall damage</style>.\r\n " +
        $"•Skill hits apply <style=cIsUtility>Cripple</style> and fire <style=cIsDamage>{TwistedRegretsLostOrbCountBase} (+{TwistedRegretsLostOrbCountScaling} per {TwistedRegretsLostOrbCountIncreaseThreshold * 100}% base damage) (scaling with proc coefficient) homing missiles</style> with <style=cIsDamage>{TwistedRegretsLostOrbDamage * 100}%</style> base damage and <style=cIsDamage>0</style> proc coefficient each. " +
        $"These <style=cIsDamage>missiles</style> have a <style=cIsDamage>{TwistedRegretsLostOrbCollapseChance}%</style> chance to apply <style=cIsDamage>Collapse</style>.";

    public override string ItemLore => "How many years had it been now? Perhaps decades, centuries, eons? Since he had an equal stand beside him and reach where he couldn't? He had long since forgotten.\r\n\r\n" +
        "For the most part, remembering those times was frustrating. He remembered the disagreements. He remembered their fights. He remembered how his brother just wouldn't respect his wishes, and see what he saw. A single moment of justice, a single moment of betrayal. Divided, never to rejoin.\r\n\r\n" +
        "But even he, godlike and ancient, felt nostalgia. Sometimes, instead of the bitter, painful past, he would remember joy. How the three of them would frolick in the meadow. How they all laughed from the bottom of their hearts, and how they created wonders. Nothing was impossible for them.\r\n\r\n" +
        "At these times, his heart would clench and something would well up, from deep within. At these times, he wondered if things could have been different. If they could get it right next time.\r\n\r\n";

    public override ItemTierDef ItemTierDef => VoidTierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.SprintRelated, ItemTag.BrotherBlacklist];

    public override ItemTierDef ItemTierToCorrupt => TierPrototype.instance.ItemTierDef;

    private static int TwistedRegretsPlayersStackTracker = 0;
    private static int TwistedRegretsMonsterStackTracker = 0;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
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
        for (int i = 0; i < readOnlyInstancesList.Count; i++)
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
        CoreOfCorruptionVoidCorruptionChanceInit = config.ActiveBind("Item: " + ItemName, "Void enhancement chance with one " + CoreOfCorruption.instance.ItemName, 5f, "The % chance any spawned enemy also has Void powers with one " + CoreOfCorruption.instance.ItemName + ". Stacks are counted across the players on the team.");
        CoreOfCorruptionVoidCorruptionChanceStack = config.ActiveBind("Item: " + ItemName, "Void enhancement chance per stack of " + CoreOfCorruption.instance.ItemName + " after one", 5f, "The % chance any spawned enemy also has Void powers per stack of " + CoreOfCorruption.instance.ItemName + " after one. Stacks are counted across the players on the team.");
        TwistedRegretsNerfInit = config.ActiveBind("Item: " + ItemName, "Enemy stat nerf with one " + ItemName, 0.1f, "How much should ALL stats on enemies (refer to Irradiant Pearl on the wiki) be reduced with one " + ItemName + "? This scales exponentially (0.1 = 10%, refer to Alien Head on the wiki).");
        TwistedRegretsNerfStack = config.ActiveBind("Item: " + ItemName, "Enemy stat nerf per stack after one " + ItemName, 0.1f, "How much should ALL stats on enemies (refer to Irradiant Pearl on the wiki) be reduced per stack of " + ItemName + " after one? This scales exponentially (0.1 = 10%, refer to Alien Head on the wiki).");
        TwistedRegretsLostHealthAndShieldBoost = config.ActiveBind("Item: " + ItemName, "Health and shield boost for Lost Elites after the shield - health split", 0.5f, "Lost Elites split their combined health pool equally into shields and health, and boost both of the resulting pools. What should this boost be? (0.5 = 50%)");
        TwistedRegretsLostMovement = config.ActiveBind("Item: " + ItemName, "Movement speed for Lost Elites", 0.2f, "How much should movement speed be increased for Lost Elites? (0.2 = 20%)");
        TwistedRegretsLostOrbDamage = config.ActiveBind("Item: " + ItemName, "Damage of the missiles launched on hit for Lost Elites", 0.2f, "What % of base damage should the missiles launched on hit from " + Lost.instance.EliteName + " Elites do? (0.2 = 10%)");
        TwistedRegretsLostOrbCollapseChance = config.ActiveBind("Item: " + ItemName, "Chance to apply Collapse on missile hit", 10f, "Upon hitting an enemy, there is a this percent chance that the missiles launched on hit from " + Lost.instance.EliteName + " Elites apply Collapse.");
        TwistedRegretsLostOrbCountBase = config.ActiveBind("Item: " + ItemName, "Number of missiles fired at base", 1, "At 1 proc coefficient, a hit will fire at least this many missiles, not counting damage scaling.");
        TwistedRegretsLostOrbCountScaling = config.ActiveBind("Item: " + ItemName, "Number to increase missile count by at threshold", 1, "Every threshold reached will increase missile count by this number.");
        TwistedRegretsLostOrbCountIncreaseThreshold = config.ActiveBind("Item: " + ItemName, "Percentage damage threshold to increase missile count", 2f, "Every this percent base damage beyond the threshold will increase missile count (2 = 200%). I recommend referring to the detailed description in-game to make better sense of missile count scaling.");
    }
}
