using System;
using System.Collections.Generic;
using System.Text;
using static RoR2.Items.BaseItemBodyBehavior;
using UnityEngine;
using RoR2.Items;
using RoR2;
using SecondMoon.EntityStates.Items.Tier2;
using EntityStates;
using UnityEngine.Networking;

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
        BirthdayBalloonControllerObject = ConstructController();
        BirthdayBalloonControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
    }

    private GameObject ConstructController()
    {
        var controller = new GameObject();
        controller.AddComponent<NetworkIdentity>();
        controller.AddComponent<NetworkedBodyAttachment>();
        controller.AddComponent<EntityStateMachine>();
        controller.GetComponent<EntityStateMachine>().initialStateType = new SerializableEntityStateType(typeof(BirthdayBalloonIdle));
        controller.GetComponent<EntityStateMachine>().mainStateType = new SerializableEntityStateType(typeof(BirthdayBalloonFloat));
        controller.AddComponent<RoR2.NetworkStateMachine>();
        return controller;
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
