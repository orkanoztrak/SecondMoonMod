using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Tier1.PlasticBrick;

public class PlasticBrick : Item<PlasticBrick>
{
    public static ConfigOption<float> PlasticBrickProcChanceInit;
    public static ConfigOption<float> PlasticBrickProcChanceStack;

    public static ConfigOption<float> PlasticBrickArmorReduction;
    public static ConfigOption<float> PlasticBrickDebuffDuration;
    public override string ItemName => "Plastic Brick";

    public override string ItemLangTokenName => "PLASTIC_BRICK";

    public override string ItemPickupDesc => "Your non-critical hits have a chance to reduce armor.";

    public override string ItemFullDesc => $"<style=cIsDamage>{PlasticBrickProcChanceInit}%</style> <style=cStack>(+{PlasticBrickProcChanceStack}% per stack)</style> chance on <style=cIsDamage>non-critical hit</style> " +
        $"to reduce <style=cIsDamage>armor</style> by <style=cIsDamage>{PlasticBrickArmorReduction}</style> for <style=cIsDamage>{PlasticBrickDebuffDuration}s</style>. " +
        $"Chance increases with <style=cIsDamage>critical chance</style> to try to ensure the overall odds stay the same.";

    public override string ItemLore => "Sometimes I think I raised you too fast.\r\n\r\n" +
        "I still remember your tiny hands grasping for the plastic bricks. You wanted to build something that none of us could understand. A shape of your own dreaming. While we wanted to build something out of you.\r\n\r\n" +
        "Sometimes I feel like my ambition clouded my love for you.\r\n\r\n" +
        "I always thought about how great of a man you would become once you had grown up, and time passed so fast because of that. And now that you have grown, all I want to do is somehow make you small once more and hold you in my arms again.\r\n\r\n" +
        "Sometimes I believe I could have been a better mother.\r\n\r\n" +
        "Although there is no guideline to being one, I sure wish somebody had written one. You would tell me that you have no regrets, and that I had done the best job I could, and that you couldn't have asked for a better mother, as you have done so many times. But sometimes I believe otherwise.\r\n\r\n" +
        "Even still, one thing I am all the time is proud of you. I always have been, and always will be.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += PlasticBrickReduceArmor;
    }

    private void PlasticBrickReduceArmor(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (!damageInfo.crit && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            if (damageInfo.attacker)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                var victimBody = victim.GetComponent<CharacterBody>();
                if (attackerBody && victimBody)
                {
                    var attacker = attackerBody.master;
                    if (attacker)
                    {
                        var stackCount = GetCount(attacker);
                        if (stackCount > 0)
                        {
                            var finalChance = PlasticBrickProcChanceInit + ((stackCount - 1) * PlasticBrickProcChanceStack);
                            finalChance = attackerBody.crit < 100 ? finalChance / (100 - attackerBody.crit) * 100 : 100;
                            if (Util.CheckRoll(finalChance * damageInfo.procCoefficient, attacker))
                            {
                                victimBody.AddTimedBuffAuthority(IncomparablePain.instance.BuffDef.buffIndex, PlasticBrickDebuffDuration * damageInfo.procCoefficient);
                            }
                        }
                    }
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
        PlasticBrickArmorReduction = config.ActiveBind("Item: " + ItemName, "Armor reduction", 2f, "Armor is reduced by this per stack of the debuff.");
        PlasticBrickDebuffDuration = config.ActiveBind("Item: " + ItemName, "Debuff duration", 3f, "The debuff lasts this many seconds.");

        PlasticBrickProcChanceInit = config.ActiveBind("Item: " + ItemName, "Proc chance with one " + ItemName, 10f, "What % of non-critical hits should proc with one " + ItemName + "?");
        PlasticBrickProcChanceStack = config.ActiveBind("Item: " + ItemName, "Proc chance per stack after one " + ItemName, 10f, "What % of non-critical hits should proc per stack of " + ItemName + " after one ?");
    }
}
