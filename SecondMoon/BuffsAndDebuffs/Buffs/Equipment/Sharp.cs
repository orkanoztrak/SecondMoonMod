using MonoMod.Cil;
using RoR2;
using SecondMoon.Equipment.SharpVinegar;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Equipment.SharpVinegar.SharpVinegar;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Equipment;

public class Sharp : Buff<Sharp>
{
    public override string Name => "Sharp";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC2/Elites/EliteBead/texBuffEliteBeadCorruptionIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(255, 0, 0, 255);

    public override bool CanStack => false;
    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += SharpVinegarModifyStats;
    }

    private void SharpVinegarModifyStats(ILContext il)
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
                    if (body.HasBuff(BuffDef))
                    {
                        body.moveSpeed *= 1 + SharpVinegarBonusMSActive;
                        body.damage *= 1 + SharpVinegarBonusDmgActive;
                    }
                }
            });
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
