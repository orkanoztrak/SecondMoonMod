using EntityStates;
using R2API;
using RoR2;
using RoR2.Items;
using SecondMoon.EntityStates.Items.Tier2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Items.Tier2.BirthdayBalloon;

public class BirthdayBalloon : Item<BirthdayBalloon>
{
    public static float BirthdayBalloonHealInit = 3f;
    public static float BirthdayBalloonHealStack = 2f;
    public static GameObject BirthdayBalloonController;

    public override string ItemName => "Birthday Balloon";

    public override string ItemLangTokenName => "BIRTHDAY_BALLOON";

    public override string ItemPickupDesc => "Test";

    public override string ItemFullDesc => "Test";

    public override string ItemLore => "Test";

    public override ItemTier ItemTier => ItemTier.Tier2;

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
        Hooks();
    }
}

