using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Equipment;
using SecondMoon.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Equipment.Stoppedwatch;

public class Stoppedwatch : Equipment<Stoppedwatch>
{
    public static ConfigOption<float> StoppedwatchDuration;

    public override string EquipmentName => "Stoppedwatch";

    public override string EquipmentLangTokenName => "GOD_WATCH";

    public override string EquipmentPickupDesc => StoppedwatchDuration == 1? $"Remove skill cooldowns and become invincible for {StoppedwatchDuration} second." : $"Remove skill cooldowns and become invincible for {StoppedwatchDuration} seconds.";

    public override string EquipmentFullDescription => $"For <style=cIsUtility>{StoppedwatchDuration}s</style>, skills do not go on cooldown upon being used and you take no damage.";

    public override string EquipmentLore => $"\"Check this out! I got this new model. Supposed to have even better accuracy than the last, it was a steal too. Let's get this test running.\"\r\n\r\n" +
        $"\"Thank god. That last one, we had to add 0.634 seconds to every result. The lag was unacceptable! Let's see how this one fares. Systems OK. Ready-\"\r\n\r\n" +
        $"\"Ah, my bad, my hand slipped. I'll reset it right away - wait, this isn't even counting up? Damn that man, he sold me a broken product! What's with my luck when it comes to these anyway, first one lags, this one doesn't even work... you know what, you buy it next time. I wouldn't be surprised if the next one I buy blows up in our face or something.\"\r\n\r\n" +
        $"\"-when you are.\"\r\n\r\n" +
        $"\"...Huh?\"\r\n\r\n" +
        $"\"Huh?\"";

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Init(ConfigFile config)
    {
        base.Init(config);
        if (IsEnabled)
        {
            CreateConfig(config);
            CreateLang();
            CreateEquipment();
            Hooks();
        }
    }

    private void CreateConfig(ConfigFile config)
    {
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 100f, "How many seconds will this equipment's cooldown be?");
        StoppedwatchDuration = config.ActiveBind("Equipment: " + EquipmentName, DlrowEht.instance.Name + " buff duration", 3f, "How many seconds should " + DlrowEht.instance.Name + " last?");
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            if (!slot.characterBody.HasBuff(DlrowEht.instance.BuffDef))
            {
                slot.characterBody.AddTimedBuffAuthority(DlrowEht.instance.BuffDef.buffIndex, StoppedwatchDuration);
                return true;
            }
        }
        return false;
    }
}
