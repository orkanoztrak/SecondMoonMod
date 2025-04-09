using BepInEx.Configuration;
using EntityStates.AffixVoid;
using HG;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SecondMoon.Equipment.AffixGuardian;

public class AffixGuardian : EliteEquipment<AffixGuardian>
{
    public ConfigOption<float> AffixGuardianDropOnDeathChance;
    public ConfigOption<float> AffixGuardianBarrierGainLowerThreshold;
    public ConfigOption<float> AffixGuardianCDForMaxBarrier;
    public ConfigOption<float> AffixGuardianNonPlayerBarrierGainNerf;

    public ConfigOption<float> AffixGuardianHurtTriggerThreshold;
    public ConfigOption<bool> AffixGuardianHurtNovaUniqueTriggers;
    public ConfigOption<float> AffixGuardianHurtNovaRadius;
    public ConfigOption<float> AffixGuardianHurtNovaBarrierPercent;
    public ConfigOption<float> AffixGuardianHurtNovaGuardianBarrierPercent;
    public override string EliteEquipmentName => "His Warding Essence";

    public override string EliteAffixToken => "AFFIX_GUARDIAN";

    public override string EliteEquipmentPickupDesc => "Become an aspect of safeguarding.";

    public override string EliteEquipmentFullDescription => "???";

    public override string EliteEquipmentLore => "";

    public override float DropOnDeathChance => 0.00025f;

    public override float HealthMultiplier => 2f;

    public override float DamageMultiplier => 2f;

    public override string EliteModifier => "Guardian";

    public override Color EliteColor => new Color32(124, 253, 234, 255);

    public override Sprite EliteBuffIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarGolem/texBuffLunarShellIcon.tif").WaitForCompletion();

    //public override Texture2D EliteRamp => Addressables.LoadAssetAsync<Texture2D>("RoR2/DLC1/Common/ColorRamps/texRampStrongerBurn.png").WaitForCompletion();
    public override Texture2D EliteRamp => SecondMoonPlugin.SecondMoonAssets.LoadAsset<Texture2D>("Assets/SecondMoonAssets/Textures/Ramps/texRampStrongerBurn.png");

    public GameObject BarrierPulseEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TPHealingNova/TeleporterHealNovaPulse.prefab").WaitForCompletion();

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        { 
            CreateConfig(config);
            CreateLang();
            CreateEliteEquipment();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        AffixGuardianDropOnDeathChance = config.ActiveBind("Equipment: " + EliteEquipmentName, "Chance to drop on death", 0.00025f, "How likely is the player to get this item upon killing a " + EliteModifier + " elite? (0.00025 = 0.025%, which is 1/4000, the base odds for an elite aspect drop in vanilla)");
        AffixGuardianBarrierGainLowerThreshold = config.ActiveBind("Equipment: " + EliteEquipmentName, "Minimum cooldown required for barrier gain", 4f, "Skills with cooldown below this many seconds will not grant barrier upon being activated.");
        AffixGuardianCDForMaxBarrier = config.ActiveBind("Equipment: " + EliteEquipmentName, "Cooldown required for gaining max barrier", 40f, "Using a skill with at least this much cooldown will grant a 100% barrier. Barrier gain for lower cooldowns is directly proportional.");
        AffixGuardianNonPlayerBarrierGainNerf = config.ActiveBind("Equipment: " + EliteEquipmentName, "Barrier gain reduction for enemies", 0.25f, "The maximum possible barrier gain through skill use is multiplied by this value. Accordingly, gain for lower cooldowns is also reduced.");
        AffixGuardianHurtTriggerThreshold = config.ActiveBind("Equipment: " + EliteEquipmentName, "Barrier nova trigger percentage", 0.25f, "Upon taking damage equal to maximum combined health multiplied by this value, a nova that grants barrier is released. By default, a nova is released at each quarter of health.");
        AffixGuardianHurtNovaUniqueTriggers = config.ActiveBind("Equipment: " + EliteEquipmentName, "Limit triggers to one for each chunk of health", true, "This prevents repeated triggers as a result of healing, so the nova will only trigger once for each chunk of lost health. Disabling it may cause fights against enemies like Clay Dunestriders to be extremely difficult. Players are unaffected by this restriction.");
        AffixGuardianHurtNovaRadius = config.ActiveBind("Equipment: " + EliteEquipmentName, "Hurt nova radius", 60f, "The radius of the nova is this many meters.");
        AffixGuardianHurtNovaBarrierPercent = config.ActiveBind("Equipment: " + EliteEquipmentName, "Hurt nova barrier percentage", 0.5f, "What is the % for barrier gain upon losing a chunk of health? (0.5 = 50% of maximum combined health)");
        AffixGuardianHurtNovaGuardianBarrierPercent = config.ActiveBind("Equipment: " + EliteEquipmentName, "Hurt nova barrier percentage for the elite", 0.125f, "Since " + EliteModifier + " elites have no barrier decay, they have appropriately lower (hopefully) barrier gain. (0.125 = 12.5% of maximum combined health)");
    }

