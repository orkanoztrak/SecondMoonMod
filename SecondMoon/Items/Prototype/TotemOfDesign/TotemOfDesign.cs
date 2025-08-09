using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.PulseController;

namespace SecondMoon.Items.Prototype.TotemOfDesign;

public class TotemOfDesign : Item<TotemOfDesign>
{
    public static ConfigOption<float> TotemOfDesignSkillFailChanceInit;
    public static ConfigOption<float> TotemOfDesignSkillFailChanceStack;
    public static ConfigOption<float> TotemOfDesignFailBlastRadius;
    public static ConfigOption<float> TotemOfDesignFailBlastHealthScaling;
    public override string ItemName => "Totem of Design";

    public override string ItemLangTokenName => "TOTEM_OF_DESIGN";

    public override string ItemPickupDesc => "Enemy skills have a chance to fail. Your skills have a chance to not go on cooldown.";

    public override string ItemFullDesc => $"Enemy skills have a <color=#7CFDEA>{TotemOfDesignSkillFailChanceInit * 100}% <style=cStack>(+{TotemOfDesignSkillFailChanceStack * 100}% per stack)</style> chance to fail</color>, going on cooldown and dealing <style=cIsDamage>{TotemOfDesignFailBlastHealthScaling * 100}% of maximum health as damage</style> to their allies in a <style=cIsDamage>{TotemOfDesignFailBlastRadius}m</style> radius.\r\n\r\nYour skills have a <color=#7CFDEA>20% <style=cStack>(+20% per stack)</style> chance to not go on cooldown.</color>";

    public override string ItemLore => $"Have I ever told you why I love Design?\r\n\r\n" +
        $"Unlike the other compounds, Design does not have its own form. It is the creator's own skills and vision that give it form. In this regard, Design is the most abstract compound that also tells the most about the contraption and its maker.\r\n\r\n" +
        $"But how would an abundance of Design even work? After our last experiment, I got to thinking.\r\n\r\n" +
        $"This is exhilarating, brother. It is a challenge to our abilities.\r\n\r\n" +
        $"I gather a modest amount of Mass, Blood and Soul. After all, material is needed to build, and for Design to exist.\r\n\r\n" +
        $"I decided on a small likeness of a humanoid creature. It represents the apex of intelligence, and infinite possibility.\r\n\r\n" +
        $"Its purpose? To find faults and strengths in other Designs. Those will always exist, and this contraption will always detect them.\r\n\r\n" +
        $"This will be a reminder that greater heights always exist. Dormancy is death.\r\n\r\n" +
        $"The last contraption was a gift to you. This one... will be a gift to myself.";

    public override ItemTierDef ItemTierDef => TierPrototype.instance.ItemTierDef;

    public override ItemTag[] Category => [ItemTag.Damage, ItemTag.Healing, ItemTag.Utility];

    private static int TotemOfDesignPlayersStackTracker = 0;
    private static int TotemOfDesignMonstersStackTracker = 0;


    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnInventoryChanged += TotemOfDesignUpdateTracker;
        On.RoR2.GenericSkill.OnExecute += TotemOfDesignCauseSkillFailure;
    }

    private void TotemOfDesignCauseSkillFailure(On.RoR2.GenericSkill.orig_OnExecute orig, GenericSkill self)
    {
        if (self.characterBody && self.skillDef && self.finalRechargeInterval > 0.5f)
        {
            var stackCount = 0;
            TeamIndex enemyTeam = TeamIndex.None;
            if (self.characterBody.teamComponent)
            {
                switch (self.characterBody.teamComponent.teamIndex)
                {
                    case TeamIndex.Player:
                        enemyTeam = TeamIndex.Monster;
                        stackCount = TotemOfDesignMonstersStackTracker;
                        break;

                    case TeamIndex.Monster:
                        enemyTeam = TeamIndex.Player;
                        stackCount = TotemOfDesignPlayersStackTracker;
                        break;
                }
                if (stackCount > 0)
                {
                    var roll = GeneralUtils.HyperbolicScaling(TotemOfDesignSkillFailChanceInit + (stackCount - 1) * TotemOfDesignSkillFailChanceStack);
                    if (Util.CheckRoll(roll * 100))
                    {
                        var state = self.characterBody.GetComponent<SetStateOnHurt>();
                        if (state)
                        {
                            state.SetStun(2f);
                        }
                        CreatePulseAttack(self.characterBody, enemyTeam);
                        self.rechargeStopwatch = 0f;
                        self.stock -= self.skillDef.stockToConsume;
                        if (self.skillDef.cancelSprintingOnActivation)
                        {
                            self.characterBody.isSprinting = false;
                        }
                        return;
                    }
                }
            }
        }
        orig(self);
        static void CreatePulseAttack(CharacterBody characterBody, TeamIndex teamIndex)
        {
            Debug.Log("called");
            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"), new EffectData
            {
                origin = characterBody.transform.position,
                scale = TotemOfDesignFailBlastRadius,
            }, true);
            new BlastAttack
            {
                procChainMask = default,
                procCoefficient = 0,
                attacker = null,
                inflictor = null,
                teamIndex = teamIndex,
                baseDamage = characterBody.healthComponent.fullCombinedHealth * TotemOfDesignFailBlastHealthScaling,
                baseForce = 100f,
                falloffModel = BlastAttack.FalloffModel.Linear,
                crit = false,
                radius = TotemOfDesignFailBlastRadius,
                position = characterBody.transform.position,
                damageColorIndex = DamageColorIndex.Item,
            }.Fire();
        }
    }


    private void TotemOfDesignUpdateTracker(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
    {
        int num = 0;
        ReadOnlyCollection<CharacterMaster> readOnlyInstancesList = CharacterMaster.readOnlyInstancesList;
        for (int i = 0; i < readOnlyInstancesList.Count; i++)
        {
            CharacterMaster characterMaster = readOnlyInstancesList[i];
            if (characterMaster.teamIndex == TeamIndex.Player && characterMaster.hasBody && characterMaster.playerCharacterMasterController)
            {
                num += characterMaster.inventory.GetItemCount(ItemDef);
            }
        }
        TotemOfDesignPlayersStackTracker = num;
        TotemOfDesignMonstersStackTracker = Util.GetItemCountForTeam(TeamIndex.Monster, ItemDef.itemIndex, requiresAlive: true, requiresConnected: false);
        orig(self);

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
        TotemOfDesignSkillFailChanceInit = config.ActiveBind("Item: " + ItemName, "Skill fail chance with one " + ItemName, 0.1f, "What % of skills should fail with one " + ItemName + " on the opposing team? This scales hyperbolically (0.1 = 10%, refer to Tougher Times on the wiki).");
        TotemOfDesignSkillFailChanceStack = config.ActiveBind("Item: " + ItemName, "Skill fail chance per stack of " + ItemName + " after one", 0.1f, "What % of skills should fail per stack of " + ItemName + " after one on the opposing team? This scales hyperbolically (0.1 = 10%, refer to Tougher Times on the wiki).");
        TotemOfDesignFailBlastRadius = config.ActiveBind("Item: " + ItemName, "Failed skill blast radius", 15f, "If a skill fails, the resulting AOE will have a raidus of this many meters.");
        TotemOfDesignFailBlastHealthScaling = config.ActiveBind("Item: " + ItemName, "Failed skill blast health percent", 0.1f, "What % of the skill user's health pool should be dealt as damage to enemies in an area? (0.1 = 10%)");
    }
}
