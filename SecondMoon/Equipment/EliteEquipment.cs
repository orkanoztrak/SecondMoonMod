using BepInEx.Configuration;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Equipment;

public abstract class EliteEquipment<T> : EliteEquipment where T : EliteEquipment<T>
{
    public static T instance { get; private set; }

    public EliteEquipment()
    {
        if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting Equipment was instantiated twice");
        instance = this as T;
    }
}
public abstract class EliteEquipment
{
    public abstract string EliteEquipmentName { get; }
    public abstract string EliteAffixToken { get; }
    public abstract string EliteEquipmentPickupDesc { get; }
    public abstract string EliteEquipmentFullDescription { get; }
    public abstract string EliteEquipmentLore { get; }

    public abstract string EliteModifier { get; }

    public virtual bool IsActivatable { get; } = false;


    public static ConfigOption<float> Cooldown;

    public virtual GameObject EliteEquipmentModel { get; } = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Mystery/PickupMystery.prefab").WaitForCompletion();
    public virtual Sprite EliteEquipmentIcon { get; } = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/MiscIcons/texMysteryIcon.png").WaitForCompletion();

    public virtual bool AppearsInSinglePlayer { get; } = true;

    public virtual bool AppearsInMultiPlayer { get; } = true;

    public static ConfigOption<bool> IsEnabled;

    public bool EnableCheck;

    public virtual bool CanDrop { get; } = false;

    public virtual float DropOnDeathChance { get; } = 0f;

    public virtual bool EnigmaCompatible { get; } = false;

    public virtual bool IsBoss { get; } = false;

    public virtual bool IsLunar { get; } = false;

    public virtual bool IsPrototype { get; } = false;


    public GameObject PrototypeVFX;

    public abstract Color EliteColor { get; }

    public BuffDef EliteBuffDef;

    public abstract Sprite EliteBuffIcon { get; }

    public abstract Texture2D EliteRamp { get; }


    public EliteDef EliteDef;

    public virtual float HealthMultiplier { get; set; } = 1;

    public virtual float DamageMultiplier { get; set; } = 1;

    public virtual Material EliteMaterial { get; set; } = null;

    public EquipmentDef EliteEquipmentDef;

    public GameObject DropletDisplayPrefab;

    public virtual ExpansionDef RequiredExpansion { get; set; } = null;
    protected virtual ItemDisplayRuleDict displayRules { get; set; } = null;

    public virtual void Init(ConfigFile config)
    {
        IsEnabled = config.ActiveBind("Equipment: " + EliteEquipmentName, "Should this be enabled?", true, "If false, this equipment will not appear in the game.");
        EnableCheck = IsEnabled;
    }

    public abstract ItemDisplayRuleDict CreateItemDisplayRules();

    protected void CreateLang()
    {
        LanguageAPI.Add("ELITE_EQUIPMENT_" + EliteAffixToken + "_NAME", EliteEquipmentName);
        LanguageAPI.Add("ELITE_EQUIPMENT_" + EliteAffixToken + "_PICKUP", EliteEquipmentPickupDesc);
        LanguageAPI.Add("ELITE_EQUIPMENT_" + EliteAffixToken + "_DESCRIPTION", EliteEquipmentFullDescription);
        LanguageAPI.Add("ELITE_EQUIPMENT_" + EliteAffixToken + "_LORE", EliteEquipmentLore);
        LanguageAPI.Add("ELITE_" + EliteAffixToken + "_MODIFIER", EliteModifier + " {0}");
    }

