using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Equipment.FriendlyAnomaly;

public class FriendlyAnomaly : Equipment<FriendlyAnomaly>
{
    public GameObject FriendlyAnomalyKillEffect;

    public override string EquipmentName => "Friendly Anomaly";

    public override string EquipmentLangTokenName => "FRIENDLY_ANOMALY";

    public override string EquipmentPickupDesc => "Execute an elite monster and gain its power. Large monsters are immune.";

    public override string EquipmentFullDescription => $"<style=cIsDamage>Execute</style> any enemy that isn't a large monster. If it was elite, gain its <style=cIsDamage>power</style> for the rest of the stage.";

    public override string EquipmentLore => $"Test";

    public override bool UseTargeting => true;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var targetComponent = slot.GetComponent<TargetingControllerComponent>();
            if (targetComponent)
            {
                if (targetComponent.TargetObject)
                {
                    var chosenHurtbox = targetComponent.TargetFinder.GetResults().First();
                    if (chosenHurtbox)
                    {
                        var targetBody = chosenHurtbox.healthComponent.body;
                        if (targetBody)
                        {
                            if (!targetBody.isChampion && targetBody.isElite)
                            {
                                for (int k = 0; k < BuffCatalog.eliteBuffIndices.Length; k++)
                                {
                                    BuffIndex buffIndex = BuffCatalog.eliteBuffIndices[k];
                                    if (targetBody.HasBuff(buffIndex))
                                    {
                                        slot.characterBody.AddBuff(buffIndex);
                                    }
                                }
                                Vector3 vector = chosenHurtbox.transform ? chosenHurtbox.transform.position : Vector3.zero;
                                GameObject effect = GameObject.Instantiate(FriendlyAnomalyKillEffect);
                                effect.transform.position = vector;
                                effect.SetActive(true);
                                targetBody.master?.TrueKill(slot.characterBody.gameObject, null, default);
                                return true;
                            }
                        }
                    }
                }
            }
        }
        return false;
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateTargeting();
            CreateKillEffect();
            CreateEquipment();
            Hooks();
        }
    }

    private void CreateKillEffect()
    {
        GameObject effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/BossHunter/BossHunterKillEffect.prefab").WaitForCompletion().InstantiateClone("AnomalyKillEffect", false);
        ParticleSystem.MainModule color1 = effectPrefab.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().main;
        color1.startColor = new ParticleSystem.MinMaxGradient(new Color32(243, 0, 146, 255));
        ParticleSystem.MainModule color2 = effectPrefab.transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>().main;
        color2.startColor = new ParticleSystem.MinMaxGradient(new Color32(221, 25, 160, 255));
        ParticleSystem.MainModule color3 = effectPrefab.transform.GetChild(0).GetChild(4).GetComponent<ParticleSystem>().main;
        color3.startColor = new ParticleSystem.MinMaxGradient(new Color32(243, 0, 146, 255));
        ParticleSystem.MainModule color4 = effectPrefab.transform.GetChild(0).GetChild(6).GetComponent<ParticleSystem>().main;
        color4.startColor = new ParticleSystem.MinMaxGradient(new Color32(101, 17, 54, 255));
        ParticleSystem.MainModule color5 = effectPrefab.transform.GetChild(0).GetChild(7).GetComponent<ParticleSystem>().main;
        color5.startColor = new ParticleSystem.MinMaxGradient(new Color32(255, 0, 25, 255));

        FriendlyAnomalyKillEffect = effectPrefab;
    }

    public override void Hooks()
    {
        On.RoR2.EquipmentSlot.Update += FilterOutBossesAndNonElites;
        On.RoR2.CharacterBody.OnEquipmentLost += RemoveTargetingIfExists;
    }

    private void RemoveTargetingIfExists(On.RoR2.CharacterBody.orig_OnEquipmentLost orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef && self)
        {
            var targetingComponent = self.GetComponent<TargetingControllerComponent>();
            if (targetingComponent)
            {
                targetingComponent.Invalidate();
                targetingComponent.Indicator.active = false;
            }
        }
        orig(self, equipmentDef);
    }

    private void FilterOutBossesAndNonElites(On.RoR2.EquipmentSlot.orig_Update orig, EquipmentSlot self)
    {
        orig(self);
        if (self.equipmentIndex == EquipmentDef.equipmentIndex)
        {
            var targetingComponent = self.GetComponent<TargetingControllerComponent>();
            if (targetingComponent)
            {
                targetingComponent.AdditionalBullseyeFunctionality = (bullseyeSearch) => 
                {
                    if (bullseyeSearch.candidatesEnumerable.Any())
                    {
                        bullseyeSearch.candidatesEnumerable = bullseyeSearch.candidatesEnumerable.Where(x => (x.hurtBox != null) && !x.hurtBox.healthComponent.body.isChampion && x.hurtBox.healthComponent.body.isElite).ToList();
                    }
                };
            }
        }
    }

    private void CreateTargeting()
    {
        TargetingIndicatorPrefabBase = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/BossHunter/BossHunterIndicator.prefab").WaitForCompletion(), "AnomalyIndicator", false);
        SpriteRenderer[] array = TargetingIndicatorPrefabBase.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in array)
        {
            renderer.color = new Color32(243, 0, 146, 255);
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 75f, "How many seconds will this equipment's cooldown be?");
    }
}
