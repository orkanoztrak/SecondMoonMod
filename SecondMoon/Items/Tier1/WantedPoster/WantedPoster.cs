using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Tier1.WantedPoster;

internal class WantedPoster : Item<WantedPoster>
{
    public static float WantedPosterBossDamageInit = 0.1f;
    public static float WantedPosterBossDamageStack = 0.1f;
    public static float WantedPosterBossGoldInit = 0.1f;
    public static float WantedPosterBossGoldStack = 0.1f;

    public override string ItemName => "Wanted Poster";

    public override string ItemLangTokenName => "WANTED_POSTER";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier1;
    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Utility];
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

    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }
}
