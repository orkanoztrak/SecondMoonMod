using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Buffs.Equipment;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace SecondMoon.Equipment.EssenceChanneler;

public class EssenceChanneler : Equipment<EssenceChanneler>
{
    public static float EssenceChannelerDuration = 6f;

    public static float ChannelingBoost = 0.3f;

    public static List<string> EssenceChannelerEliteNamesList = ["bdEliteFire", "bdEliteIce", "bdEliteLightning", "bdElitePoison", "bdEliteHaunted"];
    public static List<BuffDef> EssenceChannelerEliteList = new List<BuffDef>();
    public override string EquipmentName => "Essence Channeler";

    public override string EquipmentLangTokenName => "SECONDMOONMOD_ESSENCE_CHANNELER_EQUIP";

    public override string EquipmentPickupDesc => $"Briefly increase all stats. Gain the powers of a random Elite type with each use.";

    public override string EquipmentFullDescription => $"For <style=cIsUtility>{EssenceChannelerDuration}s</style>, increase <style=cIsUtility>ALL stats</style> by <style=cIsUtility>{ChannelingBoost * 100}</style>." +
        $"Gain a random classic Elite affix upon using this that changes with each use.";

    public override string EquipmentLore => "Test";

    public override float Cooldown => 45f;

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
        //IL.RoR2.CharacterModel.UpdateOverlays += SetEssenceChannelerOverlay;
    }

    private void SetEssenceChannelerOverlay(ILContext il)
    {
        var cursor = new ILCursor(il);
        cursor.GotoNext(x => x.MatchStloc(1));
        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
        cursor.EmitDelegate<Action<CharacterModel>>((model) =>
        {
            if (model.body)
            {
                var controller = model.body.gameObject.GetComponent<EssenceChannelerEliteControllerComponent>();
                if (controller)
                {
                    if (controller.currentAffix)
                    {
                        model.myEliteIndex = controller.currentAffix.eliteDef.eliteIndex;
                        model.shaderEliteRampIndex = controller.currentAffix.eliteDef.shaderEliteRampIndex;
                    }
                }
            }
        });
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

    public override void Init()
    {
        CreateLang();
        CreateEquipment();
        Hooks();
    }

    protected override bool ActivateEquipment(EquipmentSlot slot)
    {
        if (slot.characterBody)
        {
            var obj = slot.characterBody.gameObject;
            if (obj)
            {
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
        }
        return false;
    }

    public class EssenceChannelerEliteControllerComponent : MonoBehaviour
    {
        public BuffDef currentAffix;
    }
}
