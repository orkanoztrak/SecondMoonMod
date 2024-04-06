using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SecondMoon.Items.Void.BlissfulVisage.BlissfulVisage.BlissfulVisageSuicideComponent;

namespace SecondMoon.Items.Prototype.FlailOfMass;

public class FlailOfMass : Item<FlailOfMass>
{
    public static List<BuffIndex> FlailOfMassSlowsAndRoots = new List<BuffIndex>();
    public static string[] FlailOfMassSlowAndRootNames = ["bdEntangle", "bdNullified", "bdNullifyStack", "bdLunarSecondaryRoot", "bdSlow50", "bdSlow60", "bdSlow80", "bdClayGoo", "bdSlow30", "bdJailerSlow", "bdJailerTether"];
    public static int FlailOfMassMomentumLimit = 100;
    public static float FlailOfMassRadius = 25f;
    public static float FlailOfMassDamageMultiplier = 1000f;
    public static float FlailOfMassProcCoefficient = 1f;
    public override string ItemName => "Flail of Mass";

    public override string ItemLangTokenName => "SECONDMOON_FLAIL_OF_MASS";

    public override string ItemPickupDesc => "Become immune to stuns and slows. Consistent sprinting builds up a massive attack.";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += FlailOfMassRegisterDebuffsToIgnore;
        On.RoR2.CharacterBody.AddBuff_BuffIndex += FlailOfMassIgnoreSlowingDebuffs;
        //On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += FlailOfMassIgnoreSlowingDebuffs;
        IL.RoR2.CharacterBody.RecalculateStats += FlailOfMassIgnoreSlows;
        On.RoR2.CharacterBody.OnInventoryChanged += FlailOfMassAddIgnoreStunBehavior;
    }

    private void FlailOfMassIgnoreSlowingDebuffs(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        if (!FlailOfMassSlowsAndRoots.Contains(buffDef.buffIndex))
        {
            orig(self, buffDef, duration);
        }
    }

    [Server]
    private void FlailOfMassIgnoreSlowingDebuffs(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
    {
        if (!FlailOfMassSlowsAndRoots.Contains(buffType))
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

    private void FlailOfMassIgnoreSlows(ILContext il)
    {
        int movementTrackerIndex = 74;
        int speedTrackerIndex = 75;
        int slowTrackerIndex = 76;

        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchLdloc(movementTrackerIndex),
                        x => x.MatchLdloc(speedTrackerIndex),
                        x => x.MatchLdloc(slowTrackerIndex));
        ILLabel target = cursor.MarkLabel();

        cursor.GotoNext(x => x.MatchStloc(movementTrackerIndex),
                        x => x.MatchLdloc(59),
                        x => x.MatchLdcI4(0));
        cursor.Index++;
        ILLabel target2 = cursor.MarkLabel();

        cursor.GotoPrev(x => x.MatchLdloc(movementTrackerIndex));

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

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }

    public class FlailOfMassIgnoreStunBehavior : CharacterBody.ItemBehavior
    {
        SetStateOnHurt component;

        bool f;
        bool hs;
        bool s;

        private void OnEnable() 
        {
            component = GetComponent<SetStateOnHurt>();

            f = component.canBeFrozen;
            hs = component.canBeHitStunned;
            s = component.canBeStunned;

            component.canBeFrozen = false;
            component.canBeHitStunned = false;
            component.canBeStunned = false;
        }

        private void OnDisable()
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
