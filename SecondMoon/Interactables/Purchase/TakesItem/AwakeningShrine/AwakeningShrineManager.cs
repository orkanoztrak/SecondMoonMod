using RoR2;
using SecondMoon.MyEntityStates.Interactables;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;

public class AwakeningShrineManager : NetworkBehaviour
{
    public PickupPickerController pickupPickerController;

    private Interactor interactor;

    public CombatDirector bossDirector;

    public EntityStateMachine mainStateMachine;

    private Xoroshiro128Plus bossRng;

    private CharacterMaster bossMaster;

    private PickupIndex dormantToAwaken;

    public void Awake()
    {
        bossDirector = GetComponent<CombatDirector>();
        bossRng = new Xoroshiro128Plus(Run.instance.seed);
        mainStateMachine = GetComponent<EntityStateMachine>();
    }

    public void HandleSelection(int selection)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        GetComponent<NetworkUIPromptController>().ClearParticipant();

        if (interactor)
        {
            var body = interactor.GetComponent<CharacterBody>();
            PickupDef pickupDef = PickupCatalog.GetPickupDef(new PickupIndex(selection));
            if (AwakeningShrine.PrototypeItemIndexPairs.ContainsKey(pickupDef.itemIndex) || AwakeningShrine.PrototypeEquipmentIndexPairs.ContainsKey(pickupDef.itemIndex))
            {
                if (body.master)
                {
                    if (body.master.inventory.GetItemCount(pickupDef.itemIndex) > 0)
                    {
                        body.master.inventory.RemoveItem(pickupDef.itemIndex);
                        mainStateMachine.SetNextState(new AwakeningShrinePrepareBossSpawn());
                    }
                }
            }
        }
    }

    public void HandleInteraction(Interactor interactor)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        this.interactor = interactor;
        List<PickupPickerController.Option> options = new List<PickupPickerController.Option>();
        var body = interactor.GetComponent<CharacterBody>();
        if (body)
        {
            if (body.master)
            {
                foreach (var dormant in AwakeningShrine.PrototypeItemIndexPairs.Keys)
                {
                    if (body.master.inventory.GetItemCount(dormant) > 0)
                    {
                        options.Add(new PickupPickerController.Option
                        {
                            available = body.master.inventory.GetItemCount(dormant) > 0,
                            pickupIndex = PickupCatalog.FindPickupIndex(dormant)
                        });
                    }
                }
                foreach (var dormant in AwakeningShrine.PrototypeEquipmentIndexPairs.Keys)
                {
                    if (body.master.inventory.GetItemCount(dormant) > 0)
                    {
                        options.Add(new PickupPickerController.Option
                        {
                            available = body.master.inventory.GetItemCount(dormant) > 0,
                            pickupIndex = PickupCatalog.FindPickupIndex(dormant)
                        });
                    }
                }
            }
            pickupPickerController.SetOptionsServer(options.ToArray());
        }
    }
}