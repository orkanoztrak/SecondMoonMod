using MonoMod.Cil;
using RoR2;
using SecondMoon.Items.Tier3.Boulderball;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Prototype.BloodInfusedCore.BloodInfusedCore;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;

public class BloodFrenzy : Buff<BloodFrenzy>
{
    public override string Name => "Blood Frenzy";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Bandit2/texBuffBanditSkullIcon.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(255, 0, 0, 255);

    public override bool CanStack => false;

    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += BloodFrenzyBuffStats;
    }

    private void BloodFrenzyBuffStats(ILContext il)
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
                        body.moveSpeed *= 1 + BloodInfusedCoreBloodFrenzyBoost;
                        body.attackSpeed *= 1 + BloodInfusedCoreBloodFrenzyBoost;
                        body.damage *= 1 + BloodInfusedCoreBloodFrenzyBoost;
                    }
                }
            });
        }
    }

    private void BloodFrenzyBuffStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, RoR2.CharacterBody self)
    {
        orig(self);
        if (self.HasBuff(BuffDef))
        {
            self.moveSpeed *= 1 + BloodInfusedCoreBloodFrenzyBoost;
            self.attackSpeed *= 1 + BloodInfusedCoreBloodFrenzyBoost;
            self.damage *= 1 + BloodInfusedCoreBloodFrenzyBoost;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
