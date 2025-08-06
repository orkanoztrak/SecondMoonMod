using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondMoon.Equipment.BladeOfPetrichor;

public class BladeOfPetrichor : Equipment<BladeOfPetrichor>
{
    public static ConfigOption<float> BladeOfPetrichorDuration;
    public static ConfigOption<float> BladeOfPetrichorBonusDmgPerRemainingHealth;

    public static ConfigOption<float> BladeOfPetrichorBonusDmgActive;
    public static ConfigOption<float> BladeOfPetrichorSwordFrequencyActive;
    public static ConfigOption<float> BladeOfPetrichorSwordSearchRadiusActive;
    public static ConfigOption<float> BladeOfPetrichorSwordDmgActive;
    public static ConfigOption<float> BladeOfPetrichorSwordExecutionThresholdActive;

    public override string EquipmentName => "Blade of Petrichor";

    public override string EquipmentLangTokenName => "BLADE_OF_PETRICHOR";

    public override string EquipmentPickupDesc => "Deal more damage to healthier enemies. Activate to instead gain a fixed damage bonus and summon executing swords periodically.";

    public override string EquipmentFullDescription => $"Deal <style=cIsDamage>{BladeOfPetrichorBonusDmgPerRemainingHealth}%</style> more damage to enemies per <style=cIsHealing>1%</style> health they have. " +
        $"Activate to instead increase damage dealt by <style=cIsDamage>{BladeOfPetrichorBonusDmgActive * 100}%</style> against all enemies " +
        $"and to <color=#7CFDEA>call down swords to strike enemies</color> within <color=#7CFDEA>{BladeOfPetrichorSwordSearchRadiusActive}m</color> every <color=#7CFDEA>{BladeOfPetrichorSwordFrequencyActive}s</color> for <style=cIsDamage>{BladeOfPetrichorSwordDmgActive * 100}%</style> damage " +
        $"- these swords <color=#7CFDEA>execute</color> enemies below <color=#7CFDEA>{BladeOfPetrichorSwordExecutionThresholdActive * 100}%</color> health. Lasts <style=cIsUtility>{BladeOfPetrichorDuration}s</style>.";

    public override string EquipmentLore => "I will create a system where the strong have the right to rule. " +
        "Absolutely, this is how it was supposed to be. A society where I can take what I want and in doing so make it rightfully mine.\r\n\r\n" +
        "But do not misunderstand. The strong have the responsibility to care for the weak. Those who rule must ensure the wellbeing of those who are ruled. I will be that kind of ruler.\r\n\r\n" +
        "I don't want the power. I want the responsibility. I want my people to rely on me and for me to be a pillar for them. Power for power's sake is what fools want. I am already strong. I want to use that power to lead.\r\n\r\n" +
        "Yet, the challenge has only begun. Until I am the one that makes the rules, I have to operate within the confines of the preexisting system.\r\n\r\n" +
        "- Lord Regent Silas Thomas, 25XX";

