using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Tier1.PocketSand.PocketSand;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;

public class PocketSandDebuff : Buff<PocketSandDebuff>
{
    public override string Name => "PocketSandDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.tif").WaitForCompletion();

    public override Color BuffColor => Color.gray;

    public override bool CanStack => false;

    public override bool IsDebuff => true;

    public override bool IgnoreGrowthNectar => true;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += PocketSandEffect;
    }

    private void PocketSandEffect(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            args.moveSpeedReductionMultAdd += PocketSandReduction;
            args.attackSpeedReductionMultAdd += PocketSandReduction;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
