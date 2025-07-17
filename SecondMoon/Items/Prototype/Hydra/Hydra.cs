using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Orbs;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Prototype.Hydra;

public class Hydra : Item<Hydra>
{
    public static ConfigOption<int> HydraAdditionalHitCountInit;
    public static ConfigOption<int> HydraAdditionalHitCountStack;
    public static ConfigOption<float> HydraAdditionalHitProcCoefficientMult;
    public static ConfigOption<float> HydraAdditionalHitDamage;
    public static DamageAPI.ModdedDamageType HydraOrbLoopPrevention;
    public override string ItemName => "Hydra";

    public override string ItemLangTokenName => "HYDRA";

    public override string ItemPickupDesc => "Your skills launch additional hits with reduced damage.";

    public override string ItemFullDesc => $"Your skills hit <color=#7CFDEA>{HydraAdditionalHitCountInit} (+{HydraAdditionalHitCountStack} per stack) extra times</color>. These hits deal <style=cIsDamage>{HydraAdditionalHitDamage * 100}%</style> TOTAL damage, with <style=cIsDamage>{HydraAdditionalHitProcCoefficientMult * 100}%</style> of the original hit's proc coefficient.";

    public override string ItemLore => "When Mithrix went away, his brother hurried to the well. Without regard for his own safety, he stuck his hand into it. Thorp! Out he pulled a worm, but it, along with his hand, was brutally deformed. Still, he wouldn't cry.\r\n\r\n" +
        "Even though his hand healed after, the unfortunate creature was left unable to grow, forcibly conjoined by the gravitational force and mangled. Alone it would surely die, but he was not going to let that happen. You see, he loved worms.";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage];

    public GameObject HydraOrbEffectFireVariant;
    public GameObject HydraOrbImpactFireVariant;

    public GameObject HydraOrbEffectLightningVariant;
    public GameObject HydraOrbImpactLightningVariant;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += HydraMultiHits;
    }

    private void HydraMultiHits(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && damageInfo.damageType.IsDamageSourceSkillBased && !damageInfo.HasModdedDamageType(HydraOrbLoopPrevention))
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                var stackCount = GetCount(attackerBody);
                if (stackCount > 0)
                {
                    for (int i = 0; i < HydraAdditionalHitCountInit + (stackCount - 1) * HydraAdditionalHitCountStack; i++)
                    {
                        var teamComponent = attackerBody.GetComponent<TeamComponent>();
                        var victimBody = victim ? victim.GetComponent<CharacterBody>() : null;

                        HydraOrb hydraOrb = new HydraOrb
                        {
                            origin = damageInfo.position,
                            damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, HydraAdditionalHitDamage),
                            isCrit = damageInfo.crit,
                            teamIndex = teamComponent ? teamComponent.teamIndex : TeamIndex.Neutral,
                            attacker = damageInfo.attacker,
                            inflictor = damageInfo.inflictor,
                            procCoefficient = damageInfo.procCoefficient * HydraAdditionalHitProcCoefficientMult,
                            damageColorIndex = damageInfo.damageColorIndex,
                            procChainMask = damageInfo.procChainMask,
                            damageType = damageInfo.damageType 
                        };
                        if (i % 2 == 0)
                        {
                            hydraOrb.orbVariant = HydraOrb.OrbVariant.Fire;
                        }
                        else
                        {
                            hydraOrb.orbVariant = HydraOrb.OrbVariant.Lightning;
                        }
                        HurtBox mainHurtBox2 = victimBody?.mainHurtBox;
                        if ((bool)mainHurtBox2)
                        {
                            hydraOrb.target = mainHurtBox2;
                            OrbManager.instance.AddOrb(hydraOrb);
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
            OrbAPI.AddOrb(typeof(HydraOrb));
            HydraOrbLoopPrevention = DamageAPI.ReserveDamageType();
            CreateLang();
            CreateFireOrb();
            CreateLightningOrb();
            CreateItem();
            Hooks();
        }
    }

    private void CreateFireOrb()
    {
        var tempEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ChainLightningVoid/VoidLightningOrbEffect.prefab").WaitForCompletion().InstantiateClone("HydraOrbEffectFireVariant");
        var bezier = tempEffect.transform.Find("Bezier");
        LineRenderer lineRenderer = bezier.GetComponent<LineRenderer>();
        lineRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/DLC1/snowyforest/matSFFireStaticYellowLarge.mat").WaitForCompletion();

        var VFX = bezier.Find("HarshGlow, Billboard");
        ParticleSystemRenderer renderer = VFX.GetComponent<ParticleSystemRenderer>();
        renderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matFirePillarParticle.mat").WaitForCompletion();

        var tempImpact = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ChainLightningVoid/VoidLightningStrikeImpact.prefab").WaitForCompletion().InstantiateClone("HydraOrbImpactFireVariant");
        var flash = tempImpact.transform.Find("Flash");
        ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
        flashRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/ElementalRings/matFireTornadoBillboardEffect.mat").WaitForCompletion();

        var omniSparks = tempImpact.transform.Find("OmniSparks");
        ParticleSystemRenderer omniSparksRenderer = omniSparks.GetComponent<ParticleSystemRenderer>();
        omniSparksRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/mageMageFireStarburst.mat").WaitForCompletion();

        var orbEffect = tempEffect.GetComponent<OrbEffect>();
        orbEffect.endEffect = tempImpact;
        HydraOrbEffectFireVariant = tempEffect;
        HydraOrbImpactFireVariant = tempImpact;
        ContentAddition.AddEffect(HydraOrbEffectFireVariant);
        ContentAddition.AddEffect(HydraOrbImpactFireVariant);
    }

    private void CreateLightningOrb()
    {
        var tempEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ChainLightningVoid/VoidLightningOrbEffect.prefab").WaitForCompletion().InstantiateClone("HydraOrbEffectLightningVariant");
        var bezier = tempEffect.transform.Find("Bezier");
        LineRenderer lineRenderer = bezier.GetComponent<LineRenderer>();
        lineRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLightningLongBlue.mat").WaitForCompletion();

        var VFX = bezier.Find("HarshGlow, Billboard");
        ParticleSystem.MainModule main = VFX.GetComponent<ParticleSystem>().main;
        main.startColor = Color.white;
        ParticleSystemRenderer renderer = VFX.GetComponent<ParticleSystemRenderer>();
        renderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matJellyfishLightning.mat").WaitForCompletion();

        var tempImpact = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ChainLightningVoid/VoidLightningStrikeImpact.prefab").WaitForCompletion().InstantiateClone("HydraOrbImpactLightningVariant");
        var flash = tempImpact.transform.Find("Flash");
        ParticleSystem.MainModule flashSystem = flash.GetComponent<ParticleSystem>().main;
        flashSystem.startColor = Color.white;
        ParticleSystemRenderer flashRenderer = flash.GetComponent<ParticleSystemRenderer>();
        flashRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Merc/matMercIgnition.mat").WaitForCompletion();

        var omniSparks = tempImpact.transform.Find("OmniSparks");
        ParticleSystem.MainModule omniSparksMain = omniSparks.GetComponent<ParticleSystem>().main;
        omniSparksMain.startColor = new ParticleSystem.MinMaxGradient(new Color32(0, 60, 255, 255));
        ParticleSystemRenderer omniSparksRenderer = omniSparks.GetComponent<ParticleSystemRenderer>();
        omniSparksRenderer.material = Addressables.LoadAssetAsync<Material>("RoR2/Base/Common/VFX/matLightningLongBlue.mat").WaitForCompletion();

        var orbEffect = tempEffect.GetComponent<OrbEffect>();
        orbEffect.endEffect = tempImpact;
        HydraOrbEffectLightningVariant = tempEffect;
        HydraOrbImpactLightningVariant = tempImpact;
        ContentAddition.AddEffect(HydraOrbEffectLightningVariant);
        ContentAddition.AddEffect(HydraOrbImpactLightningVariant);
    }

    private void CreateConfig(ConfigFile config)
    {
        HydraAdditionalHitCountInit = config.ActiveBind("Item: " + ItemName, "Number of extra hits with one " + ItemName, 2, "This many extra hits are launched with one " + ItemName + ".");
        HydraAdditionalHitCountStack = config.ActiveBind("Item: " + ItemName, "Number of extra per stack after one " + ItemName, 1, "This many extra hits are launched per stack of " + ItemName + " after one.");
        HydraAdditionalHitProcCoefficientMult = config.ActiveBind("Item: " + ItemName, "Multiplier applied to the proc coefficient of the original hit", 0.5f, "The additional hits have proc coefficient equal to that of the original hit, multiplied by this value.");
        HydraAdditionalHitDamage = config.ActiveBind("Item: " + ItemName, "Damage dealt by extra hits", 0.5f, "What % of TOTAL damage should the extra hits from skills do? (0.5 = 50%)");
    }
}
