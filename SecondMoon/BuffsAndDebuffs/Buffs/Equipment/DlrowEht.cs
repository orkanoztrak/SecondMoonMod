using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Equipment;

public class DlrowEht : Buff<DlrowEht>
{
    public override string Name => "Stopped Time";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/UI/texCrownIcon.png").WaitForCompletion();

    public override Color BuffColor => new Color32(43, 80, 200, 255);

    public override bool CanStack => false;

    public override void Hooks()
    {
        On.RoR2.HealthComponent.TakeDamageProcess += GodMode;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += GodMode;
        On.RoR2.Skills.SkillDef.OnExecute += NoCooldowns;
    }

    private void GodMode(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        var body = victim.GetComponent<CharacterBody>();
        if (body)
        {
            if (body.HasBuff(BuffDef))
            {
                return;
            }
        }
        orig(self, damageInfo, victim);
    }

    private void GodMode(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (self.body)
        {
            if (self.body.HasBuff(BuffDef))
            {
                return;
            }
        }
        orig(self, damageInfo);
    }

    private void NoCooldowns(On.RoR2.Skills.SkillDef.orig_OnExecute orig, RoR2.Skills.SkillDef self, GenericSkill skillSlot)
    {
        orig(self, skillSlot);
        var body = skillSlot.characterBody;
        if (body)
        {
            if (body.HasBuff(BuffDef))
            {
                skillSlot.stock += self.stockToConsume;
                skillSlot.rechargeStopwatch = 0;
            }
        }
    }
    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
