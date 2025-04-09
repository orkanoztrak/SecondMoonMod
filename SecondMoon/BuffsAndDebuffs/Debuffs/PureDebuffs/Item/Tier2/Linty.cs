using R2API;
using RoR2;
using SecondMoon.Items.Tier2.PocketLint;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Tier2.PocketLint.PocketLint;
namespace SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Item.Tier2;

public class Linty : Buff<Linty>
{
    public override string Name => "Linty";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/ImmuneToDebuff/texBuffImmuneToDebuffConsumed.tif").WaitForCompletion();

    public override Color BuffColor => Color.yellow;

    public override bool CanStack => false;

    public override bool IsDebuff => true;

    public override bool IgnoreGrowthNectar => true;

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }

    public override void Hooks()
    {
        On.RoR2.DeathRewards.OnKilledServer += LintyBonusGold;
    }

    private void LintyBonusGold(On.RoR2.DeathRewards.orig_OnKilledServer orig, DeathRewards self, DamageReport damageReport)
    {
        if (damageReport != null)
        {
            if (damageReport.victim)
            {
                var victimBody = damageReport.victimBody;
                if (victimBody)
                {
                    if (victimBody.HasBuff(BuffDef))
                    {
                        var attackerBody = damageReport.attackerBody;
                        if (attackerBody)
                        {
                            var stackCount = PocketLint.instance.GetCount(attackerBody);
                            if (stackCount > 0)
                            {
                                var rewardMultiplier = PocketLintLintyBonusGoldInit + ((stackCount - 1) * PocketLintLintyBonusGoldStack);
                                var debuffs = 0;

                                BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
                                foreach (BuffIndex buffType in debuffBuffIndices)
                                {
                                    if (victimBody.HasBuff(buffType))
                                    {
                                        debuffs++;
                                    }
                                }

                                DotController dotController = DotController.FindDotController(victimBody.gameObject);
                                if ((bool)dotController)
                                {
                                    for (DotController.DotIndex dotIndex = 0; dotIndex < (DotController.DotIndex)(DotAPI.VanillaDotCount + DotAPI.CustomDotCount); dotIndex++)
                                    {
                                        if (dotController.HasDotActive(dotIndex))
                                        {
                                            debuffs++;
                                        }
                                    }
                                }
                                if (debuffs > 0)
                                {
                                    rewardMultiplier += rewardMultiplier * (debuffs - 1);
                                    var gold = self.goldReward * (1 + rewardMultiplier);
                                    self.goldReward = (uint)gold;
                                }
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageReport);
    }
}
