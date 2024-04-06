using EntityStates;
using R2API;
using RoR2;
using RoR2.Items;
using SecondMoon.MyEntityStates.Items.Tier2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SecondMoon.Utils;
using UnityEngine.Networking;

namespace SecondMoon.Items.Tier2.BirthdayBalloon;

public class BirthdayBalloon : Item<BirthdayBalloon>
{
    public static float BirthdayBalloonHealInit = 2f;
    public static float BirthdayBalloonHealStack = 2f;
    public static GameObject BirthdayBalloonController;

    public override string ItemName => "Birthday Balloon";

    public override string ItemLangTokenName => "SECONDMOONMOD_BIRTHDAY_BALLOON";

    public override string ItemPickupDesc => $"Hold 'Jump' while falling to reduce gravity and periodically heal.";

    public override string ItemFullDesc => $"Hold 'Jump' while falling to reduce gravity and <style=cIsHealing>heal</style> for <style=cIsHealing>{BirthdayBalloonHealInit}</style> <style=cStack>(+{BirthdayBalloonHealStack} per stack)</style> per 0.5s.";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }  

    public override void Hooks()
    {
        
    }

    public override void Init()
    {
        CreateLang();
        CreateItem();
        CreateController();
        Hooks();
    }

    private void CreateController()
    {
        BirthdayBalloonController = Utils.Utils.CreateBlankPrefab("BirthdayBalloonController", true);
        BirthdayBalloonController.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

        NetworkedBodyAttachment networkedBodyAttachment = BirthdayBalloonController.AddComponent<NetworkedBodyAttachment>();
        networkedBodyAttachment.shouldParentToAttachedBody = true;
        networkedBodyAttachment.forceHostAuthority = false;

        EntityStateMachine entityStateMachine = BirthdayBalloonController.AddComponent<EntityStateMachine>();
        entityStateMachine.initialStateType = entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(BirthdayBalloonIdle));

    }
}

