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
using BepInEx.Configuration;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Items.Tier2.BirthdayBalloon;

public class BirthdayBalloon : Item<BirthdayBalloon>
{
    public static ConfigOption<float> BirthdayBalloonHealInit;
    public static ConfigOption<float> BirthdayBalloonHealStack;
    public static ConfigOption<float> BirthdayBalloonHealInterval;

    public static GameObject BirthdayBalloonController;

    public override string ItemName => "Birthday Balloon";

    public override string ItemLangTokenName => "SECONDMOONMOD_BIRTHDAY_BALLOON";

    public override string ItemPickupDesc => "Hold 'Jump' while falling to reduce gravity and periodically heal.";

    public override string ItemFullDesc => $"Hold 'Jump' while falling to reduce gravity and <style=cIsHealing>heal</style> for <style=cIsHealing>{BirthdayBalloonHealInit}</style> <style=cStack>(+{BirthdayBalloonHealStack} per stack)</style> per {BirthdayBalloonHealInterval}s.";

    public override string ItemLore => "Test";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {

    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateItem();
            CreateController();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        BirthdayBalloonHealInit = config.ActiveBind("Item: " + ItemName, "Heal with one " + ItemName, 2f, "How much should health be restored by with one " + ItemName + "?");
        BirthdayBalloonHealStack = config.ActiveBind("Item: " + ItemName, "Heal per stack after one " + ItemName, 2f, "How much should health be restored by per stack of " + ItemName + " after one?");
        BirthdayBalloonHealInterval = config.ActiveBind("Item: " + ItemName, "Heal interval", 0.5f, "" + ItemName + " heals per n seconds while floating, where n is the value of this config option.");
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

