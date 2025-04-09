using BepInEx.Configuration;
using MonoMod.Cil;
using Newtonsoft.Json.Utilities;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Prototype.FlailOfMass;

public class FlailOfMass : Item<FlailOfMass>
{
    public static ConfigOption<int> FlailOfMassMomentumLimit;
    public static ConfigOption<float> FlailOfMassRadius;
    public static ConfigOption<float> FlailOfMassDamageMultiplierInit;
    public static ConfigOption<float> FlailOfMassDamageMultiplierStack;
    public static ConfigOption<float> FlailOfMassProcCoefficient;
    public static ConfigOption<float> FlailOfMassMomentumBuildRate;
    public static ConfigOption<float> FlailOfMassMomentumDecayRate;
    public static ConfigOption<float> FlailOfMassHealthAndShieldIncreaseBoost;

    public static List<BuffIndex> FlailOfMassSlowsAndRoots = new List<BuffIndex>();
    public static string[] FlailOfMassSlowAndRootNames = ["bdEntangle", "bdNullified", "bdNullifyStack", "bdLunarSecondaryRoot", "bdSlow50", "bdSlow60", "bdSlow80", "bdClayGoo", "bdSlow30", "bdJailerSlow", "bdJailerTether"];
    public override string ItemName => "Flail of Mass";

    public override string ItemLangTokenName => "FLAIL_OF_MASS";

    public override string ItemPickupDesc => "Become immune to stuns and slows, and increase gains to health and shields, excluding by leveling. Consistent sprinting builds up a massive attack.";

    public override string ItemFullDesc => $"<color=#7CFDEA>Become immune to stuns and slows.</color> " +
        $"All increases to <style=cIsHealing>health and shields</style> are increased by <style=cIsHealing>{FlailOfMassHealthAndShieldIncreaseBoost * 100}%</style>. " +
        $"While sprinting, gain a <color=#7CFDEA>{Momentum.instance.Name}</color> buff every <style=cIsUtility>{FlailOfMassMomentumBuildRate}s</style> (gain rate increases with <style=cIsUtility>movement speed</style> and <style=cIsHealing>bonus health and shields</style>). Lose a stack of this buff every <style=cIsUtility>{FlailOfMassMomentumDecayRate}s</style> while not sprinting. " +
        $"At <color=#7CFDEA>{FlailOfMassMomentumLimit}</color> stacks, <color=#7CFDEA>{Momentum.instance.Name}</color> stops decaying and using your <style=cIsUtility>Primary skill</style> consumes all <color=#7CFDEA>{Momentum.instance.Name}</color> stacks " +
        $"to fire an explosive homing projectile that deals <style=cIsDamage>{FlailOfMassDamageMultiplierInit * 100}%</style> <style=cStack>(+{FlailOfMassDamageMultiplierStack * 100}% per stack)</style> damage in a <style=cIsDamage>{FlailOfMassRadius}m</style> radius with a proc coefficient of <style=cIsDamage>{FlailOfMassProcCoefficient}</style>.";

