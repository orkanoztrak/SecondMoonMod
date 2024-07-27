using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Tier1.PlasticBrick.PlasticBrick;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;

public class IncomparablePain : Buff<IncomparablePain>
{
    public override string Name => "Incomparable Pain";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();

    public override Color BuffColor => Color.red;

    public override bool IsDebuff => true;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += ReduceArmor;
        On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += Refresh;
    }

    private void Refresh(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
    {
        orig(self, buffDef, duration);
        if (buffDef.Equals(BuffDef))
        {
            foreach (var timedBuff in self.timedBuffs)
            {
                if (timedBuff.buffIndex == BuffDef.buffIndex)
                {
                    if (timedBuff.timer < duration)
                    {
                        timedBuff.timer = duration;
                    }
                }
            }
        }
    }

    private void ReduceArmor(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            var buffCount = sender.GetBuffCount(BuffDef.buffIndex);
            args.armorAdd -= PlasticBrickArmorReduction * buffCount;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
