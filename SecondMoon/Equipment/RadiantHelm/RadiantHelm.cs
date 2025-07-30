using BepInEx.Configuration;
using EntityStates;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using SecondMoon.Equipment;
using SecondMoon.MyEntityStates.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace SecondMoon.Equipment.RadiantHelm;

public class RadiantHelm : Equipment<RadiantHelm>
{
    public static Dictionary<CharacterMaster, int> RadiantHelmKillTracker = new Dictionary<CharacterMaster, int>();

    public static ConfigOption<float> RadiantHelmBaseBlinkDistance;

    public static ConfigOption<float> RadiantHelmActivationCDR;

    public static ConfigOption<float> RadiantHelmBossDmgBoost;

    public static ConfigOption<float> RadiantHelmStatBoost;

    public static ConfigOption<int> RadiantHelmFirstUpgradeKillThreshold;
    public static ConfigOption<int> RadiantHelmSecondUpgradeKillThreshold;
    public static ConfigOption<int> RadiantHelmThirdUpgradeKillThreshold;

    //public override GameObject EliteEquipmentModel => SecondMoonPlugin.SecondMoonAssets.LoadAsset<GameObject>("Assets/Models/Equipment/RadiantHelm.fbx");
    public override string EquipmentName => "Radiant Helm";

    public override string EquipmentLangTokenName => "RADIANT_HELM";

    public override string EquipmentPickupDesc => "Disappear and teleport forward. Permanently upgraded with large boss monster kills. This equipment's base cooldown cannot be changed except by its own effect.";

    public override string EquipmentFullDescription => $"Disappear and teleport <style=cIsUtility>{RadiantHelmBaseBlinkDistance}m</style> forward, and reduce the <style=cIsUtility>remaining cooldown of skills</style> by <style=cIsUtility>{RadiantHelmActivationCDR * 100}%</style>. <color=#7CFDEA>Large boss monster kills enhance the power of this equipment for the rest of this run:</color>\r\n\r\n" +
        $"• <color=#7CFDEA>{RadiantHelmFirstUpgradeKillThreshold} kills:</color> Deal <style=cIsDamage>{RadiantHelmBossDmgBoost * 100}%</style> more damage to large monsters and <style=cIsDamage>double</style> your <style=cIsDamage>proc coefficient</style> against them.\r\n" +
        $"• <color=#7CFDEA>{RadiantHelmSecondUpgradeKillThreshold} kills:</color> <style=cIsUtility>Halve</style> the <style=cIsUtility>cooldown</style> of this.\r\n" +
        $"• <color=#7CFDEA>{RadiantHelmThirdUpgradeKillThreshold} kills:</color> Increase <style=cIsUtility>ALL stats</style> by <style=cIsUtility>{RadiantHelmStatBoost * 100}%</style>.\r\n\r\n" +
        $"This equipment's base cooldown cannot be changed except by its own effect.";

    public override string EquipmentLore => "For a creature on two legs to punch as effectively as possible, the body needs to contort and twist in certain ways. The weight shifts from one leg to the other, as the spine rotates. The toes rotate, and the arm extends. All of this allows the fighter to maximize the power of their attack.\r\n\r\n" +
        "Just like how the punch requires many different movements happening in tandem to each other, in complete coordination, that is exactly how wars are fought. A general must plan and command each part of her army as if they are parts of her body, preparing to throw a punch.\r\n\r\n" +
        "Now, child, observe how I place myself according to my opponent, in this case, you. Don't be scared, I am merely trying to show you some points of importance. Move, and see how I move in accordance. Now, try to attack me and observe. I do not just escape your attack. I position myself so that I may be the one who will have the opportunity to attack my opponent's now exposed soft underbelly.\r\n\r\n" +
        "War is all about placement and displacement. It is a dance of violence, performed on the grandest stage of all; history. When two experienced commanders face one another, war becomes art, painted in blood.\r\n\r\n" +
        "- Grand General Mithras to his daughter, 25XX";

