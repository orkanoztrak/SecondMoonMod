using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Prototype.TremorKnuckles;

public class TremorKnuckles : Item<TremorKnuckles>
{
    public static ConfigOption<int> TremorKnucklesRequiredPrimaryUses;
    public static ConfigOption<int> TremorKnucklesMaxCharges;
    public static ConfigOption<float> TremorKnucklesDamageBuffPerThresholdInit;
    public static ConfigOption<float> TremorKnucklesDamageBuffPerThresholdStack;
    public static ConfigOption<float> TremorKnucklesBombDamage;
    public override string ItemName => "Tremor Knuckles";

    public override string ItemLangTokenName => "TREMOR_KNUCKLES";

    public override string ItemPickupDesc => "Continued Primary skill use boosts damage and applies bombs on hit, until you use a non-Primary combat skill.";

    public override string ItemFullDesc => $"Using your <style=cIsUtility>Primary skill</style> <style=cIsDamage>{TremorKnucklesRequiredPrimaryUses}</style> times grants a stack of <color=#7CFDEA>Tremors</color>, which stacks up to <style=cIsDamage>{TremorKnucklesMaxCharges}</style> times. " +
        $"All stacks of <color=#7CFDEA>Tremors</color> are removed upon using a <style=cIsUtility>non-Primary combat skill</style>. " +
        $"Each stack of <color=#7CFDEA>Tremors</color> increases <style=cIsDamage>base damage</style> by <style=cIsDamage>{TremorKnucklesDamageBuffPerThresholdInit * 100}%</style> <style=cStack>(+{TremorKnucklesDamageBuffPerThresholdStack * 100}% per stack)</style>. " +
        $"Your attacks apply sticky bombs on hit that deal <style=cIsDamage>{TremorKnucklesBombDamage * 100}%</style> TOTAL damage while you have <color=#7CFDEA>Tremors</color>.";

    public override string ItemLore => "I don't like the phrase \"blind rage\". It implies that the rage itself is out of control or misguided, where in reality it just reflects the weakness of the mind of the one that is enraged. Rage should never be fully discarded, it exists as a reminder that we shouldn't take a beating, physically or mentally, lying down - getting angry means we have seen something that violates our sense of how things ought to be.\r\n\r\n" +
        "The truly strong know how to wield and direct their rage. Because when you control it and not the other way around, rage can become the ultimate source of focus, allowing you to single-mindedly work towards righting that wrong which made you angry in the first place. When you reach this point, the zenith upon which lies the perfect balance between rage and calm, you not only gain the greatest strength, but also discover your truest self.\r\n\r\n" +
        "- Elizabeth Briggs, Master of the Second Temple";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        IL.RoR2.CharacterBody.RecalculateStats += TremorKnucklesDamageBoost;
    }

    private void TremorKnucklesDamageBoost(ILContext il)
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
                    var stackCount = GetCount(body);
                    if (stackCount > 0)
                    {
                        var buffCount = body.GetBuffCount(Tremors.instance.BuffDef.buffIndex);
                        if (buffCount > 0)
                        {
                            body.damage *= 1 + ((TremorKnucklesDamageBuffPerThresholdInit + ((stackCount - 1) * TremorKnucklesDamageBuffPerThresholdStack)) * buffCount);
                        }
                    }
                }
            });
        }
    }

    private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        self.AddItemBehavior<TremorKnucklesBehavior>(self.inventory.GetItemCount(instance.ItemDef));
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
        TremorKnucklesRequiredPrimaryUses = config.ActiveBind("Item: " + ItemName, "Required Primary uses for a Tremors buff", 3, "After this many Primary uses, gain a stack of Tremors.");
        TremorKnucklesDamageBuffPerThresholdInit = config.ActiveBind("Item: " + ItemName, "Damage per effect stack with one " + ItemName, 0.1f, "How much should damage be increased by per Tremors stack with one " + ItemName + "? (0.1 = 10%)");
        TremorKnucklesDamageBuffPerThresholdStack = config.ActiveBind("Item: " + ItemName, "Damage per effect stack per stack after one " + ItemName, 0.1f, "How much should damage be increased by per Tremors stack per stack of " + ItemName + " after one? (0.1 = 10%)");
        TremorKnucklesMaxCharges = config.ActiveBind("Item: " + ItemName, "Maximum number of Tremors stacks", 5, "Tremors stacks up to this many times.");
        TremorKnucklesBombDamage = config.ActiveBind("Item: " + ItemName, "Tremors bomb damage", 0.5f, "How much TOTAL damage should the bombs applied on hit by the Tremors buff do? (0.5 = 50%)");
    }

    public class TremorKnucklesBehavior : CharacterBody.ItemBehavior
    {
        public int TremorKnucklesPrimaryTracker;
        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                body.onSkillActivatedServer += TremorKnucklesStacks;
                TremorKnucklesPrimaryTracker = 0;
            }
        }

        private void TremorKnucklesStacks(GenericSkill skill)
        {
            if (body)
            {
                SkillLocator skillLocator = body.skillLocator;
                if ((skillLocator?.primary) == skill)
                {
                    if (body.GetBuffCount(Tremors.instance.BuffDef) < TremorKnucklesMaxCharges)
                    {
                        TremorKnucklesPrimaryTracker++;
                        if (TremorKnucklesPrimaryTracker >= TremorKnucklesRequiredPrimaryUses)
                        {
                            TremorKnucklesPrimaryTracker = 0;
                            body.AddBuff(Tremors.instance.BuffDef);
                        }
                    }
                }
                else if (skill.isCombatSkill)
                {
                    TremorKnucklesPrimaryTracker = 0;
                    while (body.HasBuff(Tremors.instance.BuffDef))
                    {
                        body.RemoveBuff(Tremors.instance.BuffDef);
                    }
                }
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                while (body.HasBuff(Tremors.instance.BuffDef))
                {
                    body.RemoveBuff(Tremors.instance.BuffDef);
                }
                TremorKnucklesPrimaryTracker = 0;
                body.onSkillActivatedServer -= TremorKnucklesStacks;
            }
        }
    }
}
