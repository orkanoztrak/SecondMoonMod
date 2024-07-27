using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Equipment.EssenceChanneler.EssenceChanneler;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Equipment;

public class Channeling : Buff<Channeling>
{
    public override string Name => "Channeling";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteLunar/texBuffAffixLunar.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(255, 223, 135, 255);
    public override bool CanStack => false;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += ChannelingEffect;
    }

    private void ChannelingEffect(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            args.healthMultAdd += ChannelingBoost;
            args.regenMultAdd += ChannelingBoost;
            args.moveSpeedMultAdd += ChannelingBoost;
            args.damageMultAdd += ChannelingBoost;
            args.attackSpeedMultAdd += ChannelingBoost;
            args.critAdd += ChannelingBoost;
            args.armorAdd += ChannelingBoost;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
