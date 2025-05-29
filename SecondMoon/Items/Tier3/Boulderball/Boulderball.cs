using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Tier3;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier3.Boulderball;

public class Boulderball : Item<Boulderball>
{
    public static ConfigOption<float> BoulderballDamageIncreaseInit;
    public static ConfigOption<float> BoulderballDamageIncreaseStack;

    public override string ItemName => "Boulderball";

    public override string ItemLangTokenName => "BOULDERBALL";

    public override string ItemPickupDesc => "Kills grant a damage buff for the remainder of the stage.";

    public override string ItemFullDesc => $"Each kill grants a buff that increases <style=cIsDamage>damage</style> by <style=cIsDamage>{BoulderballDamageIncreaseInit * 100}%</style> <style=cStack>(+{BoulderballDamageIncreaseStack * 100}% per stack)</style> for the remainder of the stage.";

    public override string ItemLore => "\"It's just like life. One wrong step, and it starts to fall. Fail to stop it, and it becomes even bigger, causing more pain and suffering. It's easy to get caught in this spiral, thinking it's all pitch black and hopeless. That you have nowhere to go, nothing to do, nobody to fall back on. You mustn't make that mistake. It's exactly at that time that you must grab life by the throat, and come back even stronger than before. Never give up. Sometimes, the biggest chances come from the biggest downfalls.\"\r\n\r\n\r\n" +
        "\"Jeremy, it's just a rock. What the hell are you talking about?\"";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.OnKillEffect];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnCharacterDeath += BoulderballGrantBuffStack;
    }

    private void BoulderballGrantBuffStack(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
    {
        if (damageReport != null)
        {
            if (damageReport.attackerBody)
            {
                var stackCount = GetCount(damageReport.attackerBody);
                if (stackCount > 0)
                {
                    damageReport.attackerBody.AddBuff(OnARoll.instance.BuffDef);
                }
            }
        }
        orig(self, damageReport);
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
        BoulderballDamageIncreaseInit = config.ActiveBind("Item: " + ItemName, "Damage per kill with one " + ItemName, 0.02f, "How much should damage be increased by per kill with one " + ItemName + "? (0.02 = 2%)");
        BoulderballDamageIncreaseStack = config.ActiveBind("Item: " + ItemName, "Damage per kill per stack after one " + ItemName, 0.02f, "How much should damage be increased by per stack of " + ItemName + " after one? (0.02 = 2%)");
    }
}
