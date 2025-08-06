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

    public override string ItemLangTokenName => "BIRTHDAY_BALLOON";

    public override string ItemPickupDesc => "Hold 'Jump' while falling to fall slower and periodically heal.";

    public override string ItemFullDesc => $"Hold 'Jump' while falling to reduce gravity and <style=cIsHealing>heal</style> for <style=cIsHealing>{BirthdayBalloonHealInit}</style> <style=cStack>(+{BirthdayBalloonHealStack} per stack)</style> per {BirthdayBalloonHealInterval}s.";

    public override string ItemLore => "Another year has gone by and I sit here surrounded by birthday balloons. Another year of nothing. The anxieties, the fears, they all grow but amount to nothing. When every year is worse than the rest, there is nothing to do but fear anyway, and I find myself alone with that nothing.\r\n\r\n" +
        "I have spent all my years chasing nothing. Working for things just because they were the option I hated least. And now that nothing is left as an alternative, I find myself alone with that nothing.\r\n\r\n" +
        "I never had the belief that I would get to achieve my dreams, I still don't. But belief and hope are separate things, and a small part of me still hopes. Still, hope is nothing without work, and therefore I find myself alone with that nothing.\r\n\r\n" +
        "As death draws ever nearer, there is less and less I feel passionately about, and that scares me. It scares me to be surrounded by nothing I love, and have nothing that I love. And once again, I find myself alone with that nothing.\r\n\r\n" +
        "- Excerpt from the notes of Atticus Gray, 23rd century writer";

    public override ItemTierDef ItemTierDef => Addressables.LoadAssetAsync<ItemTierDef>("RoR2/Base/Common/Tier2Def.asset").WaitForCompletion();

    public override ItemTag[] Category => [ItemTag.Utility, ItemTag.Healing];

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        DisplayRules = new ItemDisplayRuleDict(null);
        return DisplayRules;
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
        BirthdayBalloonController = GeneralUtils.CreateBlankPrefab("BirthdayBalloonController", true);
        BirthdayBalloonController.GetComponent<NetworkIdentity>().localPlayerAuthority = true;

        NetworkedBodyAttachment networkedBodyAttachment = BirthdayBalloonController.AddComponent<NetworkedBodyAttachment>();
        networkedBodyAttachment.shouldParentToAttachedBody = true;
        networkedBodyAttachment.forceHostAuthority = false;

        EntityStateMachine entityStateMachine = BirthdayBalloonController.AddComponent<EntityStateMachine>();
        entityStateMachine.initialStateType = entityStateMachine.mainStateType = new SerializableEntityStateType(typeof(BirthdayBalloonIdle));

    }
}

