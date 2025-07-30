using BepInEx.Configuration;
using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Utilities;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RoR2;
using RoR2.UI;
using SecondMoon.Equipment.BladeOfPetrichor;
using SecondMoon.Equipment.RadiantHelm;
using SecondMoon.Items.ItemTiers;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Items.ItemTiers.VoidTierPrototype;
using SecondMoon.Items.Void.TwistedRegrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using static SecondMoon.Utils.PickupNotificationGenericPickupPatch;
namespace SecondMoon.Utils;

public static class GeneralUtils
{
    public static CharacterMaster FindMasterByInventory(Inventory inventory)
    {
        foreach (var master in CharacterMaster.instancesList)
        {
            if (master.inventory == inventory) return master;
        }
        return null;
    }
    public static GameObject CreateBlankPrefab(string name = "GameObject", bool network = false)
    {
        GameObject gameObject = PrefabAPI.InstantiateClone(new GameObject(name), name, false);
        if (network)
        {
            gameObject.AddComponent<NetworkIdentity>();
            PrefabAPI.RegisterNetworkPrefab(gameObject);
        }
        return gameObject;
    }

    public static float HyperbolicScaling(float value)
    {
        return 1 - 1 / (1 + value);
    }

    //Making count less than or equal to zero will instead find all hurtboxes around the target.
    public static List<HurtBox> FindCountHurtboxesInRangeAroundTarget(float count, float range, TeamIndex attackerTeam, Vector3 position, GameObject victim)
    {
        BullseyeSearch search = new BullseyeSearch();
        HealthComponent init = victim.GetComponent<HealthComponent>();
        List<HealthComponent> targetComponents = [init];
        List<HurtBox> targets = [];
        search.searchOrigin = position;
        search.searchDirection = Vector3.zero;
        search.teamMaskFilter = TeamMask.allButNeutral;
        search.teamMaskFilter.RemoveTeam(attackerTeam);
        search.filterByLoS = false;
        search.sortMode = BullseyeSearch.SortMode.Distance;
        search.maxDistanceFilter = range;

        if (count <= 0)
        {
            bool cont = true;
            while (cont)
            {
                search.RefreshCandidates();
                HurtBox hurtBox = (from v in search.GetResults()
                                   where !targetComponents.Contains(v.healthComponent)
                                   select v).FirstOrDefault();
                if ((bool)hurtBox)
                {
                    targets.Add(hurtBox);
                    targetComponents.Add(hurtBox.healthComponent);
                }
                else
                {
                    cont = false;
                }
            }
        }
        else
        {
            for (int i = 0; i < count; i++)
            {
                search.RefreshCandidates();
                HurtBox hurtBox = (from v in search.GetResults()
                                   where !targetComponents.Contains(v.healthComponent)
                                   select v).FirstOrDefault();
                if ((bool)hurtBox)
                {
                    targets.Add(hurtBox);
                    targetComponents.Add(hurtBox.healthComponent);
                }
            }
        }
        return targets;
    }

    public static List<HurtBox> FindAllHurtboxesInRadius(float radius, Vector3 origin)
    {
        Collider[] array = Physics.OverlapSphere(origin, radius, LayerIndex.entityPrecise.mask);
        List<HurtBox> targets = [];
        List<HealthComponent> targetComponents = [];
        foreach (Collider collider in array)
        {
            HurtBox hurtBox = collider.GetComponent<HurtBox>();
            if (hurtBox)
            {
                if (!targetComponents.Contains(hurtBox.healthComponent))
                {
                    targets.Add(hurtBox);
                    targetComponents.Add(hurtBox.healthComponent);
                }
            }
        }
        return targets;
    }

    public static bool IsSecondMoonCustomTier(ItemTierDef itemTierDef, out Tier tier)
    {
        tier = null;
        if (!itemTierDef) return false;
        foreach (var itemTier in SecondMoonPlugin.SecondMoonTiers)
        {
            if (itemTierDef.Equals(itemTier.ItemTierDef))
            {
                tier = itemTier;
                return true;
            }
        }
        return false;
    }

    public static bool IsSecondMoonPrototypeEquipment(EquipmentDef equipmentDef, out Equipment.Equipment equipment)
    {
        equipment = null;
        if (!equipmentDef) return false;
        foreach (var customEquipment in SecondMoonPlugin.SecondMoonEquipment)
        {
            if (equipmentDef.Equals(customEquipment.EquipmentDef) && customEquipment.IsPrototype)
            {
                equipment = customEquipment;
                return true;
            }
        }
        return false;
    }

