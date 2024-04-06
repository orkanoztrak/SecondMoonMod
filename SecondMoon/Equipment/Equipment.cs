﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Equipment;

//The Equipment base class was taken almost directly from the Aetherium mod by KomradeSpectre.
//https://github.com/KomradeSpectre/AetheriumMod
public abstract class Equipment<T> : Equipment where T : Equipment<T>
{
    public static T instance { get; private set; }

    public Equipment()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting EquipmentBoilerplate/Equipment was instantiated twice");
        instance = this as T;
    }
}


public abstract class Equipment
{
    public abstract string EquipmentName { get; }
    public abstract string EquipmentLangTokenName { get; }
    public abstract string EquipmentPickupDesc { get; }
    public abstract string EquipmentFullDescription { get; }
    public abstract string EquipmentLore { get; }
    public abstract float Cooldown { get; }

    public virtual GameObject EquipmentModel { get; } = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    public virtual Sprite EquipmentIcon { get; } = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();

    public virtual bool AppearsInSinglePlayer { get; } = true;

    public virtual bool AppearsInMultiPlayer { get; } = true;

    public virtual bool CanDrop { get; } = true;

    public virtual bool EnigmaCompatible { get; } = true;

    public virtual bool IsBoss { get; } = false;

    public virtual bool IsLunar { get; } = false;

    public virtual BuffDef PassiveBuffDef { get; set; } = null;

    public EquipmentDef EquipmentDef;
    public virtual ExpansionDef RequiredExpansion { get; set; } = null;
    protected virtual ItemDisplayRuleDict displayRules { get; set; } = null;

    public abstract void Init();

    public abstract ItemDisplayRuleDict CreateItemDisplayRules();

    protected void CreateLang()
    {
        LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_NAME", EquipmentName);
        LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_PICKUP", EquipmentPickupDesc);
        LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_DESCRIPTION", EquipmentFullDescription);
        LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_LORE", EquipmentLore);
    }

    protected void CreateEquipment()
    {
        EquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
        EquipmentDef.name = "EQUIPMENT_" + EquipmentLangTokenName;
        EquipmentDef.nameToken = "EQUIPMENT_" + EquipmentLangTokenName + "_NAME";
        EquipmentDef.pickupToken = "EQUIPMENT_" + EquipmentLangTokenName + "_PICKUP";
        EquipmentDef.descriptionToken = "EQUIPMENT_" + EquipmentLangTokenName + "_DESCRIPTION";
        EquipmentDef.loreToken = "EQUIPMENT_" + EquipmentLangTokenName + "_LORE";
        EquipmentDef.pickupModelPrefab = EquipmentModel;
        EquipmentDef.pickupIconSprite = EquipmentIcon;
        EquipmentDef.appearsInSinglePlayer = AppearsInSinglePlayer;
        EquipmentDef.appearsInMultiPlayer = AppearsInMultiPlayer;
        EquipmentDef.canDrop = CanDrop;
        EquipmentDef.cooldown = Cooldown;
        EquipmentDef.enigmaCompatible = EnigmaCompatible;
        EquipmentDef.isBoss = IsBoss;
        EquipmentDef.isLunar = IsLunar;
        EquipmentDef.requiredExpansion = RequiredExpansion;
        if (PassiveBuffDef) EquipmentDef.passiveBuffDef = PassiveBuffDef;

        ItemAPI.Add(new CustomEquipment(EquipmentDef, CreateItemDisplayRules()));
        On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;

        if (UseTargeting && TargetingIndicatorPrefabBase)
        {
            On.RoR2.EquipmentSlot.Update += UpdateTargeting;
        }
    }

