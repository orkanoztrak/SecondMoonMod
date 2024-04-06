using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier1.PocketSand;

internal class PocketSand : Item<PocketSand>
{
    public static float PocketSandHealthThreshold = 0.9f;
    public static float PocketSandMovementReduction = 0.45f;
    public static float PocketSandAttackSpeedReduction = 0.45f;
    public static float PocketSandTimerInit = 3f;
    public static float PocketSandTimerStack = 2f;
    public override string ItemName => "Pocket Sand";

    public override string ItemLangTokenName => "SECONDMOONMOD_POCKET_SAND";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier1;

    public override ItemTag[] Category => [ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamage += ThrowPocketSand;
    }

    private void ThrowPocketSand(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchStloc(19));
        cursor.Index -= 4;
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_0); //attacker(master)
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1); //damageInfo
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0); //HealthComponent(victim)
        cursor.EmitDelegate<Action<CharacterMaster, DamageInfo, HealthComponent>>((attacker, damageInfo, healthComponent) => 
        {
            var stackCount = GetCount(attacker);
            if (stackCount > 0)
            {
                var victim = healthComponent.GetComponent<CharacterBody>();
                victim.AddTimedBuffAuthority(PocketSandDebuff.instance.BuffDef.buffIndex, PocketSandTimerInit + ((stackCount - 1) * PocketSandTimerStack));
            }
        });
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
