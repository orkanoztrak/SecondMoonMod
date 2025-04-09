using R2API.Utils;
using R2API;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using SecondMoon.MyEntityStates.Items.Prototype;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.Utils;

namespace SecondMoon.Items.Prototype.FlailOfMass;

public class FlailOfMassBodyBehavior : BaseItemBodyBehavior
{
    private GameObject FlailOfMassControllerObject;

    [ItemDefAssociation(useOnServer = true, useOnClient = false)]
    private static ItemDef GetItemDef()
    {
        return FlailOfMass.instance.ItemDef;
    }

    private void OnEnable()
    {
        ConstructController();
        FlailOfMassControllerObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(body.gameObject);
    }

    private void ConstructController()
    {
        var controller = GeneralUtils.CreateBlankPrefab("FlailOfMassController", true);
        controller.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

        NetworkedBodyAttachment networkedBodyAttachment = controller.AddComponent<NetworkedBodyAttachment>();
        networkedBodyAttachment.shouldParentToAttachedBody = true;
        networkedBodyAttachment.forceHostAuthority = false;

        EntityStateMachine entityStateMachine = controller.AddComponent<EntityStateMachine>();
        entityStateMachine.initialStateType = entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(FlailOfMassBuildingMomentum));

        NetworkStateMachine networkStateMachine = controller.AddComponent<NetworkStateMachine>();
        networkStateMachine.SetFieldValue("stateMachines", new EntityStateMachine[] {
                entityStateMachine
            });

        FlailOfMassControllerObject = Instantiate(controller);
    }


    private void OnDisable()
    {
        if (FlailOfMassControllerObject)
        {
            Destroy(FlailOfMassControllerObject);
            FlailOfMassControllerObject = null;
        }
        if (body)
        {
            while (body.GetBuffCount(Momentum.instance.BuffDef.buffIndex) > 0)
            {
                body.RemoveBuff(Momentum.instance.BuffDef);
            }
        }
    }
}
