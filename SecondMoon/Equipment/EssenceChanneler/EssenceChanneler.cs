using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Equipment;
using SecondMoon.Equipment;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Equipment.EssenceChanneler;

public class EssenceChanneler : Equipment<EssenceChanneler>
{
    public static ConfigOption<float> EssenceChannelerDuration;

    public static ConfigOption<float> ChannelingBoost;

    public static List<string> EssenceChannelerEliteNamesList = ["bdEliteFire", "bdEliteIce", "bdEliteLightning", "bdElitePoison", "bdEliteHaunted"];
    public static List<BuffDef> EssenceChannelerEliteList = new List<BuffDef>();
    public override string EquipmentName => "Essence Channeler";

    public override string EquipmentLangTokenName => "ESSENCE_CHANNELER";

    public override string EquipmentPickupDesc => $"Briefly gain an increase to all stats. Cycle between the powers of a random classic elite with each use.";

    public override string EquipmentFullDescription => $"For <style=cIsUtility>{EssenceChannelerDuration}s</style>, gain <style=cIsUtility>{ChannelingBoost * 100}%</style> to <style=cIsUtility>ALL stats</style>." +
        $"Gain a random classic <style=cIsDamage>elite power</style> upon using this that changes with each use.";

    public override string EquipmentLore => "The Bellwether is written as having seven sons and seven daughters, each representing a different force of nature. Rain, snow, storms, earthquakes, drought and more.\r\n\r\n" +
        "Each enclave of Bellwether worshippers would mainly worship one child of the Bellwether aside from the Bellwether itself, giving the two equal space in their temples and rituals.\r\n\r\n" +
        "Modern belief systems have more or less driven the old belief of the Bellwether into obsoletion, however it is possible to find enclaves that still partake in the worship of the Bellwether in lesser developed areas of the galaxy.\r\n\r\n" +
        "These enclaves (some may even call them \"cults\") usually operate around a warlike subculture, with a \"blessed\" warrior at the centre. These warriors are given unnatural powers by something the enclaves call an \"essence channeler\", which they believe was sent by the Bellwether itself.\r\n\r\n" +
        "These Warriors hold summits every five years to discuss secret topics, almost like a shadow organization. Nobody except the Warriors are allowed to enter these summits, with utmost secrecy paramount.";

    public override ItemDisplayRuleDict CreateItemDisplayRules()
    {
        displayRules = new ItemDisplayRuleDict(null);
        return displayRules;
    }

    public override void Hooks()
    {
        RoR2Application.onLoad += EssenceChannelerGetElites;
        On.RoR2.CharacterBody.OnEquipmentGained += AddController;
        On.RoR2.CharacterBody.OnEquipmentLost += RemoveController;
    }

    private void RemoveController(On.RoR2.CharacterBody.orig_OnEquipmentLost orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef && self)
        {
            var controller = self.gameObject.GetComponent<EssenceChannelerEliteControllerComponent>();
            if (controller)
            {
                var body = self.gameObject.GetComponent<CharacterBody>();
                if (body)
                {
                    body.RemoveBuff(controller.currentAffix);
                }
                UnityEngine.Object.Destroy(controller);
            }
        }
        orig(self, equipmentDef);
    }

    private void AddController(On.RoR2.CharacterBody.orig_OnEquipmentGained orig, CharacterBody self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef && self)
        {
            self.gameObject.AddComponent<EssenceChannelerEliteControllerComponent>();
        }
        orig(self, equipmentDef);
    }

    private void EssenceChannelerGetElites()
    {
        foreach (var elite in BuffCatalog.buffDefs)
        {
            if (EssenceChannelerEliteNamesList.Contains(elite.name))
            {
                EssenceChannelerEliteList.Add(elite);
            }
        }
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
        Cooldown = config.ActiveBind("Equipment: " + EquipmentName, "Cooldown", 45f, "How many seconds will this equipment's cooldown be?");
        EssenceChannelerDuration = config.ActiveBind("Equipment: " + EquipmentName, Channeling.instance.Name + " buff duration", 6f, "How many seconds should " + Channeling.instance.Name + " last?");
        ChannelingBoost = config.ActiveBind("Equipment: " + EquipmentName, Channeling.instance.Name + " boost", 0.3f, "By what % should ALL stats be increased with " + Channeling.instance.Name + "? (0.3 = 30%)");
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var obj = slot.characterBody.gameObject;
            var controller = obj.GetComponent<EssenceChannelerEliteControllerComponent>();
            if (controller)
            {
                slot.characterBody.RemoveBuff(controller.currentAffix);
                System.Random rand = new System.Random();
                controller.currentAffix = EssenceChannelerEliteList[rand.Next(EssenceChannelerEliteList.Count)];
                slot.characterBody.AddBuff(controller.currentAffix);
                slot.characterBody.AddTimedBuffAuthority(Channeling.instance.BuffDef.buffIndex, EssenceChannelerDuration);
                return true;
            }
        }
        return false;
    }

    public class EssenceChannelerEliteControllerComponent : MonoBehaviour
    {
        public BuffDef currentAffix;
    }
}
