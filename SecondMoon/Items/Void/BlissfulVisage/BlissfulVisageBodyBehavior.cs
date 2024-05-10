using RoR2;
using RoR2.CharacterAI;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.Events;
using UnityEngine.Networking;
using static RoR2.CharacterBody;

namespace SecondMoon.Items.Void.BlissfulVisage;

public class BlissfulVisageBodyBehavior : BaseItemBodyBehavior
{
    private static readonly float timeBetweenGhostResummons = BlissfulVisage.BlissfulVisageGhostCooldown;

    private float ghostResummonCooldown;


    [ItemDefAssociation(useOnServer = true, useOnClient = false)]
    private static ItemDef GetItemDef()
    {
        return BlissfulVisage.instance.ItemDef;
    }

    private void OnEnable()
    {
        On.RoR2.GlobalEventManager.OnCharacterDeath += BlissfulVisageReduceGhostTimerOnKill;
    }

    private void BlissfulVisageReduceGhostTimerOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
    {
        if (damageReport?.attackerBody == body)
        {
            ghostResummonCooldown -= BlissfulVisage.BlissfulVisageReduceTimerOnKillInit + ((stack - 1) * BlissfulVisage.BlissfulVisageReduceTimerOnKillStack);
        }
        orig(self, damageReport);
    }

    private void FixedUpdate()
    {
        if (!NetworkServer.active) return;

        int num = stack;
        CharacterMaster bodyMaster = body.master;
        if (!bodyMaster)
        {
            return;
        }
        ghostResummonCooldown -= Time.fixedDeltaTime;
        if (ghostResummonCooldown <= 0f)
        {
            BlissfulVisageTryToSummonVoidGhost(body);
            ghostResummonCooldown = timeBetweenGhostResummons;
        }
    }

    [Server]
    private static void BlissfulVisageTryToSummonVoidGhost(CharacterBody ownerBody)
    {
        if (!NetworkServer.active)
        {
            Debug.LogWarning("[Server] function 'SecondMoon.Items.Void.BlissfulVisage.BlissfulVisageBodyBehavior.TryToSummonVoidGhost(CharacterBody ownerBody)' called on client");
            return;
        }
        if (!ownerBody)
        {
            return;
        }
        GameObject bodyPrefab = BodyCatalog.FindBodyPrefab(ownerBody);
        if (!bodyPrefab)
        {
            return;
        }
        CharacterMaster characterMaster = MasterCatalog.allAiMasters.FirstOrDefault((CharacterMaster master) => master.bodyPrefab == bodyPrefab);
        if (!characterMaster)
        {
            return;
        }

        MasterSummon obj = new MasterSummon
        {
            masterPrefab = characterMaster.gameObject,
            ignoreTeamMemberLimit = false,
            position = ownerBody.footPosition
        };
        CharacterDirection component = ownerBody.GetComponent<CharacterDirection>();
        obj.rotation = (component ? Quaternion.Euler(0f, component.yaw, 0f) : ownerBody.transform.rotation);
        obj.summonerBodyObject = (ownerBody ? ownerBody.gameObject : null);
        obj.inventoryToCopy = ownerBody.inventory;
        obj.useAmbientLevel = null;
        obj.preSpawnSetupCallback = (Action<CharacterMaster>)Delegate.Combine(obj.preSpawnSetupCallback, new Action<CharacterMaster>(PreSpawnSetupVoid));
        obj.loadout = ownerBody.master.loadout;

        CharacterMaster characterMaster2 = obj.Perform();
        if (!characterMaster2)
        {
            return;
        }

        CharacterBody body = characterMaster2.GetBody();
        if ((bool)body)
        {
            EntityStateMachine[] components = body.GetComponents<EntityStateMachine>();
            foreach (EntityStateMachine obj2 in components)
            {
                obj2.initialStateType = obj2.mainStateType;
            }
        }
        void PreSpawnSetupVoid(CharacterMaster master)
        {
            master.inventory.GiveItem(RoR2Content.Items.Ghost);
            master.inventory.GiveItem(BlissfulVisageSuicideComponent.instance.ItemDef);
            master.inventory.GiveEquipmentString("EliteVoidEquipment");

            var driver = master.gameObject.AddComponent<AISkillDriver>();
            driver.customName = "IdleNearLeaderWhenNoEnemies";
            driver.skillSlot = SkillSlot.None;
            driver.maxDistance = 20f;
            driver.moveTargetType = AISkillDriver.TargetType.CurrentLeader;
            driver.movementType = AISkillDriver.MovementType.StrafeMovetarget;

            driver = master.gameObject.AddComponent<AISkillDriver>();
            driver.customName = "ReturnToLeaderWhenNoEnemies";
            driver.skillSlot = SkillSlot.None;
            driver.minDistance = 20f;
            driver.moveTargetType = AISkillDriver.TargetType.CurrentLeader;
            driver.shouldSprint = true;

            master.gameObject.GetComponent<BaseAI>().skillDrivers = master.gameObject.GetComponents<AISkillDriver>();
        }
    }
}