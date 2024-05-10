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
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Items.Tier2.HexDagger;

public class HexDagger : Item<HexDagger>
{
    public static ConfigOption<float> HexDaggerInitialCritChance;
    public static ConfigOption<float> HexDaggerCriticalDOTDamageInit;
    public static ConfigOption<float> HexDaggerCriticalDOTDamageStack;

    public override string ItemName => "Hex Dagger";

    public override string ItemLangTokenName => "SECONDMOODMOD_HEX_DAGGER";

    public override string ItemPickupDesc => "Your status effects can critically strike.";

    public override string ItemFullDesc => $"Gain <style=cIsDamage>{HexDaggerInitialCritChance}% critical chance</style>. " +
        $"Each tick of a <style=cIsDamage>damage over time effect</style> can now <style=cIsDamage>critically strike. </style>" +
        $"Increase <style=cIsDamage>critical damage over time effect damage</style> by <style=cIsDamage>{HexDaggerCriticalDOTDamageInit * 100}%</style> <style=cStack>(+{HexDaggerCriticalDOTDamageStack * 100}% per stack)</style>.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        GetStatCoefficients += HexDaggerInitBuffCrit;
        On.RoR2.HealthComponent.TakeDamage += HexDaggerDOTsCanCrit;
    }

    private void HexDaggerDOTsCanCrit(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        var newDamageInfo = damageInfo;
        if (newDamageInfo.attacker)
        {
            var attackerBody = newDamageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    if ((newDamageInfo.damageType & DamageType.DoT) != 0)
                    {
                        newDamageInfo.crit = Util.CheckRoll(attackerBody.crit, attackerBody.master);
                        if (newDamageInfo.crit)
                        {
                            newDamageInfo.damage *= 1 + HexDaggerCriticalDOTDamageInit + (stackCount - 1) * HexDaggerCriticalDOTDamageStack;
                        }
                    }
                }
            }
        }
        orig(self, newDamageInfo);
    }

    private void HexDaggerInitBuffCrit(CharacterBody sender, StatHookEventArgs args)
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
        HexDaggerInitialCritChance = config.ActiveBind("Item: " + ItemName, "Critical chance with at least one " + ItemName, 5f, "By what % should critical chance be increased by with at least one Hex Dagger?");
        HexDaggerCriticalDOTDamageInit = config.ActiveBind("Item: " + ItemName, "Critical status damage with one " + ItemName, 0f, "By what % should critical status effect damage be boosted with one Hex Dagger?");
        HexDaggerCriticalDOTDamageStack = config.ActiveBind("Item: " + ItemName, "Critical status damage per stack after one " + ItemName, 0.5f, "By what % should critical status effect damage be boosted per stack of Hex Dagger after one? (0.5f = 50%)");
    }
}
