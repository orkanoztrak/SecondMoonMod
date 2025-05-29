using BepInEx.Configuration;
using RoR2.Orbs;
using R2API;
using RoR2;
using SecondMoon.Items.Prototype.FlailOfMass;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Items.Tier3.LightningRing;

public class LightningRing : Item<LightningRing>
{
    public static ConfigOption<float> LightningRingDamageThreshold;
    public static ConfigOption<int> LightningRingTargetCountInit;
    public static ConfigOption<int> LightningRingTargetCountStack;
    public static ConfigOption<float> LightningRingRadiusInit;
    public static ConfigOption<float> LightningRingRadiusStack;
    public static ConfigOption<float> LightningRingDamageInit;
    public static ConfigOption<float> LightningRingDamageStack;

    public override string ItemName => "Lightning Band";

    public override string ItemLangTokenName => "LIGHTNING_RING";

    public override string ItemPickupDesc => "Weak hits launch chain lightning.";

    public override string ItemFullDesc => $"Hits that deal <style=cIsDamage>less than {LightningRingDamageThreshold * 100}% damage</style> also strike enemies with <style=cIsDamage>lightning</style>, " +
        $"dealing <style=cIsDamage>{LightningRingDamageInit * 100}%</style> <style=cStack>(+{LightningRingDamageStack * 100}% per stack)</style> TOTAL damage. " +
        $"The lightning also chains to <style=cIsDamage>{LightningRingTargetCountInit}</style> <style=cStack>(+{LightningRingTargetCountStack} per stack)</style> enemies within <style=cIsDamage>{LightningRingRadiusInit}m</style> <style=cStack>(+{LightningRingRadiusStack}m per stack)</style> of the target for the same amount.";

    public override string ItemLore => $"\"As passion becomes memory and memory becomes passion,\r\nAs clouds and oceans wither and darken,\r\nMy fervor and patience fall silent,\r\nAs I fade, alongside them.\"\r\n\r\n" +
        $"-The Syzygy of Io and Europa, Final Verse";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier3Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Damage];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += LightningRingLaunchChainLightning;
    }

    private void LightningRingLaunchChainLightning(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimComponent = victim.GetComponent<HealthComponent>();
            if (attackerBody && victimComponent)
            {
                if (damageInfo.damage / attackerBody.damage <= LightningRingDamageThreshold) 
                {
                    var stackCount = GetCount(attackerBody);
                    if (stackCount > 0)
                    {
                        DamageInfo ringProc = new DamageInfo
                        {
                            damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, LightningRingDamageInit + ((stackCount - 1) * LightningRingDamageStack)),
                            damageColorIndex = DamageColorIndex.Item,
                            damageType = DamageType.Generic,
                            attacker = damageInfo.attacker,
                            crit = damageInfo.crit,
                            force = Vector3.zero,
                            inflictor = null,
                            position = damageInfo.position,
                            procCoefficient = 0
                        };
                        victimComponent.TakeDamage(ringProc);
                        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/SimpleLightningStrikeImpact"), new EffectData
                        {
                            origin = damageInfo.position
                        }, transmit: true);

                        List<HurtBox> targets = GeneralUtils.FindCountHurtboxesInRangeAroundTarget(LightningRingTargetCountInit + ((stackCount - 1) * LightningRingTargetCountStack), LightningRingRadiusInit + ((stackCount - 1) * LightningRingRadiusStack), attackerBody.teamComponent.teamIndex, victim.transform.position, victim);

                        foreach (HurtBox target in targets)
                        {
                            if (target)
                            {
                                LightningRingOrb lightning = new LightningRingOrb
                                {
                                    damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, LightningRingDamageInit + ((stackCount - 1) * LightningRingDamageStack)),
                                    isCrit = damageInfo.crit,
                                    teamIndex = TeamComponent.GetObjectTeam(attackerBody.gameObject),
                                    attacker = attackerBody.gameObject,
                                    procCoefficient = 0f,
                                    damageColorIndex = DamageColorIndex.Item,
                                    scale = 1f,
                                    target = target,
                                    origin = victim.transform.position
                                };
                                OrbManager.instance.AddOrb(lightning);
                            }
                        }
                    }
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
        LightningRingDamageThreshold = config.ActiveBind("Item: " + ItemName, "Damage threshold", 1f, "What is the maximum % of damage that can proc " + ItemName + "? (1 = 100%)");
        LightningRingTargetCountInit = config.ActiveBind("Item: " + ItemName, "Number of chain lightning targets with one " + ItemName, 2, "Lightning damages this many targets on hit with one " + ItemName + ".");
        LightningRingTargetCountStack = config.ActiveBind("Item: " + ItemName, "Number of chain lightning targets per stack after one " + ItemName, 1, "Lightning damages this many additional targets on hit per stack of " + ItemName + " after one.");
        LightningRingRadiusInit = config.ActiveBind("Item: " + ItemName, "Enemy search radius of chain lightning with one " + ItemName, 24f, "Chain lightning can strike enemies within this many meters with one " + ItemName + ".");
        LightningRingRadiusStack = config.ActiveBind("Item: " + ItemName, "Enemy search radius of chain lightning with per stack after one " + ItemName, 12f, "Chain lightning strike radius is increased by this many meters per stack of " + ItemName + " after one.");
        LightningRingDamageInit = config.ActiveBind("Item: " + ItemName, "Damage of the proc with one " + ItemName, 1f, "What % of TOTAL damage should the proc do with one " + ItemName + "? (1 = 100%)");
        LightningRingDamageStack = config.ActiveBind("Item: " + ItemName, "Damage of the proc per stack after one " + ItemName, 0.5f, "What % of TOTAL damage should be added to the proc per stack of " + ItemName + " after one? (0.5 = 50%)");
    }
}
