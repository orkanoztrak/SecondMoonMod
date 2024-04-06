using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Equipment;

public class Channeling : Buff<Channeling>
{
    public static float ChannelingHealthBoost = 0.3f;
    public static float ChannelingRegenBoost = 0.3f;
    public static float ChannelingMovementBoost = 0.3f;
    public static float ChannelingDamageBoost = 0.3f;
    public static float ChannelingAttackSpeedBoost = 0.3f;
    public static float ChannelingCritBoost = 0.3f;
    public static float ChannelingArmorBoost = 0.3f;

    public override string Name => "Channeling";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/EliteLunar/texAffixLunarIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color(255, 223, 135, 1);

    public override bool CanStack => false;

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += ChannelingEffect;
    }

    private void ChannelingEffect(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        if (sender.HasBuff(BuffDef))
        {
            args.healthMultAdd += ChannelingHealthBoost;
            args.baseRegenAdd += ChannelingRegenBoost;
            args.levelRegenAdd += ChannelingRegenBoost / 5;
            args.moveSpeedMultAdd += ChannelingMovementBoost;
            args.damageMultAdd += ChannelingDamageBoost;
            args.attackSpeedMultAdd += ChannelingAttackSpeedBoost;
            args.critAdd += ChannelingCritBoost;
            args.armorAdd += ChannelingArmorBoost;
        }
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
