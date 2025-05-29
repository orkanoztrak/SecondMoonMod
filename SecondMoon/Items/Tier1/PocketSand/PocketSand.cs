using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier1;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier1.PocketSand;

public class PocketSand : Item<PocketSand>
{
    public static ConfigOption<float> PocketSandHealthThreshold;
    public static ConfigOption<float> PocketSandReduction;
    public static ConfigOption<float> PocketSandTimerInit;
    public static ConfigOption<float> PocketSandTimerStack;

    public override string ItemName => "Pocket Sand";

    public override string ItemLangTokenName => "POCKET_SAND";

    public override string ItemPickupDesc => $"Slow down enemies above {PocketSandHealthThreshold * 100}% health and reduce their attack speed.";

    public override string ItemFullDesc => $"Enemies above <style=cIsUtility>{PocketSandHealthThreshold * 100}% health</style> have their <style=cIsUtility>movement speed</style> and <style=cIsDamage>attack speed</style> " +
        $"reduced by <style=cIsUtility>{PocketSandReduction * 100}</style> for <style=cIsUtility>{PocketSandTimerInit}s</style> <style=cStack>(+{PocketSandTimerStack}s per stack)</style>.";

    public override string ItemLore => "<style=cMono>Welcome to DataScraper (v3.1.53 – beta branch)\r\n$ Scraping memory... done.\r\n$ Resolving... done.\r\n$ Combing for relevant data... done.\r\nComplete!\r\nOutputting local audio transcriptions...\r\n\r\n</style>" +
        "Hey, ma!\r\n\r\n" +
        "Today we watched a documentary in history class... about ancient warriors in Rome.\r\n\r\n" +
        "Gladiators, I think they called them.\r\n\r\n" +
        "It was so cool! These guys would live their whole lives fighting each other and beasts like lions and bears!! I saw a guy throw sand at another's face and then beat him!\r\n\r\n" +
        "It's also kinda scary though. Y'know. When you don't win?\r\n\r\n" +
        "Anyway, history is so cool! I think I want to be a historian when I grow up!\r\n\r\n" +
        "Can we go to the museum this weekend, ma? Please?\r\n\r\n" +
        "Talk to you when I get home!\r\n\r\n" +
        "Love ya!";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamageProcess += ThrowPocketSand;
    }

    private void ThrowPocketSand(ILContext il)
    {
        var combinedHealthIndex = 4;
        var characterMasterIndex = 1;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdloc(combinedHealthIndex),
                               x => x.MatchLdarg(0),
                               x => x.MatchCallOrCallvirt<HealthComponent>("get_fullCombinedHealth"),
                               x => x.MatchLdcR4(0.9f)))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, combinedHealthIndex);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, characterMasterIndex);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<float, CharacterMaster, DamageInfo, HealthComponent>>((combinedHealth, attacker, damageInfo, healthComponent) =>
            {
                if (combinedHealth >= healthComponent.fullCombinedHealth * PocketSandHealthThreshold)
                {
                    var stackCount = GetCount(attacker);
                    if (stackCount > 0)
                    {
                        var victim = healthComponent.GetComponent<CharacterBody>();
                        victim.AddTimedBuffAuthority(PocketSandDebuff.instance.BuffDef.buffIndex, PocketSandTimerInit + ((stackCount - 1) * PocketSandTimerStack));
                    }
                }
            });
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
        PocketSandHealthThreshold = config.ActiveBind("Item: " + ItemName, "Health threshold for debuff", 0.9f, "Above what % of target health should hits apply the debuff? (0.9f = 90%)");
        PocketSandReduction = config.ActiveBind("Item: " + ItemName, "Amount of movement and attack speed reduction", 0.45f, "By what % should target attack and movement speeds be reduced from the debuff? (0.45 = 45%)");
        PocketSandTimerInit = config.ActiveBind("Item: " + ItemName, "Debuff timer with one " + ItemName, 3f, "How many seconds should the debuff last with one " + ItemName + "?");
        PocketSandTimerStack = config.ActiveBind("Item: " + ItemName, "Debuff timer per stack after one " + ItemName, 2f, "How many seconds should the debuff be extended by per stack of " + ItemName + " after one?");
    }
}
