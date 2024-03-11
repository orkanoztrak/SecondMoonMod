using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
namespace SecondMoon.Items.Prototype.CelestineAuger;
public class CelestineAuger : Item<CelestineAuger>
{
    public static float CelestineAugerCriticalChance = .5f;
    public static float CelestineAugerCriticalChanceStack = .5f;
    public static float CelestineAugerBaseDamage = .5f;
    public static float CelestineAugerBaseDamageStack = .5f;
    public static float CelestineAugerInitialCritChance = 5f;

    public override string ItemName => "Celestine Auger";

    public override string ItemLangTokenName => "CELESTINE_AUGER";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier3;

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        GetStatCoefficients += CelestineAugerInitBuffCrit;
        On.RoR2.CharacterBody.RecalculateStats += CelestineAugerBuffCCDmg;
        IL.RoR2.BulletAttack.FireSingle += CelestineAugerIgnoreCollisionBullet;
    }

    private void CelestineAugerIgnoreCollisionBullet(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(
            x => x.MatchLdnull(),
            x => x.MatchStloc(4));
        cursor.Index += 1;
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<BulletAttack>>((attack) =>
        {
            if (attack.owner)
            {
                var attacker = attack.owner.GetComponent<CharacterBody>();
                if (attacker)
                {
                    var stackCount = GetCount(attacker);
                    if (stackCount > 0)
                    {
                        attack.hitMask = (int)LayerIndex.entityPrecise.mask;
                    }
                }
            }
        });
    }
    public override void Init()
    {
        CreateLang();
        CreateItem();
        Hooks();
    }

    public void CelestineAugerInitBuffCrit(CharacterBody sender, StatHookEventArgs args)
    {
        var stackCount = GetCount(sender);
        if (stackCount > 0)
        {
            args.critAdd += CelestineAugerInitialCritChance;
        }
    }

    public void CelestineAugerBuffCCDmg(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
    {
        orig(self);
        var stackCount = GetCount(self);
        if (stackCount > 0)
        {
            self.crit *= 1 + (CelestineAugerCriticalChance + (stackCount - 1) * CelestineAugerCriticalChanceStack);
            self.damage *= 1 + (CelestineAugerBaseDamage + (stackCount - 1) * CelestineAugerBaseDamageStack);
        }
    }
}
