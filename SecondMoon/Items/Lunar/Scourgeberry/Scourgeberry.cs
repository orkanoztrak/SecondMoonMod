using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Lunar;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Prototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.DotController;

namespace SecondMoon.Items.Lunar.Scourgeberry;

public class Scourgeberry : Item<Scourgeberry>
{
    public static ConfigOption<float> ScourgeberryLunarScourgeDmgInit;
    public static ConfigOption<float> ScourgeberryLunarScourgeDmgStack;
    public static ConfigOption<float> ScourgeberryLunarScourgeDuration;
    public override string ItemName => "Scourgeberry";

    public override string ItemLangTokenName => "SECONDMOONMOD_SCOURGEBERRY";

    public override string ItemPickupDesc => "Turn your hits with proc coefficient into a damage over time effect that is stronger.";

    public override string ItemFullDesc => $"<color=#FF7F7F>Hits with proc coefficient deal <style=cIsDamage>0</style> damage. </color>" +
        $"Hits apply <color=#0000FF>Lunar Scourge</color> for <style=cIsDamage>{ScourgeberryLunarScourgeDmgInit * 100}%</style> <style=cStack>(+{ScourgeberryLunarScourgeDmgStack * 100}% per stack)</style> of the TOTAL damage they would deal over <style=cIsDamage>{ScourgeberryLunarScourgeDuration}s</style> <style=cStack>(2× per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamage += NoDamage;
        On.RoR2.GlobalEventManager.OnHitEnemy += ScourgeberryApplyTotalLunarScourge;
    }

    private void ScourgeberryApplyTotalLunarScourge(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
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
                    ınflictDotInfo.dotIndex = LunarScourge.instance.DotIndex;

                    var thresholdTotal = 1f;
                    var duration = ScourgeberryLunarScourgeDuration * (float)Math.Pow(2, stackCount - 1);
                    var newCoefficient = (ScourgeberryLunarScourgeDmgInit + ((stackCount - 1) * ScourgeberryLunarScourgeDmgStack)) / (duration / LunarScourge.instance.Interval);
                    var tickDamage = damageInfo.damage * newCoefficient;
                    if (thresholdTotal < tickDamage)
                    {
                        ınflictDotInfo.damageMultiplier = (damageInfo.damage / attackerBody.damage) * (newCoefficient / LunarScourge.instance.DamageCoefficient);
                        ınflictDotInfo.duration = duration;
                    }
                    else
                    {
                        ınflictDotInfo.damageMultiplier = 1f;
                        ınflictDotInfo.totalDamage = damageInfo.damage * (ScourgeberryLunarScourgeDmgInit + ((stackCount - 1) * ScourgeberryLunarScourgeDmgStack));
                    }
                    InflictDot(ref ınflictDotInfo);
                }
            }
        }
        orig(self, damageInfo, victim);
    }

    private void NoDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(42)))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, 6);
            cursor.EmitDelegate<Func<DamageInfo, float, float>>((damageInfo, damage) =>
            {
                if (damageInfo.procCoefficient > 0)
                {
                    var attacker = damageInfo.attacker;
                    if (attacker)
                    {
                        var attackerBody = attacker.gameObject.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            var stackCount = GetCount(attackerBody);
                            if (stackCount > 0)
                            {
                                return 0;
                            }
                        }
                    }
                }
                return damage;
            });
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, 6);
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
        ScourgeberryLunarScourgeDmgInit = config.ActiveBind("Item: " + ItemName, "Lunar Scourge multiplier with one " + ItemName, 2f, "What % of TOTAL damage should Lunar Scourge do with one " + ItemName + "? (2 = 200%)");
        ScourgeberryLunarScourgeDmgStack = config.ActiveBind("Item: " + ItemName, "Lunar Scourge multiplier per stack after one " + ItemName, 1f, "What % of TOTAL damage should be added to Lunar Scourge per stack of " + ItemName + " after one? (1 = 100%)");
        ScourgeberryLunarScourgeDuration = config.ActiveBind("Item: " + ItemName, "Lunar Scourge duration with one " + ItemName, 3f, "Lunar Scourge lasts this many seconds with one " + ItemName + ". Each stack after doubles the duration. This should be a value divisible by " + (double)LunarScourge.instance.Interval + ".");
    }
}