    public override bool IsPrototype => true;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamageProcess += BladeOfPetrichorDamageBoost;
    }

    private void BladeOfPetrichorDamageBoost(ILContext il)
    {
        var damageIndex = 7;
        var flagIndex = 5;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
                               x => x.MatchLdsfld(typeof(RoR2Content.Buffs), nameof(RoR2Content.Buffs.DeathMark))))
        {
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchLdloc(flagIndex)))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, damageIndex);
                cursor.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((self, info, num) =>
                {
                    if (info.attacker && self)
                    {
                        var attackerBody = info.attacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            var inventory = attackerBody.inventory;
                            if (inventory)
                            {
                                if (inventory.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                                {
                                    if (attackerBody.gameObject.GetComponent<BladeOfPetrichorActiveController>())
                                    {
                                        num *= 1 + BladeOfPetrichorBonusDmgActive;
                                    }
                                    else
                                    {
                                        num *= 1 + self.combinedHealthFraction * BladeOfPetrichorBonusDmgPerRemainingHealth;
                                    }
                                }
                            }
                        }
                    }
                    return num;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, damageIndex);
            }
        }
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var obj = slot.characterBody.gameObject;
            if (!obj.GetComponent<BladeOfPetrichorActiveController>())
            {
                var controller = obj.AddComponent<BladeOfPetrichorActiveController>();
                controller.ownerBody = slot.characterBody;
                controller.enabled = true;
                return true;
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
            CreateEquipment();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 60f, "How many seconds will this equipment's cooldown be?");
        BladeOfPetrichorDuration = config.ActiveBind("Equipment: " + EquipmentName, "Duration of active", 10f, "How many seconds does this equipment's active effect last?");
        BladeOfPetrichorBonusDmgPerRemainingHealth = config.ActiveBind("Equipment: " + EquipmentName, "Passive damage boost per remaining % health", 0.5f, "How much should damage be increased per remaining % health on the enemy while having the " + EquipmentName + " and not having it active? (0.5 = 0.5%, meaning 50% against an enemy with full health by default)");
        BladeOfPetrichorBonusDmgActive = config.ActiveBind("Equipment: " + EquipmentName, "Active damage boost", 1f, "How much should damage be increased by during the active effect? (1 = 100%)");
        BladeOfPetrichorSwordFrequencyActive = config.ActiveBind("Equipment: " + EquipmentName, "Active sword summoning frequency", 2f, "Every this many seconds swords will be summoned to strike enemies during the active effect.");
        BladeOfPetrichorSwordSearchRadiusActive = config.ActiveBind("Equipment: " + EquipmentName, "Active sword search radius", 15f, "Swords strike enemies within a radius of this many meters during the active effect.");
        BladeOfPetrichorSwordExecutionThresholdActive = config.ActiveBind("Equipment: " + EquipmentName, "Active sword execution threshold", 0.2f, "Below how much remaining health should the active effect swords execute targets? (0.2 = 20%)");
        BladeOfPetrichorSwordDmgActive = config.ActiveBind("Equipment: " + EquipmentName, "Active sword damage", 2f, "How much base damage should every active effect sword do? (2 = 200%)");
    }

    public class BladeOfPetrichorActiveController : MonoBehaviour
    {
        public float remainingDuration;

        public float frequencyStopwatch;

        public CharacterBody ownerBody;

        private void Awake()
        {
            enabled = false;
        }

        private void Start()
        {
            remainingDuration = BladeOfPetrichorDuration;
            frequencyStopwatch = BladeOfPetrichorSwordFrequencyActive;
        }
        
        private void FixedUpdate()
        {
            if (frequencyStopwatch >= BladeOfPetrichorSwordFrequencyActive)
            {
                List<HurtBox> targets = GeneralUtils.FindAllHurtboxesInRadius(BladeOfPetrichorSwordSearchRadiusActive, ownerBody.gameObject.transform.position);
                foreach (HurtBox target in targets)
                {
                    HealthComponent healthComponent = target.healthComponent;
                    if (healthComponent)
                    {
                        CharacterBody characterBody = healthComponent.body;
                        if (characterBody)
                        {
                            if (characterBody.teamComponent.teamIndex == ownerBody.teamComponent.teamIndex)
                            {
                                continue;
                            }
                        }
                        if (!target.healthComponent.Equals(ownerBody.healthComponent))
                        {
                            if (target.healthComponent.combinedHealthFraction <= BladeOfPetrichorSwordExecutionThresholdActive)
                            {
                                target.healthComponent.body?.master?.TrueKill(ownerBody.gameObject, null, default);
                            }
                            else
                            {
                                DamageInfo swordStrike = new DamageInfo
                                {
                                    damage = ownerBody.damage * BladeOfPetrichorSwordDmgActive,
                                    damageColorIndex = DamageColorIndex.Item,
                                    damageType = DamageType.Generic,
                                    attacker = ownerBody.gameObject,
                                    crit = false,
                                    force = Vector3.zero,
                                    inflictor = ownerBody.gameObject,
                                    position = target.transform.position,
                                    procCoefficient = 0
                                };
                                target.healthComponent.TakeDamage(swordStrike);
                            }
                        }
                    }
                }
                frequencyStopwatch = 0;
            }
            if (remainingDuration <= 0)
            {
                Destroy(this);
            }
            remainingDuration -= Time.fixedDeltaTime;
            frequencyStopwatch += Time.fixedDeltaTime;
        }
    }
}