    public static CharacterModel.RendererInfo[] ItemDisplaySetup(GameObject obj)
    {
        List<Renderer> AllRenderers = new List<Renderer>();

        var meshRenderers = obj.GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0) { AllRenderers.AddRange(meshRenderers); }

        var skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>();
        if (skinnedMeshRenderers.Length > 0) { AllRenderers.AddRange(skinnedMeshRenderers); }

        CharacterModel.RendererInfo[] renderInfos = new CharacterModel.RendererInfo[AllRenderers.Count];

        for (int i = 0; i < AllRenderers.Count; i++)
        {
            renderInfos[i] = new CharacterModel.RendererInfo
            {
                defaultMaterial = AllRenderers[i] is SkinnedMeshRenderer ? AllRenderers[i].sharedMaterial : AllRenderers[i].material,
                renderer = AllRenderers[i],
                defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On,
                ignoreOverlays = false
            };
        }

        return renderInfos;
    }
}

public class ConfigOption<T>
{
    ConfigEntry<T> Bind;

    public ConfigOption(ConfigFile config, string categoryName, string configOptionName, T defaultValue, string fullDescription)
    {
        Bind = config.Bind(categoryName, configOptionName, defaultValue, fullDescription);
    }

    public static implicit operator T(ConfigOption<T> x)
    {
        return x.Bind.Value;
    }

    public override string ToString()
    {
        return Bind.Value.ToString();
    }
}

public static class ConfigExtension
{
    public static ConfigOption<T> ActiveBind<T>(this ConfigFile configWrapper, string categoryName, string configOptionName, T defaultValue, string fullDescription)
    {
        return new ConfigOption<T>(configWrapper, categoryName, configOptionName, defaultValue, fullDescription);
    }
}

//A class that uses PickupNotificationGenericPickupPatch and vanilla code to make Prototype Equipment corruptions via Twisted Regrets look similar to regular Void item corruptions.
public static class ContagiousPrototypeManager
{
    private static List<InventoryReplacementCandidate> pendingCorruptions = new List<InventoryReplacementCandidate>();

    private struct InventoryReplacementCandidate
    {
        public Inventory inventory;

        public uint equipmentSlot;

        public PickupIndex originalItemOrEquipment;

        public PickupIndex newItemOrEquipment;

        public Run.FixedTimeStamp time;

        public bool isForced;
    }


    [SystemInitializer]
    public static void Init()
    {
        Inventory.onInventoryChangedGlobal += OnInventoryChangedGlobal;
        RoR2Application.onFixedUpdate += StaticFixedUpdate;
    }

    private static void StaticFixedUpdate()
    {
        if (pendingCorruptions.Count > 0)
        {
            ProcessPendingCorruptions();
        }
    }

    private static void ProcessPendingCorruptions()
    {
        if (!NetworkServer.active || !Run.instance)
        {
            pendingCorruptions.Clear();
            return;
        }
        for (int i = pendingCorruptions.Count - 1; i >= 0; i--)
        {
            var candidate = pendingCorruptions[i];
            if (candidate.time.hasPassed)
            {
                var currentEquipment = candidate.inventory.GetEquipment(candidate.equipmentSlot);
                if (currentEquipment.equipmentIndex != EquipmentIndex.None && candidate.inventory.GetItemCount((ItemIndex)Array.IndexOf(PickupCatalog.itemIndexToPickupIndex, candidate.newItemOrEquipment)) > 0)
                {
                    if (PickupCatalog.FindPickupIndex(currentEquipment.equipmentDef.equipmentIndex) == candidate.originalItemOrEquipment)
                    {
                        candidate.inventory.SetEquipment(EquipmentState.empty, candidate.equipmentSlot);
                        candidate.inventory.GiveItem(TwistedRegrets.instance.ItemDef.itemIndex, 1);
                        CharacterMaster component = candidate.inventory.GetComponent<CharacterMaster>();
                        if ((bool)component)
                        {
                            new PickupTransformationNotificationMessage(component, candidate.originalItemOrEquipment, candidate.newItemOrEquipment, CharacterMasterNotificationQueue.TransformationType.ContagiousVoid).Send(NetworkDestination.Clients | NetworkDestination.Server);
                        }
                        pendingCorruptions.RemoveAt(i);
                    }
                }
            }
        }
    }

