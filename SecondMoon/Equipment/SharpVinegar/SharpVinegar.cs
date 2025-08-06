using BepInEx.Configuration;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Equipment;
using SecondMoon.BuffsAndDebuffs.Debuffs.PureDebuffs.Equipment;
using SecondMoon.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using static SecondMoon.Equipment.EssenceChanneler.EssenceChanneler;

namespace SecondMoon.Equipment.SharpVinegar;

public class SharpVinegar : Equipment<SharpVinegar>
{
    public static ConfigOption<float> SharpVinegarBuffDuration;
    public static ConfigOption<float> SharpVinegarBonusDmgActive;
    public static ConfigOption<float> SharpVinegarBonusMSActive;
    public static ConfigOption<float> SharpVinegarCCASConversion;

    public override string EquipmentName => "Sharp Vinegar";

    public override string EquipmentLangTokenName => "SHARP_VINEGAR";

    public override string EquipmentPickupDesc => "Increase damage, additionally increase attack speed based on critical chance... <color=#FF7F7F>BUT disable critical strikes while active and deal no damage afterwards.</color>\n";

    public override string EquipmentFullDescription => $"For <style=cIsUtility>{SharpVinegarBuffDuration}s</style>, increase <style=cIsDamage>base damage</style> by <style=cIsDamage>{SharpVinegarBonusDmgActive * 100}%</style>, <style=cIsUtility>movement speed</style> by <style=cIsUtility>{SharpVinegarBonusMSActive * 100}%</style> " +
        $"and for every <style=cIsDamage>1% critical chance (up to 100%)</style>, increase <style=cIsDamage>attack speed</style> by <style=cIsDamage>{1 * SharpVinegarCCASConversion}% (+{0.01 * SharpVinegarCCASConversion}% per 1% critical damage increase)</style>. " +
        $"<color=#FF7F7F>You cannot critically strike while this effect is active, and for <style=cIsUtility>{SharpVinegarBuffDuration / 2}s</style> after it expires, you cannot deal damage.</color>";

    public override string EquipmentLore => $"Item Log: 60052\r\n\r\n" +
        $"Identification: Vinegar Bottle\r\n\r\n" +
        $"Notes:\r\n\r\n" +
        $"Identification is tentative, as the looks of the substance differ greatly from widely known variants of vinegar, yet the chemical structure is matching with 99% accuracy.\r\n\r\n" +
        $"Extremely volatile and corrosive. The bottle was opened in isolated and ideal conditions, yet its vapor jeopardized the health of 3 of the 5 scientists present in the room. Furthermore, attempts to transfer it to different containers from the one it was originally in were futile, as the containers quickly started showing signs of deterioration.\r\n\r\n" +
        $"Immediate effects on victims included increased physical capabilities, yet also irritation and emotional instability. Near-lethal lethargy and weakness followed afterwards. These victims were strangely not averse to receiving further exposure - which naturally wasn't provided.\r\n\r\n" +
        $"A method of safe disposal hasn't been found yet, and potential uses can be dangerous. Should only be handled by the science team if possible, and interactions with the item should be kept at a minimum.";

    public override bool IsLunar => true;
    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;

    }

    public override void Hooks()
    {
        On.RoR2.CharacterBody.RemoveBuff_BuffIndex += SharpVinegarAddDebuff;
    }

    private void SharpVinegarAddDebuff(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
    {
        orig(self, buffType);
        if (buffType == Sharp.instance.BuffDef.buffIndex)
        {
            self.AddTimedBuffAuthority(Blunt.instance.BuffDef.buffIndex, SharpVinegarBuffDuration / 2);
        }
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            slot.characterBody.AddTimedBuffAuthority(Sharp.instance.BuffDef.buffIndex, SharpVinegarBuffDuration);
            return true;
        }
        return false;
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
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 30f, "How many seconds will this equipment's cooldown be?");
        SharpVinegarBuffDuration = config.ActiveBind("Equipment: " + EquipmentName, Sharp.instance.Name + " buff duration", 10f, "How many seconds should " + Sharp.instance.Name + " last?");
        SharpVinegarBonusDmgActive = config.ActiveBind("Equipment: " + EquipmentName, "Active damage boost", 0.5f, "How much should " + Sharp.instance.Name + " increase damage by? (0.5 = 50%)");
        SharpVinegarBonusMSActive = config.ActiveBind("Equipment: " + EquipmentName, "Active movement boost", 0.5f, "How much should " + Sharp.instance.Name + " increase movement speed by? (0.5 = 50%)");
        SharpVinegarCCASConversion = config.ActiveBind("Equipment: " + EquipmentName, "Conversion of critical chance to attack speed", 1f, "By default, conversion happens so that the disabling of critical hits does not reduce or increase damage output. Attack speed boost will be multiplied by the value of this config option.");
    }
}
