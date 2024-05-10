using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier1.WantedPoster;

public class WantedPoster : Item<WantedPoster>
{
    public static ConfigOption<float> WantedPosterBossDamageInit;
    public static ConfigOption<float> WantedPosterBossDamageStack;
    public static ConfigOption<float> WantedPosterBossGoldInit;
    public static ConfigOption<float> WantedPosterBossGoldStack;

    public override string ItemName => "Wanted Poster";

    public override string ItemLangTokenName => "SECONDMOONMOD_WANTED_POSTER";

    public override string ItemPickupDesc => $"Deal extra damage to bosses and get bonus gold from killing them.";

    public override string ItemFullDesc => $"Deal an additional <style=cIsDamage>{WantedPosterBossDamageInit}%</style> damage <style=cStack>(+{WantedPosterBossDamageStack}% per stack)</style> to bosses. " +
        $"They give an additional <style=cIsUtility>{WantedPosterBossGoldInit}%</style> <style=cStack>(+{WantedPosterBossGoldStack}% per stack)</style> <style=cIsUtility>gold</style> upon death.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier1Def.asset").WaitForCompletion();
    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility, ItemTag.BrotherBlacklist];
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.DeathRewards.OnKilledServer += WantedPosterBonusBossGold;
        IL.RoR2.HealthComponent.TakeDamage += WantedPosterBonusBossDamage;
    }

    private void WantedPosterBonusBossGold(On.RoR2.DeathRewards.orig_OnKilledServer orig, DeathRewards self, DamageReport damageReport)
    {
        var attacker = damageReport.attackerBody;
        if (attacker)
        {
            var stackCount = GetCount(attacker);
            if (stackCount > 0)
            {
                if (damageReport.victimBody && damageReport.victimBody.isBoss)
                {
                    self.goldReward *= (uint)(1 + (WantedPosterBossGoldInit + ((stackCount - 1) * WantedPosterBossGoldStack)));
                }
            }
        }
        orig(self, damageReport);
    }

    private void WantedPosterBonusBossDamage(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(
            MoveType.After,
            x => x.MatchLdloc(32));
        int stackCount = 0;
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
        cursor.EmitDelegate<Func<int, DamageInfo, int>>((apCount, damageInfo) =>
        {
            stackCount = GetCount(damageInfo.attacker?.GetComponent<CharacterBody>());
            return apCount + stackCount;
        });
        cursor.GotoNext(
            MoveType.After,
            x => x.MatchLdloc(32),
            x => x.MatchConvR4(),
            x => x.MatchMul(),
            x => x.MatchAdd());
        cursor.EmitDelegate<Func<float, float>>(accumulator =>
        {
            if (stackCount > 0)
            {
                return accumulator + WantedPosterBossDamageInit + (stackCount - 1) * WantedPosterBossDamageStack;
            }
            return accumulator;
        });
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
        WantedPosterBossDamageInit = config.ActiveBind("Item: " + ItemName, "Boss damage with one " + ItemName, 0.1f, "How much should boss damage be increased by with one Wanted Poster? (0.1 = 10%)");
        WantedPosterBossDamageStack = config.ActiveBind("Item: " + ItemName, "Boss damage per stack after one " + ItemName, 0.1f, "How much should boss damage be increased by per stack of Wanted Poster after one? (0.1 = 10%)");

        WantedPosterBossGoldInit = config.ActiveBind("Item: " + ItemName, "Boss gold with one " + ItemName, 0.1f, "How much should boss gold reward on death be increased by with one Wanted Poster? (0.1 = 10%)");
        WantedPosterBossGoldStack = config.ActiveBind("Item: " + ItemName, "Boss gold per stack after one " + ItemName, 0.1f, "How much should boss gold reward on death be increased by per stack of Wanted Poster after one? (0.1 = 10%)");
    }
}
