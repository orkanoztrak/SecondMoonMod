using MonoMod.Cil;
using RoR2;
using SecondMoon.Items.Tier2.HexDagger;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RoR2.DotController;
using static SecondMoon.Items.Void.Popperbloom.Popperbloom;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Void;

public class Popperseeds : DOT<Popperseeds>
{
    public override float Interval => 0.75f;

    public override float DamageCoefficient => 0.25f;

    public override DamageColorIndex DamageColorIndex => DamageColorIndex.Void;

    public override string AssociatedBuffName => "PopperseedsDebuff";

    public override void Hooks()
    {
        IL.RoR2.DotController.EvaluateDotStacksForType += Pop;
    }

    private void Pop(ILContext il)
    {
        bool done = false;
        var cursor = new ILCursor(il);
        ILLabel target = null;

        if (cursor.TryGotoNext(MoveType.After,
        x => x.MatchCallOrCallvirt<DotController>("AddPendingDamageEntry")))
        {
            target = cursor.MarkLabel();
        }

        if (cursor.TryGotoPrev(MoveType.After,
            x => x.MatchStfld<DotStack>("timer")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc_3);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<DotStack, DotController, bool>>((dotStack, dotController) =>
            {
                done = false;
                if (dotStack.dotIndex == instance.DotIndex)
                {
                    EffectManager.SpawnEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ExplodeOnDeathVoid/ExplodeOnDeathVoidExplosionEffect.prefab").WaitForCompletion(), new EffectData
                    {
                        origin = dotController.victimObject.transform.position,
                        scale = PopperseedsRadius,
                    }, true);

                    BlastAttack pop = new BlastAttack
                    {
                        procCoefficient = 0,
                        attacker = dotStack.attackerObject,
                        teamIndex = dotStack.attackerTeam,
                        baseDamage = dotStack.damage,
                        baseForce = 0,
                        falloffModel = BlastAttack.FalloffModel.None,
                        crit = false,
                        radius = PopperseedsRadius,
                        position = dotController.victimObject.transform.position,
                        damageType = DamageType.DoT | DamageType.AOE,
                        damageColorIndex = DamageColorIndex.Void,
                        attackerFiltering = AttackerFiltering.NeverHitSelf
                    };
                    pop.Fire();
                    done = true;
                }
                return done;
            });
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target);
        }
    }

    public override void Init()
    {
        SetAssociatedBuff();
        CreateDOT();
        Hooks();
    }

    public override void SetAssociatedBuff()
    {
        AssociatedBuff = PopperseedsDebuff.instance.BuffDef;
    }
}
