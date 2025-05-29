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

    public override string ItemLangTokenName => "POCKET_LINT";

    public override string ItemPickupDesc => "Debuffing enemies increases the gold they drop upon death.";

    public override string ItemFullDesc => $"Hits permanently apply the " + Linty.instance.Name + $" debuff, increasing <style=cIsUtility>gold</style> upon death by <style=cIsUtility>{PocketLintLintyBonusGoldInit * 100}%</style> </style=cStack>({PocketLintLintyBonusGoldStack * 100}% per stack)</style> " +
        $"per debuff on the victim.";

    public override string ItemLore => $"\"Hey man, got some change on ya?\"\r\n\r\n" +
        $"\"Nah, I gave my last to Frank at the docks.\"\r\n\r\n" +
        $"\"Which Frank? And which docks, dude? The docks are, like, at the other side of the city.\"\r\n\r\n" +
        $"\"You know, Frank from two blocks down. He used to get on the school bus with us during high school. And I don't mean the SEA docks, I meant the transplanetary travel center.\"\r\n\r\n" +
        $"\"Why would you call that 'the docks'?\"\r\n\r\n" +
        $"\"Well aren't they technically docks?\"\r\n\r\n" +
        $"\"No? I would call it an airport at best.\"\r\n\r\n" +
        $"\"But spaceships 'dock' at hangars.\"\r\n\r\n" +
        $"\"That's- true...\"\r\n\r\n" +
        $"\"Right?\"\r\n\r\n" +
        $"----\r\n\r\n" +
        $"\"Oh! I found some change! Do you want it?";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.OnKillEffect];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += PocketLintApplyDebuff;
    }

    private void PocketLintApplyDebuff(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
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
