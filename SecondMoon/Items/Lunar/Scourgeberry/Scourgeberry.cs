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

    public override string ItemLangTokenName => "SCOURGEBERRY";

    public override string ItemPickupDesc => "Turn your hits with proc coefficient into a damage over time effect that is stronger.";

    public override string ItemFullDesc => $"<color=#FF7F7F>Hits with proc coefficient deal <style=cIsDamage>0</style> damage. </color>" +
        $"Hits apply <color=#0000FF>Lunar Scourge</color> for <style=cIsDamage>{ScourgeberryLunarScourgeDmgInit * 100}%</style> <style=cStack>(+{ScourgeberryLunarScourgeDmgStack * 100}% per stack)</style> of the TOTAL damage they would deal over <style=cIsDamage>{ScourgeberryLunarScourgeDuration}s</style> <style=cStack>(2× per stack)</style>.";

    public override string ItemLore => "<style=cMono>FIELDTECH Image-To-Text Translator (v2.5.10b)\r\n# Awaiting input... done.\r\n# Reading image for text... done.\r\n# Transcribing data... done.\r\n# Translating text... done. [8 exceptions raised]\r\nComplete: outputting results.\r\n\r\n</style>" +
        "Down below the mountain range, when you follow the little creek that flows from the easy slopes on the east, is a valley.\r\n\r\n" +
        "The elders call it the \"Forbidden Valley\". You wouldn't get where the name comes from by the looks of it. The creek keeps flowing, slower than before thanks to the barely apparent slope, zig zagging through the fields of beautiful purple flowers as far as the eye can see.\r\n\r\n" +
        "The name comes from the small bushes of berries one can find nestled among the flower beds. Blood red berries grow from those bushes, and they do naught but bring misfortune to those who deign to taste of them. They are said to have been planted by the [Elder Brother] to bring pain to the creations of his betrayer sibling.\r\n\r\n" +
        "That's why from a young age, our tribe is taught to never go to the valley below the mountains. Yet sometimes I find myself wondering; what lies beyond the valley? What amazing things can be beyond such a place?\r\n\r\n" +
        "I have made my mind. Tomorrow night, while everyone is asleep, I will escape. I will escape and discover this world for all that it has to offer. I will see the creations of [the Brothers] with my own eyes.\r\n\r\n" +
        "<style=cMono>Translation Errors:</style>\r\n# [Elder Brother] could not be fully translated.\r\n# [The Brothers] could not be fully translated.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/LunarTierDef.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamageProcess += NoDamage;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += ScourgeberryApplyTotalLunarScourge;
    }

    private void ScourgeberryApplyTotalLunarScourge(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
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
        int damageIndex = 7;
        int flag2index = 6;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(flag2index)))
        { // done in order to skip blocks like Safer Spaces, they shouldn't proc on a 0 damage hit
            var cache = 0f;
            var cacheCheck = false;
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
            cursor.EmitDelegate<Action<DamageInfo>>((damageInfo) =>
            {
                cacheCheck = false;
                cache = damageInfo.damage;
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
                                cacheCheck = true;
                                damageInfo.damage = 0;
                            }
                        }
                    }
                }
            });
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(damageIndex)))
            { // done in order to make sure procs are correctly calculated, we need the original damageInfo to stay unchanged
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.EmitDelegate<Action<DamageInfo>>((damageInfo) =>
                {
                    if (cacheCheck)
                    {
                        damageInfo.damage = cache;
                    }
                });
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
        ScourgeberryLunarScourgeDmgInit = config.ActiveBind("Item: " + ItemName, LunarScourge.instance.AssociatedBuffName + " multiplier with one " + ItemName, 2f, "What % of TOTAL damage should " + LunarScourge.instance.AssociatedBuffName + " do with one " + ItemName + "? (2 = 200%)");
        ScourgeberryLunarScourgeDmgStack = config.ActiveBind("Item: " + ItemName, LunarScourge.instance.AssociatedBuffName + " multiplier per stack after one " + ItemName, 1f, "What % of TOTAL damage should be added to " + LunarScourge.instance.AssociatedBuffName +" per stack of " + ItemName + " after one? (1 = 100%)");
        ScourgeberryLunarScourgeDuration = config.ActiveBind("Item: " + ItemName, LunarScourge.instance.AssociatedBuffName + " duration with one " + ItemName, 3f, LunarScourge.instance.AssociatedBuffName + " lasts this many seconds with one " + ItemName + ". Each stack after doubles the duration. This should be a value divisible by " + (double)LunarScourge.instance.Interval + ".");
    }
}
