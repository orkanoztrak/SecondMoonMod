using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Tier1.SandPouch.SandPouch;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;

public class SandPouchDebuff : Buff<SandPouchDebuff>
{
    public override string Name => "SandPouchDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.tif").WaitForCompletion();

    public override Color BuffColor => Color.gray;

    public override bool CanStack => false;

    public override bool IsDebuff => true;

    public override bool IgnoreGrowthNectar => true;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += SandPouchEffect;
    }

    private void SandPouchEffect(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            args.moveSpeedReductionMultAdd += SandPouchReduction;
            args.attackSpeedReductionMultAdd += SandPouchReduction;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