    public override bool IsPrototype => true;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnHitAllProcess += RadiantHelmBossProcCoefficientBoost;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += RadiantHelmBossProcCoefficientBoost;
        On.RoR2.HealthComponent.TakeDamage += RadiantHelmBossProcCoefficientBoost;
        IL.RoR2.HealthComponent.TakeDamageProcess += RadiantHelmBossDamageBoost;
        On.RoR2.Inventory.CalculateEquipmentCooldownScale += RadiantHelmFixedCooldown;
        IL.RoR2.CharacterBody.RecalculateStats += RadiantHelmModifyStats;
        RecalculateStatsAPI.GetStatCoefficients += Crit;
        On.RoR2.GlobalEventManager.OnCharacterDeath += RadiantHelmTrackKillUpgrades;
        On.RoR2.CharacterBody.OnEquipmentGained += AddController;
        On.RoR2.CharacterBody.OnEquipmentLost += RemoveController;
    }

    private void RadiantHelmBossProcCoefficientBoost(On.RoR2.GlobalEventManager.orig_OnHitAllProcess orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
    {
        var flag = false;
        var component = hitObject.GetComponent<HealthComponent>();
        if (component)
        {
            if (component.body.isBoss && component.body.isChampion)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    var inventory = attackerBody.inventory;
                    if (inventory)
                    {
                        if (inventory.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                        {
                            if (RadiantHelmKillTracker[attackerBody.master] >= RadiantHelmFirstUpgradeKillThreshold)
                            {
                                flag = true;
                                damageInfo.procCoefficient *= 2;
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, hitObject);
        if (flag)
        {
            damageInfo.procCoefficient /= 2;
        }

    }

    private void RadiantHelmBossProcCoefficientBoost(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        var flag = false;
        var component = victim.GetComponent<HealthComponent>();
        if (component)
        {
            if (component.body.isBoss && component.body.isChampion)
            {
                var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    var inventory = attackerBody.inventory;
                    if (inventory)
                    {
                        if (inventory.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                        {
                            if (RadiantHelmKillTracker[attackerBody.master] >= RadiantHelmFirstUpgradeKillThreshold)
                            {
                                flag = true;
                                damageInfo.procCoefficient *= 2;
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
        if (flag)
        {
            damageInfo.procCoefficient /= 2;
        }
    }

    private void RadiantHelmBossProcCoefficientBoost(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        var flag = false;
        if (self.body)
        {
            if (self.body.isBoss && self.body.isChampion)
            {
                if (damageInfo.attacker)
                {
                    var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                    if (attackerBody)
                    {
                        var inventory = attackerBody.inventory;
                        if (inventory)
                        {
                            if (inventory.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                            {
                                if (RadiantHelmKillTracker[attackerBody.master] >= RadiantHelmFirstUpgradeKillThreshold)
                                {
                                    flag = true;
                                    damageInfo.procCoefficient *= 2;
                                }
                            }
                        }
                    }
                }
            }
        }
        orig(self, damageInfo);
        if (flag)
        {
            damageInfo.procCoefficient /= 2;
        }
    }

    private void RadiantHelmBossDamageBoost(ILContext il)
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
                        if (self.body.isBoss && self.body.isChampion)
                        {
                            var attackerBody = info.attacker.GetComponent<CharacterBody>();
                            if (attackerBody)
                            {
                                var inventory = attackerBody.inventory;
                                if (inventory)
                                {
                                    if (inventory.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                                    {
                                        if (RadiantHelmKillTracker[attackerBody.master] >= RadiantHelmFirstUpgradeKillThreshold)
                                        {
                                            num *= 1 + RadiantHelmBossDmgBoost;
                                        }
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

    private float RadiantHelmFixedCooldown(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
    {
        if (self.currentEquipmentIndex == EquipmentDef.equipmentIndex) 
        {
            var master = self.gameObject.GetComponent<CharacterMaster>();
            if (master)
            {
                if (RadiantHelmKillTracker[master] >= RadiantHelmSecondUpgradeKillThreshold)
                {
                    return 0.5f;
                }
                return 1;
            }
        }
        return orig(self);
    }

    private void RadiantHelmModifyStats(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchLdarg(0),
            x => x.MatchLdarg(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth"),
            x => x.MatchCallOrCallvirt<CharacterBody>("set_maxBonusHealth")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                if (body)
                {
                    if (body.master)
                    {
                        if (body.master.inventory?.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                        {
                            if (RadiantHelmKillTracker[body.master] >= RadiantHelmThirdUpgradeKillThreshold)
                            {
                                body.maxHealth *= 1 + RadiantHelmStatBoost;
                                body.maxShield *= 1 + RadiantHelmStatBoost;
                                body.regen += Math.Abs(body.regen) * RadiantHelmStatBoost;
                                body.moveSpeed *= 1 + RadiantHelmStatBoost;
                                body.damage *= 1 + RadiantHelmStatBoost;
                                body.attackSpeed *= 1 + RadiantHelmStatBoost;
                                body.armor *= 1 + RadiantHelmStatBoost;
                            }
                        }
                    }
                }
            });
        }
    }

    private void Crit(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
    {
        var master = sender.master;
        if (master)
        {
            if (master.inventory?.currentEquipmentIndex == EquipmentDef.equipmentIndex)
            {
                if (RadiantHelmKillTracker[master] >= RadiantHelmThirdUpgradeKillThreshold)
                {
                    args.critAdd += RadiantHelmStatBoost;
                }
            }
        }
    }


    private void RadiantHelmTrackKillUpgrades(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
    {
        if (damageReport != null)
        {
            if (damageReport.victimIsBoss && damageReport.victimIsChampion)
            {
                if (damageReport.attackerMaster)
                {
                    if (damageReport.attackerMaster.inventory?.currentEquipmentIndex == EquipmentDef.equipmentIndex)
                    {
                        RadiantHelmKillTracker[damageReport.attackerMaster]++;
                    }
                }
            }
        }
        orig(self, damageReport);
    }

    private void RemoveController(On.RoR2.CharacterBody.orig_OnEquipmentLost orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef && self)
        {
            var controller = self.gameObject.GetComponent<RadiantHelmBehavior>();
            if (controller)
            {
                UnityEngine.Object.Destroy(controller);
            }
        }
        orig(self, equipmentDef);
    }

    private void AddController(On.RoR2.CharacterBody.orig_OnEquipmentGained orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef && self)
        {
            if (!self.gameObject.GetComponent<RadiantHelmBehavior>())
            {
                var controller = self.gameObject.AddComponent<RadiantHelmBehavior>();
                controller.body = self;
                controller.enabled = true;
                if (!RadiantHelmKillTracker.TryGetValue(self.master, out _))
                {
                    RadiantHelmKillTracker[self.master] = 0;
                }
            }
        }
        orig(self, equipmentDef);
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var obj = slot.characterBody.gameObject;
            var controller = obj.GetComponent<RadiantHelmBehavior>();
            if (controller)
            {
                var entityStateMachine = controller.RadiantHelmControllerObject?.GetComponent<EntityStateMachine>();
                if (entityStateMachine)
                {
                    bool flag = entityStateMachine.state.GetType() == typeof(Idle);
                    if (flag)
                    {
                        entityStateMachine.SetNextState(new RadiantHelmBlink());
                        slot.characterBody.skillLocator.primary.rechargeStopwatch += slot.characterBody.skillLocator.primary.finalRechargeInterval * RadiantHelmActivationCDR;
                        slot.characterBody.skillLocator.secondary.rechargeStopwatch += slot.characterBody.skillLocator.secondary.finalRechargeInterval * RadiantHelmActivationCDR;
                        slot.characterBody.skillLocator.utility.rechargeStopwatch += slot.characterBody.skillLocator.utility.finalRechargeInterval * RadiantHelmActivationCDR;
                        slot.characterBody.skillLocator.special.rechargeStopwatch += slot.characterBody.skillLocator.special.finalRechargeInterval * RadiantHelmActivationCDR;
                        return true;
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
            CreateEquipment();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 10f, "How many seconds will this equipment's cooldown be?");
        RadiantHelmBaseBlinkDistance = config.ActiveBind("Equipment: " + EquipmentName, "Blink distance at base speed", 20f, "Without any movement items and assuming 7 base movement speed (true for most survivors), the blink will teleport the user this many meters at the direction the camera is looking at.");

        RadiantHelmActivationCDR = config.ActiveBind("Equipment: " + EquipmentName, "Remaining cooldown reduction on skills on activation", 0.25f, "By what % should remaining cooldown on all skills be reduced upon activating the " + EquipmentName + "? (0.25 = 25%)");

        RadiantHelmBossDmgBoost = config.ActiveBind("Equipment: " + EquipmentName, "Bonus damage against large monsters", 0.2f, "How much should damage against large monsters be increased by while holding the " + EquipmentName + "? (0.2 = 20%)");

        RadiantHelmStatBoost = config.ActiveBind("Equipment: " + EquipmentName, "Boost to all stats", 0.2f, "By what % should ALL stats be increased while holding the " + EquipmentName + "? (0.2 = 20%)");

        RadiantHelmFirstUpgradeKillThreshold = config.ActiveBind("Equipment: " + EquipmentName, "Total large boss kills needed to unlock first upgrade", 1, "This many large bosses need to be defeated to boost damage and proc coefficient against large monsters. \"Large boss\" refers to any large monster with a red health bar.");
        RadiantHelmSecondUpgradeKillThreshold = config.ActiveBind("Equipment: " + EquipmentName, "Total large boss kills needed to unlock second upgrade", 3, "This many large bosses need to be defeated to halve the cooldown of " + EquipmentName + " (reminder that only this can change the base cooldown on this equipment). \"Large boss\" refers to any large monster with a red health bar.");
        RadiantHelmThirdUpgradeKillThreshold = config.ActiveBind("Equipment: " + EquipmentName, "Total large boss kills needed to unlock third upgrade", 5, "This many large bosses need to be defeated to increase all stats. \"Large boss\" refers to any large monster with a red health bar.");
    }

    public class RadiantHelmBehavior : MonoBehaviour
    {
        public GameObject RadiantHelmControllerObject;

        public CharacterBody body;
        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            ConstructController();
            RadiantHelmControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
        }

        private void ConstructController()
        {
            var controller = GeneralUtils.CreateBlankPrefab("RadiantHelmController", true);
            controller.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

            NetworkedBodyAttachment networkedBodyAttachment = controller.AddComponent<NetworkedBodyAttachment>();
            networkedBodyAttachment.shouldParentToAttachedBody = true;
            networkedBodyAttachment.forceHostAuthority = false;

            EntityStateMachine entityStateMachine = controller.AddComponent<EntityStateMachine>();
            entityStateMachine.initialStateType = entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(Idle));

            NetworkStateMachine networkStateMachine = controller.AddComponent<NetworkStateMachine>();
            networkStateMachine.SetFieldValue("stateMachines", new EntityStateMachine[] {
                entityStateMachine
            });

            RadiantHelmControllerObject = Instantiate(controller);
        }

        private void OnDisable()
        {
            if (RadiantHelmControllerObject)
            {
                var entityStateMachine = RadiantHelmControllerObject.GetComponent<EntityStateMachine>();
                bool flag = entityStateMachine.state.GetType() == typeof(RadiantHelmBlink);
                if (flag)
                {
                    entityStateMachine.SetNextState(new Idle());
                }
                Destroy(RadiantHelmControllerObject);
                RadiantHelmControllerObject = null;
            }
        }
    }
}
