using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Void;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.DotController;

namespace SecondMoon.Items.Void.Popperbloom;

public class Popperbloom : Item<Popperbloom>
{
    public static ConfigOption<float> PopperseedsChanceInit;
    public static ConfigOption<float> PopperseedsChanceStack;
    public static ConfigOption<float> PopperseedsHealthDmgConversion;
    public static ConfigOption<float> PopperseedsRadius;
    public static ConfigOption<float> PopperseedsDuration;

    public override string ItemName => "Popperbloom";

    public override string ItemLangTokenName => "STICKYBOMBVOID";

    public override string ItemPickupDesc => "Chance on hit to apply an exploding status effect that is strengthened by health bonuses. <style=cIsVoid>Corrupts all Sticky Bombs</style>.";

    public override string ItemFullDesc => $"Hits have a <style=cIsDamage>{PopperseedsChanceInit}%</style> <style=cStack>(+{PopperseedsChanceStack}% per stack)</style> chance to apply <color=#FF0064>Popperseeds</color> " +
        $"for <style=cIsDamage>{Popperseeds.instance.DamageCoefficient * PopperseedsDuration / Popperseeds.instance.Interval * 100}%</style> <style=cIsHealing>(+{Popperseeds.instance.DamageCoefficient * PopperseedsDuration / Popperseeds.instance.Interval * PopperseedsHealthDmgConversion * 100}% per 1 bonus health, excluding by leveling)</style> TOTAL damage. " +
        $"<color=#FF0064>Popperseeds</color> ticks deal their damage in a <style=cIsDamage>{PopperseedsRadius}m</style> explosion. " +
        $"<style=cIsVoid>Corrupts all Sticky Bombs</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/DLC1/Common/VoidTier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDef ItemToCorrupt => Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/StickyBomb/StickyBomb.asset").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += PopperbloomApplyTotalPopperseeds;
    }

    private void PopperbloomApplyTotalPopperseeds(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0 && Util.CheckRoll((PopperseedsChanceInit + ((stackCount - 1) * PopperseedsChanceStack)) * damageInfo.procCoefficient, attackerBody.master))
                {
                    var healthBonus = attackerBody.maxHealth - (attackerBody.baseMaxHealth + ((attackerBody.level - 1) * attackerBody.levelMaxHealth));
                    healthBonus = healthBonus > 0 ? healthBonus : 0;
                    var mul = 1 + (healthBonus * PopperseedsHealthDmgConversion);
                    var thresholdTotal = 1f;
                    var tickDamage = Popperseeds.instance.DamageCoefficient * damageInfo.damage * mul;

                    InflictDotInfo ınflictDotInfo = default;
                    ınflictDotInfo.victimObject = victim;
                    ınflictDotInfo.attackerObject = damageInfo.attacker;
                    ınflictDotInfo.dotIndex = Popperseeds.instance.DotIndex;
                    if (thresholdTotal < tickDamage)
                    {
                        ınflictDotInfo.damageMultiplier = damageInfo.damage / attackerBody.damage * mul;
                        ınflictDotInfo.duration = PopperseedsDuration;
                    }
                    else
                    {
                        ınflictDotInfo.damageMultiplier = 1f;
                        ınflictDotInfo.totalDamage = damageInfo.damage * (Popperseeds.instance.DamageCoefficient * PopperseedsDuration / Popperseeds.instance.Interval) * mul;
                    }
                    InflictDot(ref ınflictDotInfo);
                }
            }
        }
        orig(self, damageInfo, victim);
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
        PopperseedsChanceInit = config.ActiveBind("Item: " + ItemName, "Proc chance with one " + ItemName, 10f, "The % chance of hits applying Popperseeds with one " + ItemName + ".");
        PopperseedsChanceStack = config.ActiveBind("Item: " + ItemName, "Proc chance per stack after one " + ItemName, 10f, "The % chance of hits applying Popperseeds per stack of " + ItemName + " after one.");
        PopperseedsHealthDmgConversion = config.ActiveBind("Item: " + ItemName, "Popperseeds damage boost with health bonuses", 0.004f, "By what % should Popperseeds' damage be increased by with 1 bonus health? (0.004 = 0.4%, meaning Popperseeds damage becomes 1.04x)");
        PopperseedsRadius = config.ActiveBind("Item: " + ItemName, "Popperseeds tick explosion radius", 12f, "The explosion of Popperseeds ticks will have a radius of this many meters.");
        PopperseedsDuration = config.ActiveBind("Item: " + ItemName, "Popperseeds duration", 2.25f, "Popperseeds ticks every 0.75s. By default, it ticks 3 times total (0.75 x 3 = 2.25).");
    }
}