    public override void Hooks()
    {
        IL.RoR2.CharacterBody.RecalculateStats += StopBarrierDecay;
        On.RoR2.GenericSkill.OnExecute += GetBarrierBySkillUse;
        On.RoR2.CharacterBody.OnEquipmentGained += AddGuardianBehavior;
        On.RoR2.CharacterBody.OnEquipmentLost += RemoveGuardianBehavior;
        On.RoR2.HealthComponent.TakeDamage += TriggerGuardianHurtNova;
    }

    private void TriggerGuardianHurtNova(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        orig(self, damageInfo);
        var body = self.body;
        if (body)
        {
            var guardianController = body.gameObject.GetComponent<GuardianEliteBehavior>();
            if (guardianController)
            {
                int i = 0;
                float j = 1f;
                bool stop = false;
                float combinedHealthAndShield = (self.health + self.shield) / (self.fullHealth + self.fullShield);
                while (!stop)
                {
                    if (j > combinedHealthAndShield)
                    {
                        j -= instance.AffixGuardianHurtTriggerThreshold;
                        i++;
                    }
                    else
                    {
                        stop = true;
                    }
                }
                Debug.Log("Current health fraction: " + combinedHealthAndShield);
                Debug.Log("Current health chunk (from the right): " + i);
                if (1 - combinedHealthAndShield >= guardianController.damageChunkTracker * instance.AffixGuardianHurtTriggerThreshold 
                    && body.healthComponent.alive 
                    && !guardianController.triggerTheNova 
                    && guardianController.lastTriggeredAtChunk != i)
                {
                    Debug.Log("Trigger the nova");
                    guardianController.lastTriggeredAtChunk = i;
                    guardianController.triggerTheNova = true;
                }
                else if (1 - instance.AffixGuardianHurtTriggerThreshold * guardianController.lastTriggeredAtChunk > combinedHealthAndShield || combinedHealthAndShield > 1 - (instance.AffixGuardianHurtTriggerThreshold * (guardianController.lastTriggeredAtChunk - 1)))
                {
                    guardianController.lastTriggeredAtChunk = 0;
                }
            }
        }
    }