    protected void CreateEliteEquipment()
    {
        EliteBuffDef = ScriptableObject.CreateInstance<BuffDef>();
        EliteBuffDef.name = EliteAffixToken;
        EliteBuffDef.canStack = false;
        EliteBuffDef.isCooldown = false;
        EliteBuffDef.isDebuff = false;
        EliteBuffDef.buffColor = EliteColor;
        EliteBuffDef.iconSprite = EliteBuffIcon;

        EliteEquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
        EliteEquipmentDef.name = "ELITE_EQUIPMENT_" + EliteAffixToken;
        EliteEquipmentDef.nameToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_NAME";
        EliteEquipmentDef.pickupToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_PICKUP";
        EliteEquipmentDef.descriptionToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_DESCRIPTION";
        EliteEquipmentDef.loreToken = "ELITE_EQUIPMENT_" + EliteAffixToken + "_LORE";
        EliteEquipmentDef.pickupModelPrefab = EliteEquipmentModel;
        EliteEquipmentDef.pickupIconSprite = EliteEquipmentIcon;
        EliteEquipmentDef.appearsInSinglePlayer = AppearsInSinglePlayer;
        EliteEquipmentDef.appearsInMultiPlayer = AppearsInMultiPlayer;
        EliteEquipmentDef.canDrop = CanDrop;
        EliteEquipmentDef.dropOnDeathChance = DropOnDeathChance;
        if (IsActivatable) 
        { 
            EliteEquipmentDef.cooldown = Cooldown; 
        }
        else
        {
            EliteEquipmentDef.cooldown = 10f;
        }
        EliteEquipmentDef.enigmaCompatible = EnigmaCompatible;
        EliteEquipmentDef.isBoss = IsBoss;
        EliteEquipmentDef.isLunar = IsLunar;
        EliteEquipmentDef.requiredExpansion = RequiredExpansion;
        EliteEquipmentDef.passiveBuffDef = EliteBuffDef;

        EliteDef = ScriptableObject.CreateInstance<EliteDef>();
        EliteDef.name = "ELITE_" + EliteAffixToken;
        EliteDef.modifierToken = "ELITE_" + EliteAffixToken + "_MODIFIER";
        EliteDef.healthBoostCoefficient = HealthMultiplier;
        EliteDef.damageBoostCoefficient = DamageMultiplier;
        R2API.EliteRamp.AddRamp(EliteDef, EliteRamp);
        EliteDef.eliteEquipmentDef = EliteEquipmentDef;
        ContentAddition.AddEliteDef(EliteDef);

        EliteBuffDef.eliteDef = EliteDef;
        ContentAddition.AddBuffDef(EliteBuffDef);
        ItemAPI.Add(new CustomEquipment(EliteEquipmentDef, CreateItemDisplayRules()));

        if (IsActivatable) 
        { 
            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;
            if (UseTargeting && TargetingIndicatorPrefabBase)
            {
                On.RoR2.EquipmentSlot.Update += UpdateTargeting;
            }
        }

        if (EliteMaterial)
        {
            On.RoR2.CharacterBody.FixedUpdate += OverlayManager;
        }

    }

    private void OverlayManager(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
    {
        if (self.modelLocator && self.modelLocator.modelTransform && self.HasBuff(EliteBuffDef) && !self.GetComponent<EliteOverlayManager>())
        {
            var overlay = TemporaryOverlayManager.AddOverlay(self.modelLocator.modelTransform.gameObject);
            overlay.duration = float.PositiveInfinity;
            overlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            overlay.animateShaderAlpha = true;
            overlay.destroyComponentOnEnd = true;
            overlay.originalMaterial = EliteMaterial;
            overlay.AddToCharacterModel(self.modelLocator.modelTransform.GetComponent<CharacterModel>());
            var EliteOverlayManager = self.gameObject.AddComponent<EliteOverlayManager>();
            EliteOverlayManager.Overlay = overlay;
            EliteOverlayManager.Body = self;
            EliteOverlayManager.EliteBuffDef = EliteBuffDef;
        }
        orig(self);
    }

    public class EliteOverlayManager : MonoBehaviour
    {
        public TemporaryOverlayInstance Overlay;
        public CharacterBody Body;
        public BuffDef EliteBuffDef;

        public void FixedUpdate()
        {
            if (!Body.HasBuff(EliteBuffDef))
            {
                Destroy(this);
                Overlay.CleanupEffect();
            }
        }
    }

    protected bool PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
    {
        if (equipmentDef == EliteEquipmentDef)
        {
            return ActivateEquipment(self);
        }
        else
        {
            return orig(self, equipmentDef);
        }
    }

    protected virtual bool ActivateEquipment(EquipmentSlot slot) { return false; }
    public virtual void Hooks() { }

    public virtual bool UseTargeting { get; } = false;
    public GameObject TargetingIndicatorPrefabBase = null;
    public enum TargetingType
    {
        Enemies,
        Friendlies,
    }
    public virtual TargetingType TargetingTypeEnum { get; } = TargetingType.Enemies;

    protected void UpdateTargeting(On.RoR2.EquipmentSlot.orig_Update orig, EquipmentSlot self)
    {
        orig(self);

        if (self.equipmentIndex == EliteEquipmentDef.equipmentIndex)
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
                    case TargetingType.Enemies:
                        targetingComponent.ConfigureTargetFinderForEnemies(self);
                        break;
                    case TargetingType.Friendlies:
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
