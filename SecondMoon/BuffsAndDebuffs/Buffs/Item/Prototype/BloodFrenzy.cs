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
        On.RoR2.CharacterBody.RecalculateStats += BloodFrenzyBuffStats;
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