    private void AddGuardianBehavior(On.RoR2.CharacterBody.orig_OnEquipmentGained orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EliteEquipmentDef && self)
        {
            var controller = self.gameObject.AddComponent<GuardianEliteBehavior>();
            controller.body = self;
            controller.enabled = true;
        }
        orig(self, equipmentDef);
    }

    private void RemoveGuardianBehavior(On.RoR2.CharacterBody.orig_OnEquipmentLost orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EliteEquipmentDef && self)
        {
            var controller = self.gameObject.GetComponent<GuardianEliteBehavior>();
            if (controller)
            {
                UnityEngine.Object.Destroy(controller);
            }
        }
        orig(self, equipmentDef);
    }

    private void GetBarrierBySkillUse(On.RoR2.GenericSkill.orig_OnExecute orig, GenericSkill self)
    {
        orig(self);
        if (self.cooldownRemaining >= AffixGuardianBarrierGainLowerThreshold)
        {
            var body = self.characterBody;
            if (body)
            {
                var healthComponent = body.healthComponent;
                if (healthComponent && body.HasBuff(EliteBuffDef))
                {
                    var barrierAmount = healthComponent.fullCombinedHealth * self.cooldownRemaining / AffixGuardianCDForMaxBarrier;
                    if (!body.isPlayerControlled)
                    {
                        barrierAmount *= AffixGuardianNonPlayerBarrierGainNerf;
                    }
                    healthComponent.AddBarrierAuthority(barrierAmount);
                }
            }
        }
    }

    private void StopBarrierDecay(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchCallOrCallvirt<CharacterBody>("set_barrierDecayRate")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<float, CharacterBody, float>>((decayRate, body) =>
            {
                if (body)
                {
                    return body.HasBuff(EliteBuffDef) ? 0f : decayRate;
                }
                return decayRate;
            });
        }
    }

    public class GuardianEliteBehavior : MonoBehaviour
    {
        public CharacterBody body;

        public int damageChunkTracker;

        public int lastTriggeredAtChunk;

        public bool triggerTheNova;

        public static AnimationCurve novaRadiusCurve;

        public float duration;

        private Transform effectTransform;

        private BarrierPulse barrierPulse;

        private float radius;

        private float stopwatch;

        private void Awake()
        {
            enabled = false;
        }

        private void Start()
        {
            triggerTheNova = false;
            Keyframe keyframe1 = new()
            {
                time = 0,
                value = 0,
                inTangent = 2,
                outTangent = 2
            };

            Keyframe keyframe2 = new()
            {
                time = 1f,
                value = 1f
            };
            novaRadiusCurve = new AnimationCurve(keyframe1, keyframe2);
            duration = 2f;
            radius = instance.AffixGuardianHurtNovaRadius;
            damageChunkTracker = 1;
            lastTriggeredAtChunk = 0;
            stopwatch = 0;
        }

        private void FixedUpdate()
        {
            if (body.healthComponent) 
            {
                if (triggerTheNova && barrierPulse == null)
                {
                    if (NetworkServer.active)
                    {
                        if (body.teamComponent)
                        {
                            TeamIndex teamIndex = body.teamComponent.teamIndex;
                            if (instance.AffixGuardianHurtNovaUniqueTriggers && !body.isPlayerControlled)
                            {
                                damageChunkTracker++;
                            }
                            effectTransform = instance.BarrierPulseEffect.transform.GetChild(0);
                            effectTransform.gameObject.SetActive(true);
                            barrierPulse = new BarrierPulse(body.transform.position, radius, duration, teamIndex);
                        }
                    }
                }
            }
            if (barrierPulse != null)
            {
                stopwatch += Time.fixedDeltaTime;
                if (effectTransform.gameObject.activeSelf)
                {
                    if (novaRadiusCurve == null)
                    {
                        Debug.Log("lmao");
                    }
                    float num = radius * novaRadiusCurve.Evaluate(stopwatch / duration);
                    Debug.Log("num: " + num);
                    effectTransform.localScale = new Vector3(num, num, num);
                }
                if (barrierPulse.isFinished)
                {
                    barrierPulse = null;
                    triggerTheNova = false;
                    stopwatch = 0;
                    effectTransform.gameObject.SetActive(false);
                }
                else
                {
                    barrierPulse.Update();
                }
            }
        }

        private class BarrierPulse
        {
            private readonly List<HealthComponent> barrierTargets = new List<HealthComponent>();

            private readonly SphereSearch sphereSearch;

            private float rate;

            private float t;

            private float finalRadius;

            private TeamMask teamMask;

            private readonly List<HurtBox> hurtBoxesList = new List<HurtBox>();

            public bool isFinished => t >= 1f;

            public BarrierPulse(Vector3 origin, float finalRadius, float duration, TeamIndex teamIndex)
            {
                sphereSearch = new SphereSearch
                {
                    mask = LayerIndex.entityPrecise.mask,
                    origin = origin,
                    queryTriggerInteraction = QueryTriggerInteraction.Collide,
                    radius = 0f
                };
                this.finalRadius = finalRadius;
                rate = 1f / duration;
                teamMask = default;
                teamMask.AddTeam(teamIndex);
            }

            public void Update()
            {
                t += rate * Time.fixedDeltaTime;
                t = (t > 1f) ? 1f : t;
                sphereSearch.radius = finalRadius * novaRadiusCurve.Evaluate(t);
                sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities()
                    .GetHurtBoxes(hurtBoxesList);
                int i = 0;
                for (int count = hurtBoxesList.Count; i < count; i++)
                {
                    HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                    if (!barrierTargets.Contains(healthComponent))
                    {
                        barrierTargets.Add(healthComponent);
                        ApplyBarrier(healthComponent);
                    }
                }
                Debug.Log("hurtBoxes count: " + hurtBoxesList.Count + "\nt: " + t);
                hurtBoxesList.Clear();
            }

            private void ApplyBarrier(HealthComponent target)
            {
                if (target.body)
                {
                    if (target.body.HasBuff(instance.EliteBuffDef))
                    {
                        target.AddBarrierAuthority(target.fullBarrier * instance.AffixGuardianHurtNovaGuardianBarrierPercent);
                    }
                    else
                    {
                        target.AddBarrierAuthority(target.fullBarrier * instance.AffixGuardianHurtNovaBarrierPercent);
                    }
                }
                Util.PlaySound("Play_item_proc_TPhealingNova_hitPlayer", target.gameObject);
            }
        }
    }
}
