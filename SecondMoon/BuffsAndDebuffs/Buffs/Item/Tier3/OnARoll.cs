using MonoMod.Cil;
using RoR2;
using SecondMoon.Items.Tier3.Boulderball;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Tier3.Boulderball.Boulderball;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Tier3;

public class OnARoll : Buff<OnARoll>
{
    public override string Name => "On a Roll";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ShieldOnly/texShieldBugIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(153, 146, 100, 255);

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }

    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += Rolling;
    }

    private void Rolling(ILContext il)
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
                    var buffCount = body.GetBuffCount(BuffDef);
                    if (buffCount > 0)
                    {
                        var stackCount = Boulderball.instance.GetCount(body);
                        if (stackCount > 0)
                        {
                            var increase = BoulderballDamageIncreaseInit + ((stackCount - 1) * BoulderballDamageIncreaseStack);
                            increase *= buffCount;
                            body.damage *= 1 + increase;
                        }
                    }
                }
            });
        }
    }
}
