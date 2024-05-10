using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using SecondMoon.BuffsAndDebuffs;
using UnityEngine;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using MonoMod.Cil;
using static RoR2.DotController;
using TMPro;
using SecondMoon.Utils;
using BepInEx.Configuration;
using SecondMoon.Items.Tier2.HexDagger;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Prototype.MalachiteNeedles;

public class MalachiteNeedles : Item<MalachiteNeedles>
{
    public static ConfigOption<float> MalachiteNeedlesCorrosionDmgInit;
    public static ConfigOption<float> MalachiteNeedlesCorrosionDmgStack;
    public static ConfigOption<float> MalachiteNeedlesDOTBurstConversion;

    public override string ItemName => "Malachite Needles";

    public override string ItemLangTokenName => "SECONDMOONMOD_MALACHITE_NEEDLES";

    public override string ItemPickupDesc => $"Corrode enemies on hit. Damage over time effects deal damage based on their total when applied.";

    public override string ItemFullDesc => $"Hits <color=#8aa626>corrode</color> enemies for <style=cIsDamage>{MalachiteNeedlesCorrosionDmgInit * 100}%</style> <style=cStack>(+{MalachiteNeedlesCorrosionDmgStack * 100}% per stack)</style> TOTAL damage. " +
        $"<color=#7CFDEA>Damage over time effects, in addition to their normal effects, deal damage equal to {MalachiteNeedlesDOTBurstConversion * 100}% of their total when applied</color>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

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
        var dot = (DotStack)dotStack;
        var attackerBody = dot.attackerObject.GetComponent<CharacterBody>();
        if (attackerBody)
        {
            var stackCount = GetCount(attackerBody);
            if (stackCount > 0)
            {
                DamageInfo damageInfo = new DamageInfo
                {
                    attacker = dot.attackerObject,
                    crit = false,
                    damage = (dot.damage * dot.timer / dot.dotDef.interval) * MalachiteNeedlesDOTBurstConversion,
                    force = Vector3.zero,
                    inflictor = null,
                    position = self.victimBody.corePosition,
                    procCoefficient = 0f,
                    damageColorIndex = dot.dotDef.damageColorIndex,
                    damageType = dot.damageType | DamageType.DoT
                };
                self.victimHealthComponent.TakeDamage(damageInfo);
            }
        }
    }

    private void MalachiteNeedlesApplyTotalCorrosion(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        orig(self, damageInfo, victim);
        if (damageInfo.procCoefficient > 0f)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    InflictDotInfo ınflictDotInfo = default;
                    ınflictDotInfo.victimObject = victim;
                    ınflictDotInfo.attackerObject = damageInfo.attacker;
                    ınflictDotInfo.dotIndex = Corrosion.instance.DotIndex;
                    ınflictDotInfo.damageMultiplier = 1f;
                    ınflictDotInfo.totalDamage = damageInfo.damage * (MalachiteNeedlesCorrosionDmgInit + ((stackCount - 1) * MalachiteNeedlesCorrosionDmgStack)) * damageInfo.procCoefficient;
                    InflictDot(ref ınflictDotInfo);
                }
            }
        }
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        MalachiteNeedlesCorrosionDmgInit = config.ActiveBind("Item: " + ItemName, "Corrosion multiplier with one " + ItemName, 0.35f, "What % of TOTAL damage should Corrosion do with one Malachite Needles? (0.35 = 35%)");
        MalachiteNeedlesCorrosionDmgStack = config.ActiveBind("Item: " + ItemName, "Corrosion multiplier per stack after one " + ItemName, 0.35f, "What % of TOTAL damage should be added to Corrosion per stack of Malachite Needles after one? (0.35 = 35%)");
        MalachiteNeedlesDOTBurstConversion = config.ActiveBind("Item: " + ItemName, "DOT burst damage multiplier", 1f, "What % of a damage over time effect's total damage should be dealt as additional damage when it is applied? (1 = 100%)");
    }
}