    protected bool PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, RoR2.EquipmentSlot self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EquipmentDef)
        {
            return ActivateEquipment(self);
        }
        else
        {
            return orig(self, equipmentDef);
        }
    }

    protected abstract bool ActivateEquipment(EquipmentSlot slot);
    public abstract void Hooks();

    //Targeting Support
    public virtual bool UseTargeting { get; } = false;
    public GameObject TargetingIndicatorPrefabBase = null;
    public enum TargetingType
    {
        Enemies,
        Friendlies,
    }
    public virtual TargetingType TargetingTypeEnum { get; } = TargetingType.Enemies;

    //Based on MysticItem's targeting code.
    protected void UpdateTargeting(On.RoR2.EquipmentSlot.orig_Update orig, EquipmentSlot self)
    {
        orig(self);

        if (self.equipmentIndex == EquipmentDef.equipmentIndex)
        {
            var targetingComponent = self.GetComponent<TargetingControllerComponent>();
            if (!targetingComponent)
            {
                targetingComponent = self.gameObject.AddComponent<TargetingControllerComponent>();
                targetingComponent.VisualizerPrefab = TargetingIndicatorPrefabBase;
            }

            if (self.stock > 0)
            {
                switch (TargetingTypeEnum)
                {
                    case (TargetingType.Enemies):
                        targetingComponent.ConfigureTargetFinderForEnemies(self);
                        break;
                    case (TargetingType.Friendlies):
                        targetingComponent.ConfigureTargetFinderForFriendlies(self);
                        break;
                }
            }
            else
            {
                targetingComponent.Invalidate();
                targetingComponent.Indicator.active = false;
            }
        }
    }

    public class TargetingControllerComponent : MonoBehaviour
    {
        public GameObject TargetObject;
        public GameObject VisualizerPrefab;
        public Indicator Indicator;
        public BullseyeSearch TargetFinder;
        public Action<BullseyeSearch> AdditionalBullseyeFunctionality = (search) => { };

        public void Awake()
        {
            Indicator = new Indicator(gameObject, null);
        }

        public void OnDestroy()
        {
            Invalidate();
        }

        public void Invalidate()
        {
            TargetObject = null;
            Indicator.targetTransform = null;
        }

        public void ConfigureTargetFinderBase(EquipmentSlot self)
        {
            if (TargetFinder == null) TargetFinder = new BullseyeSearch();
            TargetFinder.teamMaskFilter = TeamMask.allButNeutral;
            TargetFinder.teamMaskFilter.RemoveTeam(self.characterBody.teamComponent.teamIndex);
            TargetFinder.sortMode = BullseyeSearch.SortMode.Angle;
            TargetFinder.filterByLoS = true;
            float num;
            Ray ray = CameraRigController.ModifyAimRayIfApplicable(self.GetAimRay(), self.gameObject, out num);
            TargetFinder.searchOrigin = ray.origin;
            TargetFinder.searchDirection = ray.direction;
            TargetFinder.maxAngleFilter = 10f;
            TargetFinder.viewer = self.characterBody;
        }

        public void ConfigureTargetFinderForEnemies(EquipmentSlot self)
        {
            ConfigureTargetFinderBase(self);
            TargetFinder.teamMaskFilter = TeamMask.GetUnprotectedTeams(self.characterBody.teamComponent.teamIndex);
            TargetFinder.RefreshCandidates();
            TargetFinder.FilterOutGameObject(self.gameObject);
            AdditionalBullseyeFunctionality(TargetFinder);
            PlaceTargetingIndicator(TargetFinder.GetResults());
        }

        public void ConfigureTargetFinderForFriendlies(EquipmentSlot self)
        {
            ConfigureTargetFinderBase(self);
            TargetFinder.teamMaskFilter = TeamMask.none;
            TargetFinder.teamMaskFilter.AddTeam(self.characterBody.teamComponent.teamIndex);
            TargetFinder.RefreshCandidates();
            TargetFinder.FilterOutGameObject(self.gameObject);
            AdditionalBullseyeFunctionality(TargetFinder);
            PlaceTargetingIndicator(TargetFinder.GetResults());

        }

        public void PlaceTargetingIndicator(IEnumerable<HurtBox> TargetFinderResults)
        {
            HurtBox hurtbox = TargetFinderResults.Any() ? TargetFinderResults.First() : null;

            if (hurtbox)
            {
                TargetObject = hurtbox.healthComponent.gameObject;
                Indicator.visualizerPrefab = VisualizerPrefab;
                Indicator.targetTransform = hurtbox.transform;
            }
            else
            {
                Invalidate();
            }
            Indicator.active = hurtbox;
        }
    }
}
