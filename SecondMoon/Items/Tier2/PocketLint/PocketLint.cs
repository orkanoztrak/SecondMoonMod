using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Tier2.PocketLint;

public class PocketLint : Item<PocketLint>
{
    public static ConfigOption<float> PocketLintLintyBonusGoldInit;
    public static ConfigOption<float> PocketLintLintyBonusGoldStack;
    public override string ItemName => "Pocket Lint";

    public override string ItemLangTokenName => "SECONDMOONMOD_POCKET_LINT";

    public override string ItemPickupDesc => "Debuffing enemies increases the gold they drop upon death.";

    public override string ItemFullDesc => $"Hits permanently apply the " + Linty.instance.Name + $" debuff, increasing <style=cIsUtility>gold</style> upon death by <style=cIsUtility>{PocketLintLintyBonusGoldInit * 100}%</style> </style=cStack>({PocketLintLintyBonusGoldStack * 100}% per stack)</style> " +
        $"per debuff on the victim.";

    public override string ItemLore => $"Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.OnKillEffect];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnHitEnemy += PocketLintApplyDebuff;
    }

    private void PocketLintApplyDebuff(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimBody = victim.GetComponent<CharacterBody>();
            if (attackerBody && victimBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    victimBody.AddBuff(Linty.instance.BuffDef);
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
        PocketLintLintyBonusGoldInit = config.ActiveBind("Item: " + ItemName, "Bonus gold per debuff with one " + ItemName, 0.25f, "How much should gold on kill be increased by per debuff on the victim with one " + ItemName + "? (0.25 = 25%)");
        PocketLintLintyBonusGoldStack = config.ActiveBind("Item: " + ItemName, "Bonus gold per debuff per stack after one " + ItemName, 0.25f, "How much should gold on kill be increased by per debuff on the victim per stack of " + ItemName + " after one? (0.25 = 25%)");
    }
}
