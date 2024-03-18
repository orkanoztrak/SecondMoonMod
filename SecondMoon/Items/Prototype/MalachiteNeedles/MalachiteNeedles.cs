using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using SecondMoon.BuffsAndDebuffs;
using UnityEngine;
using System.Diagnostics;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using MonoMod.Cil;
using static RoR2.DotController;
using TMPro;

namespace SecondMoon.Items.Prototype.MalachiteNeedles;

public class MalachiteNeedles : Item<MalachiteNeedles>
{
    public static float MalachiteNeedlesCorrosionDmgInit = .5f;
    public static float MalachiteNeedlesCorrosionDmgStack = .25f;
    public static float MalachiteNeedlesCorrosionDuration = 2f;

    public override string ItemName => "Malachite Needles";

    public override string ItemLangTokenName => "MALACHITE_NEEDLES";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

    public override ItemTag[] Category => [ItemTag.Damage];
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnHitEnemy += MalachiteNeedlesApplyTotalCorrosion;
        On.RoR2.DotController.OnDotStackAddedServer += MalachiteNeedlesDOTBurst;
    }

    private void MalachiteNeedlesDOTBurst(On.RoR2.DotController.orig_OnDotStackAddedServer orig, DotController self, object dotStack)
    {
        orig(self, dotStack);
        var dot = (DotController.DotStack)dotStack;
        var attackerBody = dot.attackerObject.GetComponent<CharacterBody>();
        if (attackerBody)
        {
            var stackCount = GetCount(attackerBody);
            if (stackCount > 0)
            {
                DamageInfo damageInfo = new DamageInfo();
                damageInfo.attacker = dot.attackerObject;
                damageInfo.crit = false;
                damageInfo.damage = dot.damage * dot.timer / dot.dotDef.interval;
                damageInfo.force = Vector3.zero;
                damageInfo.inflictor = null;
                damageInfo.position = self.victimBody.corePosition;
                damageInfo.procCoefficient = 0f;
                damageInfo.damageColorIndex = dot.dotDef.damageColorIndex;
                damageInfo.damageType = dot.damageType | DamageType.DoT;
                self.victimHealthComponent.TakeDamage(damageInfo);
            }
        }
    }

    private void MalachiteNeedlesApplyTotalCorrosion(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
        if (attackerBody)
        {
            var stackCount = GetCount(attackerBody);
            if (stackCount > 0)
            {
                InflictDotInfo ınflictDotInfo = default(InflictDotInfo);
                ınflictDotInfo.victimObject = victim;
                ınflictDotInfo.attackerObject = damageInfo.attacker;
                ınflictDotInfo.dotIndex = Corrosion.instance.DotIndex;
                ınflictDotInfo.duration = MalachiteNeedlesCorrosionDuration;
                ınflictDotInfo.damageMultiplier = damageInfo.damage / attackerBody.damage * (MalachiteNeedlesCorrosionDmgInit + ((stackCount - 1) * MalachiteNeedlesCorrosionDmgStack));
                DotController.InflictDot(ref ınflictDotInfo);
            }
        }
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
