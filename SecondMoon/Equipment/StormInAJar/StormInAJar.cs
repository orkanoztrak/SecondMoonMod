using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SecondMoon.Equipment.BladeOfPetrichor.BladeOfPetrichor;

namespace SecondMoon.Equipment.StormInAJar;

public class StormInAJar : Equipment<StormInAJar>
{
    public static ConfigOption<float> StormInAJarDuration;
    public static ConfigOption<float> StormInAJarDamage;
    public static ConfigOption<float> StormInAJarChainChance;
    public static ConfigOption<float> StormInAJarChainRadius;
    public override string EquipmentName => "Storm-In-a-Jar";

    public override string EquipmentLangTokenName => "STORMINAJAR";

    public override string EquipmentPickupDesc => "Summon a storm that attacks enemies as you attack them.";

    public override string EquipmentFullDescription => "Test";

    public override string EquipmentLore => "Welcome to DataScraper (v3.1.53 – beta branch)\r\n" +
        "$ Scraping memory... done.\r\n" +
        "$ Resolving... done.\r\n" +
        "$ Combing for relevant data... done.\r\n" +
        "Complete!\r\n" +
        "Outputting local audio transcriptions...\r\n\r\n" +
        "How does one capture lightning in a bottle, you might ask. Most believe it to be pure luck. A product of mere circumstance. However, there is more to it. More to luck, even, which is surely part of it.\r\n\r\n" +
        "One factor to take into consideration is skill. Skill to make sure when luck shows her face, one knows how to capitalize. Skill to handle that luck and turn it into something tangible.\r\n\r\n" +
        "Second is hard work. Every day we get lucky in some situations and unlucky in some others. When we work hard for our goals, we increase the amount we spend on that goal, and therefore increase the probability of our luck turning up during our goal-oriented activities. This has been proven by research, done by me.\r\n\r\n" +
        "Finally, experience, that comes from hard work and leads to skill. Experience is the bridge that connects the factors in capturing lightning in a bottle. Now go and do your thing, students. And if you are not satisfied with lightning in a bottle, join my next seminar on capturing a storm in a jar, to realize your full potential!\r\n";

