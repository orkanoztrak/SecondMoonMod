using System;
using System.Collections.Generic;
using System.Text;
using static RoR2.Items.BaseItemBodyBehavior;
using UnityEngine;
using RoR2.Items;
using RoR2;
using SecondMoon.MyEntityStates.Items.Tier2;
using EntityStates;
using UnityEngine.Networking;
using R2API.Utils;
using R2API;

namespace SecondMoon.Items.Tier2.BirthdayBalloon;

public class BirthdayBalloonBodyBehavior : BaseItemBodyBehavior
{
    private GameObject BirthdayBalloonControllerObject;

    [ItemDefAssociation(useOnServer = true, useOnClient = false)]
    private static ItemDef GetItemDef()
    {
        return BirthdayBalloon.instance.ItemDef;
    }

    private void OnEnable()
    {
        ConstructController();
        BirthdayBalloonControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
    }
    private void ConstructController()
    {
        var controller = CreateBlankPrefab("BirthdayBalloonController", true);
        controller.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

        NetworkedBodyAttachment networkedBodyAttachment = controller.AddComponent<NetworkedBodyAttachment>();
        networkedBodyAttachment.shouldParentToAttachedBody = true;
        networkedBodyAttachment.forceHostAuthority = false;

        EntityStateMachine entityStateMachine = controller.AddComponent<EntityStateMachine>();
        entityStateMachine.initialStateType = entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(BirthdayBalloonIdle));

        NetworkStateMachine networkStateMachine = controller.AddComponent<NetworkStateMachine>();
        networkStateMachine.SetFieldValue("stateMachines", new EntityStateMachine[] {
                entityStateMachine
            });

        BirthdayBalloonControllerObject = Instantiate(controller);
    }

    public static GameObject CreateBlankPrefab(string name = "GameObject", bool network = false)
    {
        GameObject gameObject = PrefabAPI.InstantiateClone(new GameObject(name), name, false);
        if (network)
        {
            gameObject.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(gameObject);
        }
        return gameObject;
    }


    private void OnDisable()
    {
        if ((bool)BirthdayBalloonControllerObject)
        {
            Destroy(BirthdayBalloonControllerObject);
            BirthdayBalloonControllerObject = null;
        }
    }

}