    public override string ItemLore => "We know the principle compounds now, brother - Mass, Design, Blood and Soul. But what would happen in an abundance of any in a structure?\r\n\r\n" +
        "To start, I choose Mass. Perhaps using unusually high quantities will make it amusing instead of boring.\r\n\r\n" +
        "My design will lend itself to being swung, taking advantage of the size and weight. The wielder's strength matters not besides being able to lift it in the first place, such is the quantity of Mass used.\r\n\r\n" +
        "However, no such feasible wielder can exist as is. It is too cumbersome for mortal use, and we have no need for it, given our abundant might. You see, brother, the saddest fate for a creation is to never prove of any use.\r\n\r\n" +
        "Thus, a smattering of Blood and Soul will allow it to guide its wielder in handling it. They will be as unstoppable as the contraption itself.\r\n\r\n" +
        "This is my gift to you, brother. I want you to gift this contraption to one you find worthy, because you love mortals so much. In doing so, you will also gift our creation its purpose. And me, the joy of seeing it put to use.";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.Damage, ItemTag.SprintRelated];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += FlailOfMassRegisterDebuffsToIgnore;
        On.RoR2.CharacterBody.AddBuff_BuffIndex += FlailOfMassIgnoreSlowingDebuffs;
        On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += FlailOfMassIgnoreSlowingDebuffs;
        IL.RoR2.CharacterBody.RecalculateStats += FlailOfMassIgnoreSlowsAndBoostHealthAndShieldGains;
        On.RoR2.CharacterBody.OnInventoryChanged += FlailOfMassAddIgnoreStunBehavior;
    }

    private void FlailOfMassIgnoreSlowingDebuffs(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        if (FlailOfMassSlowsAndRoots.Contains(buffDef.buffIndex))
        {
            var stackCount = GetCount(self);
            if (stackCount == 0)
            {
                orig(self, buffDef, duration);
            }
        }
        else
        {
            orig(self, buffDef, duration);
        }
    }

    private void FlailOfMassIgnoreSlowingDebuffs(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
    {
        if (!NetworkServer.active)
        {
            orig(self, buffType);
        }
        else
        {
            if (FlailOfMassSlowsAndRoots.Contains(buffType))
            {
                var stackCount = GetCount(self);
                if (stackCount == 0)
                {
                    orig(self, buffType);
                }
            }
            else
            {
                orig(self, buffType);
            }
        }
    }

    private void FlailOfMassRegisterDebuffsToIgnore()
    {
        foreach (var debuff in BuffCatalog.buffDefs)
        {
            if (FlailOfMassSlowAndRootNames.Contains(debuff.name))
            {
                FlailOfMassSlowsAndRoots.Add(debuff.buffIndex);
            }
        }
    }

    private void FlailOfMassAddIgnoreStunBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (NetworkServer.active) 
        {
            if (self.healthComponent)
            {
                if (self.healthComponent.GetComponent<SetStateOnHurt>())
                {
                    self.AddItemBehavior<FlailOfMassIgnoreStunBehavior>(self.inventory.GetItemCount(instance.ItemDef));
                }
            }
        }
        orig(self);
    }

    private void FlailOfMassIgnoreSlowsAndBoostHealthAndShieldGains(ILContext il)
    {
        int movementTrackerIndex = 84;
        int speedTrackerIndex = 85;
        int slowTrackerIndex = 86;
        int buffCountIndex = 65;

        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(x => x.MatchLdloc(movementTrackerIndex),
                        x => x.MatchLdloc(speedTrackerIndex),
                        x => x.MatchLdloc(slowTrackerIndex)))
        {
            ILLabel target = cursor.MarkLabel();

            if (cursor.TryGotoNext(x => x.MatchStloc(movementTrackerIndex),
                            x => x.MatchLdloc(buffCountIndex),
                            x => x.MatchLdcI4(0)))
            {
                cursor.Index++;
                ILLabel target2 = cursor.MarkLabel();

                if (cursor.TryGotoPrev(x => x.MatchLdloc(movementTrackerIndex)))
                {
                    cursor.MoveBeforeLabels();
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<CharacterBody, int>>(GetCount);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ble_S, target.Target);

                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, movementTrackerIndex);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, speedTrackerIndex);
                    cursor.EmitDelegate<Func<float, float, float>>((movement, speed) =>
                    {
                        movement *= speed;
                        return movement;
                    });
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, movementTrackerIndex);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Br, target2.Target);
                    if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                        x => x.MatchLdcR4(0),
                        x => x.MatchLdarg(0),
                        x => x.MatchCallOrCallvirt<CharacterBody>("get_oneShotProtectionFraction"),
                        x => x.MatchLdcR4(1),
                        x => x.MatchLdcR4(1),
                        x => x.MatchLdarg(0),
                        x => x.MatchCallOrCallvirt<CharacterBody>("get_cursePenalty")))
                    {
                        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                        cursor.EmitDelegate<Action<CharacterBody>>((body) =>
                        {
                            if (body)
                            {
                                if (GetCount(body) > 0)
                                {
                                    var unchangedHealth = body.baseMaxHealth + body.levelMaxHealth * (body.level - 1);
                                    var unchangedShield = body.baseMaxShield + body.levelMaxShield * (body.level - 1);
                                    body.maxHealth += (body.maxHealth - unchangedHealth) *  FlailOfMassHealthAndShieldIncreaseBoost;
                                    body.maxShield += (body.maxShield - unchangedShield) * FlailOfMassHealthAndShieldIncreaseBoost;
                                }
                            }
                        });
                    }
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
        FlailOfMassMomentumLimit = config.ActiveBind("Item: " + ItemName, "Momentum stacks necessary to access the attack", 150, "At this number of Momentum stacks, they will not decay until the attack is fired.");
        FlailOfMassRadius = config.ActiveBind("Item: " + ItemName, "Attack explosion radius", 25f, "The explosion of the attack will have a radius of this many meters.");
        FlailOfMassDamageMultiplierInit = config.ActiveBind("Item: " + ItemName, "Damage multiplier of the attack with one " + ItemName, 500f, "What % of base damage should the attack do with one " + ItemName + "? (500 = 50000%)");
        FlailOfMassDamageMultiplierStack = config.ActiveBind("Item: " + ItemName, "Damage multiplier of the attack per stack after one " + ItemName, 500f, "What % of base damage should be added to the attack per stack of " + ItemName + " after one? (500 = 50000%)");
        FlailOfMassProcCoefficient = config.ActiveBind("Item: " + ItemName, "Attack proc coefficient", 1f, "The proc coefficient of the attack and the explosion it makes.");
        FlailOfMassMomentumBuildRate = config.ActiveBind("Item: " + ItemName, "Momentum build rate", 1f, "Momentum is added every (10.15/(totalSpeed * buildRate)) seconds while sprinting, where buildRate is this config value.");
        FlailOfMassMomentumDecayRate = config.ActiveBind("Item: " + ItemName, "Momentum build rate", 1f, "Momentum is removed every 1/decayRate seconds while not sprinting, where decayRate is this config value.");
        FlailOfMassHealthAndShieldIncreaseBoost = config.ActiveBind("Item: " + ItemName, "Increase to health and shield gains", 0.5f, "How much should EXTRA health and shields be increased by? Health or shields must be increased by some source to see this in effect (0.5 = 50%).");
    }

    public class FlailOfMassIgnoreStunBehavior : CharacterBody.ItemBehavior
    {
        SetStateOnHurt component;

        bool f;
        bool hs;
        bool s;

        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                component = GetComponent<SetStateOnHurt>();

                f = component.canBeFrozen;
                hs = component.canBeHitStunned;
                s = component.canBeStunned;

                component.canBeFrozen = false;
                component.canBeHitStunned = false;
                component.canBeStunned = false;
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                if (GetComponent<SetStateOnHurt>())
                {
                    component.canBeFrozen = f;
                    component.canBeHitStunned = hs;
                    component.canBeStunned = s;
                }
            }
        }
    }
}