    private static void OnInventoryChangedGlobal(Inventory inventory)
    {
        if (!NetworkServer.active) return;
        if (inventory.GetItemCount(TwistedRegrets.instance.ItemDef) > 0)
        {
            for (int i = 0; i < inventory.GetEquipmentSlotCount(); i++)
            {
                var equip = inventory.GetEquipment((uint)i).equipmentDef;
                if (GeneralUtils.IsSecondMoonPrototypeEquipment(equip, out var equipment) && FindInventoryReplacementCandidateIndex(inventory, PickupCatalog.FindPickupIndex(equip.equipmentIndex)) == -1)
                {
                    pendingCorruptions.Add(new InventoryReplacementCandidate
                    {
                        inventory = inventory,
                        equipmentSlot = (uint)i,
                        originalItemOrEquipment = PickupCatalog.FindPickupIndex(equipment.EquipmentDef.equipmentIndex),
                        newItemOrEquipment = PickupCatalog.FindPickupIndex(TwistedRegrets.instance.ItemDef.itemIndex),
                        time = Run.FixedTimeStamp.now + 0.5f,
                        isForced = false
                    });
                }
            }
        }
    }

    private static int FindInventoryReplacementCandidateIndex(Inventory inventory, PickupIndex pickupIndex)
    {
        for (int i = 0; i < pendingCorruptions.Count; i++)
        {
            InventoryReplacementCandidate inventoryReplacementCandidate = pendingCorruptions[i];
            if (inventoryReplacementCandidate.inventory == inventory && inventoryReplacementCandidate.originalItemOrEquipment == pickupIndex)
            {
                return i;
            }
        }
        return -1;
    }

    //stolen from ShrineOfRepair
    public static void AddPersistentListener(this UnityEvent<MPButton, PickupDef> unityEvent, UnityAction<MPButton, PickupDef> action)
    {
        unityEvent.m_PersistentCalls.AddListener(new PersistentCall
        {
            m_Target = action.Target as UnityEngine.Object,
            m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
            m_MethodName = action.Method.Name,
            m_CallState = UnityEventCallState.RuntimeOnly,
            m_Mode = PersistentListenerMode.EventDefined,
        });
    }

    public static void AddPersistentListener(this UnityEvent<int> unityEvent, UnityAction<int> action)
    {
        unityEvent.m_PersistentCalls.AddListener(new PersistentCall
        {
            m_Target = action.Target as UnityEngine.Object,
            m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
            m_MethodName = action.Method.Name,
            m_CallState = UnityEventCallState.RuntimeOnly,
            m_Mode = PersistentListenerMode.EventDefined,
        });
    }

    public static void AddPersistentListener(this UnityEvent<Interactor> unityEvent, UnityAction<Interactor> action)
    {
        unityEvent.m_PersistentCalls.AddListener(new PersistentCall
        {
            m_Target = action.Target as UnityEngine.Object,
            m_TargetAssemblyTypeName = UnityEventTools.TidyAssemblyTypeName(action.Method.DeclaringType.AssemblyQualifiedName),
            m_MethodName = action.Method.Name,
            m_CallState = UnityEventCallState.RuntimeOnly,
            m_Mode = PersistentListenerMode.EventDefined,
        });
    }
}

//shamelessly copied from BubbetsItems
[HarmonyPatch]
public static class ExtraHealthBarSegments
{
    private static List<Type> barDataTypes = new();