    public GameObject ItemBodyModelPrefab;

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        ItemBodyModelPrefab = PrefabAPI.InstantiateClone(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RandomEquipmentTrigger/DisplayBottledChaos.prefab").WaitForCompletion(), "StormInAJarFollower", false);
        UnityEngine.Object.Destroy(ItemBodyModelPrefab.GetComponent<ItemDisplay>());
        ItemBodyModelPrefab.AddComponent<ItemDisplay>();
        ItemBodyModelPrefab.GetComponent<ItemDisplay>().rendererInfos = GeneralUtils.ItemDisplaySetup(ItemBodyModelPrefab);

        ItemDisplayRuleDict rules = new ItemDisplayRuleDict();
        rules.Add("mdlCommandoDualies", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlHuntress", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlToolbot", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(-9.05238F, -2F, 5.00013F),
                    localAngles = new Vector3(270F, 0F, 0F),
                    localScale = new Vector3(1F, 1F, 1F)
                }
        });
        rules.Add("mdlEngi", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(1.28301F, -0.34921F, -1.00009F),
                    localAngles = new Vector3(270F, 0F, 0F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
        });
        rules.Add("mdlMage", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlMerc", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlTreebot", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlLoader", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.5f, 0f, -1f),
                    localAngles = new Vector3(-90f, 0f, 0f),
                    localScale = new Vector3(0.15f, 0.15f, 0.15f)
                }
        });
        rules.Add("mdlCroco", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(5F, 0F, 10F),
                    localAngles = new Vector3(270F, 0F, 0F),
                    localScale = new Vector3(1F, 1F, 1F)
                }
        });
        rules.Add("mdlCaptain", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.70726F, -0.17282F, -1.00137F),
                    localAngles = new Vector3(270F, 0F, 0F),
                    localScale = new Vector3(0.15F, 0.15F, 0.15F)
                }
        });
        rules.Add("mdlBandit2", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Base",
                    localPos = new Vector3(0.039F, -0.8778F, -0.5109F),
                    localAngles = new Vector3(-90f, 0F, 0F),
                    localScale = new Vector3(0.1F, 0.1F, 0.1F)
                }
        });
        rules.Add("CHEF", new ItemDisplayRule[]
        {
                new ItemDisplayRule
                {
                    ruleType = ItemDisplayRuleType.ParentedPrefab,
                    followerPrefab = ItemBodyModelPrefab,
                    childName = "Chef",
                    localPos = new Vector3(0.03661F, 0.01391F, 0.03791F),
                    localAngles = new Vector3(90F, 0F, 0F),
                    localScale = new Vector3(0.00424F, 0.00424F, 0.00424F)
                }
        });
        return rules;
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var obj = slot.characterBody.gameObject;
            if (!obj.GetComponent<StormInAJarController>())
            {
                var controller = obj.AddComponent<StormInAJarController>();
                controller.enabled = true;
                return true;
            }
        }
        return false;
    }

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.ProcessHitEnemy += StormInAJarZapEnemy;
    }

    private void StormInAJarZapEnemy(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            var victimComponent = victim.GetComponent<HealthComponent>();

            if (attackerBody && victimComponent)
            {
                if (attackerBody.master)
                {
                    var controller = damageInfo.attacker.GetComponent<StormInAJarController>();
                    if (controller)
                    {
                        DamageInfo stormProc = new DamageInfo
                        {
                            damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, StormInAJarDamage),
                            damageColorIndex = DamageColorIndex.Item,
                            damageType = DamageType.Generic,
                            attacker = damageInfo.attacker,
                            crit = damageInfo.crit,
                            force = Vector3.zero,
                            inflictor = null,
                            position = damageInfo.position,
                            procCoefficient = 0
                        };
                        victimComponent.TakeDamage(stormProc);
                        if (Util.CheckRoll(StormInAJarChainChance * damageInfo.procCoefficient, attackerBody.master))
                        {
                            List<HurtBox> targets = GeneralUtils.FindCountHurtboxesInRangeAroundTarget(0, StormInAJarChainRadius, attackerBody.teamComponent.teamIndex, victim.transform.position, victim);
                            foreach (HurtBox target in targets)
                            {
                                if (target)
                                {
                                    if (target.healthComponent)
                                    {
                                        if (target.healthComponent.body)
                                        {
                                            DamageInfo stormChainProc = new DamageInfo
                                            {
                                                damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, StormInAJarDamage),
                                                damageColorIndex = DamageColorIndex.Item,
                                                damageType = DamageType.Generic,
                                                attacker = damageInfo.attacker,
                                                crit = damageInfo.crit,
                                                force = Vector3.zero,
                                                inflictor = null,
                                                position = target.healthComponent.body.gameObject.transform.position,
                                                procCoefficient = 0
                                            };
                                            target.healthComponent.TakeDamage(stormChainProc);
                                        }
                                    }
                                }
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
            CreateEquipment();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 60f, "How many seconds will this equipment's cooldown be?");
        StormInAJarDuration = config.ActiveBind("Equipment: " + EquipmentName, "Duration of active", 10f, "How many seconds does this equipment's active effect last?");
        StormInAJarDamage = config.ActiveBind("Equipment: " + EquipmentName, "Damage of the proc", 0.5f, "What % of TOTAL damage should the proc do? (0.5 = 50%)");
        StormInAJarChainChance = config.ActiveBind("Equipment: " + EquipmentName, "Chance for chain lightning", 25f, "The % chance for lightning to chain to all enemies nearby.");
        StormInAJarChainRadius = config.ActiveBind("Equipment: " + EquipmentName, "Chain lightning radius", 24f, "Chain lightning will strike all enemies within this many meters.");
    }

    public class StormInAJarController : MonoBehaviour
    {
        public float remainingDuration;

        private void Awake()
        {
            enabled = false;
        }

        private void Start()
        {
            remainingDuration = StormInAJarDuration;
        }

        private void FixedUpdate()
        {
            if (remainingDuration <= 0)
            {
                Destroy(this);
            }
            remainingDuration -= Time.fixedDeltaTime;
        }
    }
}