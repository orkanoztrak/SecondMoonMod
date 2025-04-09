using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using UnityEngine;
using RoR2;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Equipment;

public class Blunt : Buff<Blunt>
{
    public override string Name => "Blunt";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC2/Elites/EliteBead/texBuffEliteBeadCorruptionIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(0, 0, 255, 255);

    public override bool CanStack => false;

    public override bool IsDebuff => true;

    public override bool IgnoreGrowthNectar => true;

    public override BuffDef.Flags Flags => base.Flags | BuffDef.Flags.ExcludeFromNoxiousThorns;

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamage += NoDamage;
    }

    private void NoDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (damageInfo.attacker)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                if (attackerBody.HasBuff(BuffDef))
                {
                    return;
                }
            }
        }
        orig(self, damageInfo);
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