    public static void AddType<T>() where T : BarData, new()
    {
        barDataTypes.Add(typeof(T));
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.Awake))]
    public static void AddTracker(HealthBar __instance)
    {
        __instance.gameObject.AddComponent<ExtraHealthbarInfoTracker>().Init(__instance);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.CheckInventory))]
    public static void CheckInventory(HealthBar __instance)
    {
        var tracker = __instance.GetComponent<ExtraHealthbarInfoTracker>();
        if (!tracker) return;
        var source = __instance.source;
        if (!source) return;
        var body = source.body;
        if (!body) return;
        var inv = body.inventory;
        if (!inv) return;
        tracker.CheckInventory(inv, body, source);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.UpdateBarInfos))]
    public static void UpdateInfos(HealthBar __instance)
    {
        var tracker = __instance.GetComponent<ExtraHealthbarInfoTracker>();
        tracker.UpdateInfo();
    }

    [HarmonyILManipulator, HarmonyPatch(typeof(HealthBar), nameof(HealthBar.ApplyBars))]
    public static void ApplyBar(ILContext il)
    {
        var c = new ILCursor(il);

        var cls = -1;
        FieldReference fld = null;
        c.GotoNext(
            x => x.MatchLdloca(out cls),
            x => x.MatchLdcI4(0),
            x => x.MatchStfld(out fld)
        );

        c.GotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt<HealthBar.BarInfoCollection>(nameof(HealthBar.BarInfoCollection.GetActiveCount))
        );
        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate<Func<int, HealthBar, int>>((i, bar) =>
        {
            var tracker = bar.GetComponent<ExtraHealthbarInfoTracker>();
            i += tracker.barInfos.Count(x => x.info.enabled);
            return i;
        });
        c.Index = il.Instrs.Count - 2;
        c.Emit(OpCodes.Ldloca, cls);
        c.Emit(OpCodes.Ldarg_0);
        c.Emit(OpCodes.Ldloca, cls);
        c.Emit(OpCodes.Ldfld, fld);
        c.EmitDelegate<Func<HealthBar, int, int>>((bar, i) =>
        {
            var tracker = bar.GetComponent<ExtraHealthbarInfoTracker>();
            tracker.ApplyBar(ref i);
            return i;
        });
        c.Emit(OpCodes.Stfld, fld); ;
    }

    public abstract class BarData
    {
        public ExtraHealthbarInfoTracker tracker;
        public HealthBar bar;
        public HealthBar.BarInfo info;
        public HealthBarStyle.BarStyle? cachedStyle;
        private Image _imageReference;
        public virtual Image ImageReference
        {
            get => _imageReference;
            set
            {
                if (_imageReference && _imageReference != value)
                {
                    _imageReference.material = bar.barAllocator.elementPrefab.GetComponent<Image>().material;
                }
                _imageReference = value;
            }
        }

        public abstract HealthBarStyle.BarStyle GetStyle();

        public virtual void UpdateInfo(ref HealthBar.BarInfo info, HealthComponent.HealthBarValues healthBarValues)
        {
            if (cachedStyle == null) cachedStyle = GetStyle();
            var style = cachedStyle.Value;

            info.enabled &= style.enabled;
            info.color = style.baseColor;
            info.imageType = style.imageType;
            info.sprite = style.sprite;
            info.sizeDelta = style.sizeDelta;
        }

        public virtual void CheckInventory(ref HealthBar.BarInfo info, Inventory inventory, CharacterBody characterBody, HealthComponent healthComponent) { }
        public virtual void ApplyBar(ref HealthBar.BarInfo info, Image image, ref int i)
        {
            image.type = info.imageType;
            image.sprite = info.sprite;
            image.color = info.color;

            var rectTransform = (RectTransform)image.transform;
            rectTransform.anchorMin = new Vector2(info.normalizedXMin, 0f);
            rectTransform.anchorMax = new Vector2(info.normalizedXMax, 1f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(info.sizeDelta * 0.5f + 1f, info.sizeDelta + 1f);

            i++;
        }
    }

    public class ExtraHealthbarInfoTracker : MonoBehaviour
    {
        public List<BarData> barInfos;
        public HealthBar healthBar;

        public void CheckInventory(Inventory inv, CharacterBody characterBody, HealthComponent healthComponent)
        {
            foreach (var barInfo in barInfos)
            {
                barInfo.CheckInventory(ref barInfo.info, inv, characterBody, healthComponent);
            }
        }

        public void UpdateInfo()
        {
            if (!healthBar || !healthBar.source) return;
            var healthBarValues = healthBar.source.GetHealthBarValues();
            foreach (var barInfo in barInfos)
            {
                if (barInfo.tracker == null)
                    barInfo.tracker = this;
                if (barInfo.bar == null)
                    barInfo.bar = healthBar;
                barInfo.UpdateInfo(ref barInfo.info, healthBarValues);
            }
        }
        public void ApplyBar(ref int i)
        {
            foreach (var barInfo in barInfos)
            {
                ref var info = ref barInfo.info;
                if (!info.enabled)
                {
                    barInfo.ImageReference = null;
                    continue;
                }

                Image image = healthBar.barAllocator.elements[i];
                barInfo.ImageReference = image;
                barInfo.ApplyBar(ref barInfo.info, image, ref i);
            }
        }

        public void Init(HealthBar healthBar)
        {
            this.healthBar = healthBar;
            barInfos = barDataTypes.Select(dataType => (BarData)Activator.CreateInstance(dataType)).ToList();
        }
    }
}

