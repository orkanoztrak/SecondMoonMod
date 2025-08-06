using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier2.HexDagger;

public class HexDagger : Item<HexDagger>
{
    public static ConfigOption<float> HexDaggerInitialCritChance;
    public static ConfigOption<float> HexDaggerCriticalDOTDamageInit;
    public static ConfigOption<float> HexDaggerCriticalDOTDamageStack;

    public override string ItemName => "Hex Dagger";

    public override string ItemLangTokenName => "HEX_DAGGER";

    public override string ItemPickupDesc => "Your damage over time effects can critically strike.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{HexDaggerInitialCritChance}% critical chance</style>. " +
        $"Each tick of a <style=cIsDamage>damage over time effect</style> can now <style=cIsDamage>critically strike. </style>" +
        $"Increase <style=cIsDamage>critical damage over time effect damage</style> by <style=cIsDamage>{HexDaggerCriticalDOTDamageInit * 100}%</style> <style=cStack>(+{HexDaggerCriticalDOTDamageStack * 100}% per stack)</style>.";

    public override string ItemLore => "A dagger juts out from the chest. Blood, so much blood. It pools everywhere, yet there is order beyond the physical to it. It is slow, as if unwilling to flow from its host.\r\n\r\n" +
        "A moment's stillness. The blood follows the moment. Then, slowly, silently, it is pulled back towards the direction of the body. All the blood that has escaped, every single drop moves back with a single-minded purpose.\r\n\r\n" +
        "The dagger glows red. As the blood flows from the ground to the body, and from the body to the dagger, the glow grows stronger. The blood turns viscous as it is forced to move against its nature. As the last drop is absorbed by the dagger, the glow dies down.\r\n\r\n" +
        "The air around the dagger blurs, pulses with the power of life, now contained within it. A single hand approaches from the darkness to grasp the hilt. It hesitates for a second, then holds the dagger tightly as if holding on for dear life.\r\n\r\n" +
        "The power now belongs to it.";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        RecalculateStatsAPI.GetStatCoefficients += HexDaggerInitBuffCrit;
        On.RoR2.HealthComponent.TakeDamageProcess += HexDaggerDOTsCanCrit;
    }

    private void HexDaggerDOTsCanCrit(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (damageInfo.attacker)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    if ((DamageType)(damageInfo.damageType & DamageType.DoT) != 0)
                    {
                        damageInfo.crit = Util.CheckRoll(attackerBody.crit, attackerBody.master);
                        if (damageInfo.crit)
                        {
                            damageInfo.damage *= 1 + HexDaggerCriticalDOTDamageInit + (stackCount - 1) * HexDaggerCriticalDOTDamageStack;
                        }
                    }
                }
            }
        }
        orig(self, damageInfo);
    }

    private void HexDaggerInitBuffCrit(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += HexDaggerInitialCritChance;
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
        HexDaggerInitialCritChance = config.ActiveBind("Item: " + ItemName, "Critical chance with at least one " + ItemName, 5f, "By what % should critical chance be increased by with at least one " + ItemName + "?");
        HexDaggerCriticalDOTDamageInit = config.ActiveBind("Item: " + ItemName, "Critical status damage with one " + ItemName, 0f, "By what % should critical status effect damage be boosted with one " + ItemName + "?");
        HexDaggerCriticalDOTDamageStack = config.ActiveBind("Item: " + ItemName, "Critical status damage per stack after one " + ItemName, 0.5f, "By what % should critical status effect damage be boosted per stack of " + ItemName + " after one? (0.5f = 50%)");
    }
}
