using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;

public class PocketSandDebuff : Buff<PocketSandDebuff>
{
    public static float PocketSandHealthThreshold = 0.9f;
    public static float PocketSandMovementReduction = 0.4f;
    public static float PocketSandAttackSpeedReduction = 0.4f;
    public override string Name => "PocketSandDebuff";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.tif").WaitForCompletion();

    public override Color BuffColor => Color.gray;

    public override bool CanStack => false;

    public override bool IsDebuff => true;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += PocketSandEffect;
    }

    private void PocketSandEffect(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            args.moveSpeedReductionMultAdd += PocketSandMovementReduction;
            args.attackSpeedReductionMultAdd += PocketSandAttackSpeedReduction;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