//unabashedly copied from RiskOfChaos
public static class PickupNotificationGenericPickupPatch
{
    [SystemInitializer]
    public static void Init()
    {
        NetworkingAPI.RegisterMessageType<PickupTransformationNotificationMessage>();
        On.RoR2.UI.NotificationUIController.SetUpNotification += NotificationUIController_SetUpNotification;
    }

    public static void NotificationUIController_SetUpNotification(On.RoR2.UI.NotificationUIController.orig_SetUpNotification orig, NotificationUIController self, CharacterMasterNotificationQueue.NotificationInfo notificationInfo)
    {
        orig(self, notificationInfo);

        GenericNotification notification = self.currentNotification;

        if (!notification || notificationInfo is null)
            return;

        if (notificationInfo.transformation is not null)
        {
            if (notificationInfo.transformation.previousData is PickupDef fromPickup)
            {
                SetPreviousPickup(notification, fromPickup);
            }
        }

        if (notificationInfo.data is PickupDef toPickup)
        {
            SetPickup(notification, toPickup);
        }
    }

    public static void SetPreviousPickup(GenericNotification notification, PickupDef pickup)
    {
        if (notification.previousIconImage && pickup.iconTexture)
        {
            notification.previousIconImage.texture = pickup.iconTexture;
        }
    }

    public static void SetPickup(GenericNotification notification, PickupDef pickup)
    {
        notification.titleText.token = pickup.nameToken;

        if (pickup.itemIndex != ItemIndex.None)
        {
            ItemDef item = ItemCatalog.GetItemDef(pickup.itemIndex);
            notification.descriptionText.token = item.pickupToken;
        }
        else if (pickup.equipmentIndex != EquipmentIndex.None)
        {
            EquipmentDef equipment = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
            notification.descriptionText.token = equipment.pickupToken;
        }

        if (pickup.iconTexture)
        {
            notification.iconImage.texture = pickup.iconTexture;
        }

        notification.titleTMP.color = pickup.baseColor;
    }

    public class PickupTransformationNotificationMessage : INetMessage
    {
        CharacterMaster _master;
        PickupIndex _fromPickupIndex;
        PickupIndex _toPickupIndex;
        CharacterMasterNotificationQueue.TransformationType _transformationType;

        public PickupTransformationNotificationMessage()
        {
        }

        public PickupTransformationNotificationMessage(CharacterMaster characterMaster, PickupIndex fromPickupIndex, PickupIndex toPickupIndex, CharacterMasterNotificationQueue.TransformationType transformationType)
        {
            _master = characterMaster;
            _fromPickupIndex = fromPickupIndex;
            _toPickupIndex = toPickupIndex;
            _transformationType = transformationType;
        }

        void ISerializableObject.Serialize(NetworkWriter writer)
        {
            writer.Write(_master.gameObject);
            PickupIndex.WriteToNetworkWriter(writer, _fromPickupIndex);
            PickupIndex.WriteToNetworkWriter(writer, _toPickupIndex);
            writer.WritePackedUInt32((uint)_transformationType);
        }

        void ISerializableObject.Deserialize(NetworkReader reader)
        {
            GameObject masterObject = reader.ReadGameObject();
            _master = masterObject ? masterObject.GetComponent<CharacterMaster>() : null;

            _fromPickupIndex = PickupIndex.ReadFromNetworkReader(reader);
            _toPickupIndex = PickupIndex.ReadFromNetworkReader(reader);

            _transformationType = (CharacterMasterNotificationQueue.TransformationType)reader.ReadPackedUInt32();
        }

        void INetMessage.OnReceived()
        {
            if (!_master || !_master.hasAuthority)
                return;

            if (!_fromPickupIndex.isValid || !_toPickupIndex.isValid)
                return;

            CharacterMasterNotificationQueue notificationQueue = CharacterMasterNotificationQueue.GetNotificationQueueForMaster(_master);
            if (!notificationQueue)
                return;

            CharacterMasterNotificationQueue.TransformationInfo transformationInfo = new CharacterMasterNotificationQueue.TransformationInfo(_transformationType, PickupCatalog.GetPickupDef(_fromPickupIndex));
            CharacterMasterNotificationQueue.NotificationInfo notificationInfo = new CharacterMasterNotificationQueue.NotificationInfo(PickupCatalog.GetPickupDef(_toPickupIndex), transformationInfo);
            notificationQueue.PushNotification(notificationInfo, CharacterMasterNotificationQueue.firstNotificationLengthSeconds);
        }
    }
}