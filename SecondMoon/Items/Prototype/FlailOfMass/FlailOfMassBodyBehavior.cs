using R2API.Utils;
using R2API;
using RoR2;
using RoR2.Items;
using SecondMoon.MyEntityStates.Items.Tier2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates;
using SecondMoon.MyEntityStates.Items.Prototype;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;

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
        var controller = CreateBlankPrefab("FlailOfMassController", true);
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
        if ((bool)FlailOfMassControllerObject)
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
