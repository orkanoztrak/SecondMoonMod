using BepInEx.Configuration;
using MonoMod.Cil;
using Newtonsoft.Json.Utilities;
using R2API;
using RoR2;
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

    public static List<BuffIndex> FlailOfMassSlowsAndRoots = new List<BuffIndex>();
    public static string[] FlailOfMassSlowAndRootNames = ["bdEntangle", "bdNullified", "bdNullifyStack", "bdLunarSecondaryRoot", "bdSlow50", "bdSlow60", "bdSlow80", "bdClayGoo", "bdSlow30", "bdJailerSlow", "bdJailerTether"];
    public override string ItemName => "Flail of Mass";

    public override string ItemLangTokenName => "SECONDMOONMOD_FLAIL_OF_MASS";

    public override string ItemPickupDesc => "Become immune to stuns and slows. Consistent sprinting builds up a massive attack.";

    public override string ItemFullDesc => $"<color=#7CFDEA>Become immune to stuns and slows</color>. " +
        $"While sprinting, gain a <color=#7CFDEA>Momentum</color> buff every <style=cIsUtility>{FlailOfMassMomentumBuildRate}s</style> (gain rate increases with <style=cIsUtility>movement speed</style>). Lose a stack of this buff every <style=cIsUtility>{FlailOfMassMomentumDecayRate}s</style> while not sprinting. " +
        $"At <color=#7CFDEA>{FlailOfMassMomentumLimit}</color> stacks, using your <style=cIsUtility>Primary skill</style> fires an explosive homing projectile that deals <style=cIsDamage>{FlailOfMassDamageMultiplierInit * 100}%</style> <style=cStack>(+{FlailOfMassDamageMultiplierStack * 100}% per stack)</style> damage in a <style=cIsDamage>{FlailOfMassRadius}m</style> radius.";

    public override string ItemLore => "Test";

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
        IL.RoR2.CharacterBody.RecalculateStats += FlailOfMassIgnoreSlows;
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

    [Server]
    private void FlailOfMassIgnoreSlowingDebuffs(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
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

    [Server]
    private void FlailOfMassAddIgnoreStunBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        if (self.healthComponent)
        {
            if (self.healthComponent.GetComponent<SetStateOnHurt>())
            {
                self.AddItemBehavior<FlailOfMassIgnoreStunBehavior>(self.inventory.GetItemCount(instance.ItemDef));
            }
        }
        orig(self);
    }

    private void FlailOfMassIgnoreSlows(ILContext il)
    {
        int movementTrackerIndex = 74;
        int speedTrackerIndex = 75;
        int slowTrackerIndex = 76;

        var cursor = new ILCursor(il);

        if (cursor.TryGotoNext(x => x.MatchLdloc(movementTrackerIndex),
                        x => x.MatchLdloc(speedTrackerIndex),
                        x => x.MatchLdloc(slowTrackerIndex)))
        {
            ILLabel target = cursor.MarkLabel();

            if (cursor.TryGotoNext(x => x.MatchStloc(movementTrackerIndex),
                            x => x.MatchLdloc(59),
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
        FlailOfMassMomentumLimit = config.ActiveBind("Item: " + ItemName, "Momentum stacks necessary to access the attack", 100, "At this number of Momentum stacks, they will not decay until the attack is fired.");
        FlailOfMassRadius = config.ActiveBind("Item: " + ItemName, "Attack explosion radius", 25f, "The explosion of the attack will have a radius of this many meters.");
        FlailOfMassDamageMultiplierInit = config.ActiveBind("Item: " + ItemName, "Damage multiplier of the attack with one " + ItemName, 150f, "What % of base damage should the attack do with one " + ItemName + "? (150 = 15000%)");
        FlailOfMassDamageMultiplierStack = config.ActiveBind("Item: " + ItemName, "Damage multiplier of the attack per stack after one " + ItemName, 150f, "What % of base damage should be added to the attack per stack of " + ItemName + " after one? (150 = 15000%)");
        FlailOfMassProcCoefficient = config.ActiveBind("Item: " + ItemName, "Attack proc coefficient", 1f, "The proc coefficient of the attack and the explosion it makes.");
        FlailOfMassMomentumBuildRate = config.ActiveBind("Item: " + ItemName, "Momentum build rate", 1f, "Momentum is added every (10.15/(totalSpeed * buildRate)) seconds while sprinting, where buildRate is this config value.");
        FlailOfMassMomentumDecayRate = config.ActiveBind("Item: " + ItemName, "Momentum build rate", 1f, "Momentum is removed every 1/decayRate seconds while not sprinting, where decayRate is this config value.");
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
