using BepInEx;
using HarmonyLib;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using SecondMoon.BuffsAndDebuffs;
using SecondMoon.BuffsAndDebuffs.Buffs.Equipment;
using SecondMoon.EquipmentlessElites;
using SecondMoon.Equipment.SharpVinegar;
using SecondMoon.Items;
using SecondMoon.Items.ItemTiers;
using SecondMoon.Items.ItemTiers.TierPrototype;
using SecondMoon.Items.ItemTiers.VoidTierPrototype;
using SecondMoon.Items.Lunar.NarrowMagnifier;
using SecondMoon.Items.Prototype.Thunderbolt;
using SecondMoon.Items.Tier3.QuicksilverVest;
using SecondMoon.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ConstrainedExecution;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SecondMoon.Equipment;
using SecondMoon.Interactables;
using SecondMoon.Items.Void.TwistedRegrets;
using SecondMoon.Equipment.RadiantHelm;
using R2API.Networking.Interfaces;
using R2API.Networking;
using SecondMoon.Items.ItemTiers.TierPrototypeDormant;
using SecondMoon.Items.ItemTiers.VoidTierPrototypeDormant;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SecondMoon
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(DotAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(PrefabAPI.PluginGUID)]
    [BepInDependency(DamageAPI.PluginGUID)]
    [BepInDependency(OrbAPI.PluginGUID)]
    [BepInDependency(ColorsAPI.PluginGUID)]
    [BepInDependency(RecalculateStatsAPI.PluginGUID)]
    [BepInDependency("droppod.lookingglass", BepInDependency.DependencyFlags.SoftDependency)]

    // This is the main declaration of our plugin class.
    // BepInEx searches for all classes inheriting from BaseUnityPlugin to initialize on startup.
    // BaseUnityPlugin itself inherits from MonoBehaviour,
    // so you can use this as a reference for what you can declare and use in your plugin class
    // More information in the Unity Docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.html
    public class SecondMoonPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "AuthorName";
        public const string PluginName = "SecondMoon";
        public const string PluginVersion = "1.0.0";

        public static AssetBundle SecondMoonAssets;
        internal static BepInEx.Configuration.ConfigFile MainConfig;

        public static ExpansionDef SecondMoonExpansionDef = ScriptableObject.CreateInstance<ExpansionDef>();

        public static List<Tier> SecondMoonTiers = [];
        public static List<EquipmentlessElite> SecondMoonEquipmentlessElites = [];
        public static List<EliteEquipment> SecondMoonEliteEquipment = [];
        public static List<Buff> SecondMoonBuffs = [];
        public static List<DOT> SecondMoonDots = [];
        public static List<Item> SecondMoonItems = [];
        public static List<Equipment.Equipment> SecondMoonEquipment = [];
        public static List<Interactable> SecondMoonInteractables = [];

        public static HashSet<ItemDef> BlacklistedFromPrinter = new HashSet<ItemDef>();
        public static List<Item> VoidItemList = [];
        public static ExpansionDef DLC1;
        public static ExpansionDef DLC2;


        public void Awake()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("SecondMoon.secondmoon_assets"))
            {
                SecondMoonAssets = AssetBundle.LoadFromStream(stream);
            }

            Log.Init(Logger);
            MainConfig = Config;

            DLC1 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
            DLC2 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC2/Common/DLC2.asset").WaitForCompletion();

            var harm = new Harmony(Info.Metadata.GUID);
            new PatchClassProcessor(harm, typeof(ExtraHealthBarSegments)).Patch();
            //GenerateExpansionDef();

            SecondMoonModColors.Init();
            PickupNotificationGenericPickupPatch.Init();
            ContagiousPrototypeManager.Init();
            ItemTierPickupVFXHelper.Init();

            var TierTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Tier)));
            Logger.LogInfo("----------------------TIERS--------------------");

            foreach (var tierType in TierTypes)
            {
                Tier tier = (Tier)Activator.CreateInstance(tierType);
                tier.Init();
                SecondMoonTiers.Add(tier);
                Logger.LogInfo("Tier: " + tier.Name + " Initialized!");
            }

            var EquipmentlessEliteTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentlessElite)));
            Logger.LogInfo("--------------EQUIPMENTLESS ELITES-------------");

            foreach (var equipmentlessEliteType in EquipmentlessEliteTypes)
            {
                EquipmentlessElite equipmentlessElite = (EquipmentlessElite)Activator.CreateInstance(equipmentlessEliteType);
                equipmentlessElite.Init();
                SecondMoonEquipmentlessElites.Add(equipmentlessElite);
                Logger.LogInfo("Equipmentless elite: " + equipmentlessElite.EliteName + " Initialized!");
            }

            var EliteEquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipment)));
            Logger.LogInfo("-----------------ELITE EQUIPMENT---------------");

            foreach (var eliteEquipmentType in EliteEquipmentTypes)
            {
                EliteEquipment eliteEquipment = (EliteEquipment)Activator.CreateInstance(eliteEquipmentType);
                eliteEquipment.Init(Config);
                SecondMoonEliteEquipment.Add(eliteEquipment);
                if (eliteEquipment.EnableCheck)
                {
                    Logger.LogInfo("Elite equipment: " + eliteEquipment.EliteEquipmentName + " Initialized!");
                }
            }

            var BuffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Buff)));
            Logger.LogInfo("----------------------BUFFS--------------------");

            foreach (var buffType in BuffTypes)
            {
                Buff buff = (Buff)Activator.CreateInstance(buffType);
                buff.Init();
                SecondMoonBuffs.Add(buff);
                Logger.LogInfo("Buff: " + buff.Name + " Initialized!");
            }

            var DotTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(DOT)));
            Logger.LogInfo("----------------------DOTS---------------------");

            foreach (var dotType in DotTypes)
            {
                DOT dot = (DOT)Activator.CreateInstance(dotType);
                dot.Init();
                SecondMoonDots.Add(dot);
                Logger.LogInfo("DOT: " + dot.AssociatedBuffName + " Initialized!");
            }


            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Item)));
            Logger.LogInfo("----------------------ITEMS--------------------");
            var UntieredOrDormantItems = new List<Item>();
            foreach (var itemType in ItemTypes)
            {
                Item item = (Item)Activator.CreateInstance(itemType);
                if (item.ItemTierDef != null && !item.ItemTierDef.Equals(TierPrototypeDormant.instance.ItemTierDef) && !item.ItemTierDef.Equals(VoidTierPrototypeDormant.instance.ItemTierDef))
                {
                    item.Init(Config);
                    SecondMoonItems.Add(item);
                    if (item.EnableCheck)
                    {
                        Logger.LogInfo("Item: " + item.ItemName + " Initialized!");
                        if (item.ItemToCorrupt || item.ItemsToCorrupt != null || item.ItemTierToCorrupt)
                        {
                            VoidItemList.Add(item);
                        }
                    }
                }
                else
                {
                    UntieredOrDormantItems.Add(item);
                }
            }

            On.RoR2.Items.ContagiousItemManager.Init += Corrupt;
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Equipment.Equipment)));

            Logger.LogInfo("--------------------EQUIPMENT------------------");

            foreach (var equipmentType in EquipmentTypes)
            {
                Equipment.Equipment equipment = (Equipment.Equipment)Activator.CreateInstance(equipmentType);
                equipment.Init(Config);
                SecondMoonEquipment.Add(equipment);
                if (equipment.EnableCheck)
                {
                    Logger.LogInfo("Equipment: " + equipment.EquipmentName + " Initialized!");
                }
            }

            foreach (var untieredItem in UntieredOrDormantItems)
            {
                untieredItem.Init(Config);
                SecondMoonItems.Add(untieredItem);
            }

            var InteractableTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Interactable)));
            Logger.LogInfo("------------------INTERACTABLES----------------");

            foreach (var interactableType in InteractableTypes)
            {
                Interactable interactable = (Interactable)Activator.CreateInstance(interactableType);
                interactable.Init(Config);
                SecondMoonInteractables.Add(interactable);
                if (interactable.EnableCheck)
                {
                    Logger.LogInfo("Interactable: " + interactable.InteractableName + " Initialized!");
                }
            }

            On.RoR2.UI.LogBook.LogBookController.CanSelectItemEntry += ExcludePrototypesAndDormants;
            On.RoR2.UI.LogBook.LogBookController.CanSelectEquipmentEntry += ExcludePrototypes;
            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += AddPrototypes;
            IL.RoR2.EquipmentDef.CreatePickupDef += EquipmentDef_CreatePickupDef;
            On.RoR2.CharacterBody.RecalculateStats += FinalModificationsToStats;

        }


        public void GenerateExpansionDef()
        {
            LanguageAPI.Add("SECONDMOONMOD_EXPANSION_DEF_NAME", "Second Moon");
            LanguageAPI.Add("SECONDMOONMOD_EXPANSION_DEF_DESCRIPTION", "Adds a whole host of new items with unique effects. Some will require you to take on challenges to unleash their true power...");

            SecondMoonExpansionDef.descriptionToken = "SECONDMOONMOD_EXPANSION_DEF_DESCRIPTION";
            SecondMoonExpansionDef.nameToken = "SECONDMOONMOD_EXPANSION_DEF_NAME";
            //SecondMoonExpansionDef.iconSprite = MainAssets.LoadAsset<Sprite>("AetheriumActiveIconAlt.png");
            //SecondMoonExpansionDef.disabledIconSprite = MainAssets.LoadAsset<Sprite>("AetheriumInactiveIconAlt.png");

            ContentAddition.AddExpansionDef(SecondMoonExpansionDef);
        }



        private RoR2.UI.LogBook.Entry[] AddPrototypes(On.RoR2.UI.LogBook.LogBookController.orig_BuildPickupEntries orig, Dictionary<ExpansionDef, bool> expansionAvailability)
        {
            List<RoR2.UI.LogBook.Entry> array = orig(expansionAvailability).ToList();
            List<RoR2.UI.LogBook.Entry> prototypes = [];
            List<RoR2.UI.LogBook.Entry> prototypeEquipment = [];
            List<RoR2.UI.LogBook.Entry> voidPrototypes = [];

            foreach (PickupDef pickupDef in PickupCatalog.allPickups)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
                if (itemDef)
                {
                    if (GeneralUtils.IsSecondMoonCustomTier(itemDef._itemTierDef, out Tier tier))
                    {
                        if (tier.ItemTierDef.Equals(TierPrototype.instance.ItemTierDef))
                        {
                            prototypes.Add(new RoR2.UI.LogBook.Entry
                            {
                                nameToken = itemDef.nameToken,
                                color = ColorCatalog.GetColor(itemDef._itemTierDef.darkColorIndex),
                                iconTexture = itemDef.pickupIconTexture,
                                bgTexture = itemDef._itemTierDef.bgIconTexture,
                                extraData = PickupCatalog.FindPickupIndex(itemDef.itemIndex),
                                modelPrefab = itemDef.pickupModelPrefab,
                                getStatusImplementation = RoR2.UI.LogBook.LogBookController.GetPickupStatus,
                                getTooltipContentImplementation = RoR2.UI.LogBook.LogBookController.GetPickupTooltipContent,
                                pageBuilderMethod = RoR2.UI.LogBook.PageBuilder.SimplePickup,
                                isWIPImplementation = RoR2.UI.LogBook.LogBookController.IsEntryPickupItemWithoutLore
                            });
                        }
                        else if (tier.ItemTierDef.Equals(VoidTierPrototype.instance.ItemTierDef))
                        {
                            voidPrototypes.Add(new RoR2.UI.LogBook.Entry
                            {
                                nameToken = itemDef.nameToken,
                                color = ColorCatalog.GetColor(itemDef._itemTierDef.darkColorIndex),
                                iconTexture = itemDef.pickupIconTexture,
                                bgTexture = itemDef._itemTierDef.bgIconTexture,
                                extraData = PickupCatalog.FindPickupIndex(itemDef.itemIndex),
                                modelPrefab = itemDef.pickupModelPrefab,
                                getStatusImplementation = RoR2.UI.LogBook.LogBookController.GetPickupStatus,
                                getTooltipContentImplementation = RoR2.UI.LogBook.LogBookController.GetPickupTooltipContent,
                                pageBuilderMethod = RoR2.UI.LogBook.PageBuilder.SimplePickup,
                                isWIPImplementation = RoR2.UI.LogBook.LogBookController.IsEntryPickupItemWithoutLore
                            });
                        }
                    }
                }

                EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
                if (equipmentDef)
                {
                    if (GeneralUtils.IsSecondMoonPrototypeEquipment(equipmentDef, out Equipment.Equipment _))
                    {
                        prototypeEquipment.Add(new RoR2.UI.LogBook.Entry 
                        {
                            nameToken = equipmentDef.nameToken,
                            color = ColorCatalog.GetColor(SecondMoonModColors.PrototypeDarkColorIndex),
                            iconTexture = equipmentDef.pickupIconTexture,
                            bgTexture = TierPrototype.instance.BgIconTexture,
                            extraData = PickupCatalog.FindPickupIndex(equipmentDef.equipmentIndex),
                            modelPrefab = equipmentDef.pickupModelPrefab,
                            getStatusImplementation = RoR2.UI.LogBook.LogBookController.GetPickupStatus,
                            getTooltipContentImplementation = RoR2.UI.LogBook.LogBookController.GetPickupTooltipContent,
                            pageBuilderMethod = RoR2.UI.LogBook.PageBuilder.SimplePickup,
                            isWIPImplementation = RoR2.UI.LogBook.LogBookController.IsEntryPickupEquipmentWithoutLore
                        });
                    }
                }
            }
            var proto = array.FindLastIndex(x => PickupCatalog.GetPickupDef((PickupIndex)x.extraData).itemTier == ItemTier.Boss);
            array.InsertRange(proto + 1, prototypes.Concat(prototypeEquipment).ToList());

            var voidProto = array.FindLastIndex(x => PickupCatalog.GetPickupDef((PickupIndex)x.extraData).itemTier == ItemTier.VoidBoss);
            array.InsertRange(voidProto + 1, voidPrototypes);

            return array.ToArray();
        }

        //I want Prototypes to be listed in a specific way in the logbook.
        //So these two remove them from the selector methods for me to manually add them later.
        //ExcludePrototypesAndDormants also allows me to remove Dormants from the Logbook completely alongside this.
        private bool ExcludePrototypesAndDormants(On.RoR2.UI.LogBook.LogBookController.orig_CanSelectItemEntry orig, ItemDef itemDef, Dictionary<ExpansionDef, bool> expansionAvailability)
        {
            if (itemDef)
            {
                ItemTierDef itemTierDef = ItemTierCatalog.GetItemTierDef(itemDef.tier);
                if (GeneralUtils.IsSecondMoonCustomTier(itemTierDef, out var tier))
                {
                    if (tier.ItemTierDef.Equals(TierPrototype.instance.ItemTierDef) || tier.ItemTierDef.Equals(TierPrototypeDormant.instance.ItemTierDef))
                    {
                        return false;
                    }
                }
            }
            return orig(itemDef, expansionAvailability);
        }

        private bool ExcludePrototypes(On.RoR2.UI.LogBook.LogBookController.orig_CanSelectEquipmentEntry orig, EquipmentDef equipmentDef, Dictionary<ExpansionDef, bool> expansionAvailability)
        {
            if (GeneralUtils.IsSecondMoonPrototypeEquipment(equipmentDef, out _)) return false;
            return orig(equipmentDef, expansionAvailability);
        }

        //Turns Prototype Equipment droplets into Prototype droplets.
        //Lunar Equipment have Equipment droplets but Prototypes are meant to be extra special.
        private void EquipmentDef_CreatePickupDef(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(x => x.MatchRet()))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Dup);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<PickupDef, EquipmentDef>>((pickupDef, equipmentDef) =>
                {
                    if (equipmentDef)
                    {
                        if (GeneralUtils.IsSecondMoonPrototypeEquipment(equipmentDef, out var equipment))
                        {
                            GameObject droplet = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Tier3Orb.prefab").WaitForCompletion().InstantiateClone("PrototypeEquipmentOrb", false);
                            Color colorLight = ColorCatalog.GetColor(SecondMoonModColors.PrototypeColorIndex);
                            Color colorDark = ColorCatalog.GetColor(SecondMoonModColors.PrototypeDarkColorIndex);

                            droplet.transform.GetChild(0).gameObject.GetComponent<TrailRenderer>().startColor = colorLight;
                            droplet.transform.GetChild(0).gameObject.GetComponent<TrailRenderer>().set_startColor_Injected(ref colorLight);

                            Light[] lights = droplet.GetComponentsInChildren<Light>();
                            foreach (Light thisLight in lights)
                            {
                                thisLight.color = colorLight;
                            }

                            ParticleSystem[] array = droplet.GetComponentsInChildren<ParticleSystem>();
                            foreach (ParticleSystem obj in array)
                            {
                                ParticleSystem.MainModule main = obj.main;
                                ParticleSystem.ColorOverLifetimeModule COL = obj.colorOverLifetime;
                                main.startColor = new ParticleSystem.MinMaxGradient(colorLight);
                                COL.color = colorLight;
                            }
                            equipmentDef.colorIndex = SecondMoonModColors.PrototypeColorIndex;
                            pickupDef.dropletDisplayPrefab = droplet;
                            pickupDef.baseColor = colorLight;
                            pickupDef.darkColor = colorDark;
                        }
                    }
                });
            }
        }

        //For some modifications we need to ensure they go after all others.
        //An example is Thunderbolt, because its non-attack speed stat buffs depend on attack speed bonuses. For it to work properly, all other modded attack speed buffs need to be applied beforehand.
        //This method is to handle these types of items and effects.
        private void FinalModificationsToStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            var cachedAttackSpeed = self.attackSpeed;
            var cachedHealth = self.maxHealth;
            var cachedShield = self.maxShield;
            var cachedCrit = self.crit;
            var cachedCritMult = self.critMultiplier;
            var totalSpeedIncrease = 0f;
            var totalAttackSpeedIncrease = 0f;
            var totalDamageIncrease = 0f;
            var totalCdr = 1f;

            if (Thunderbolt.IsEnabled)
            {
                var stackCount = Thunderbolt.instance.GetCount(self);
                if (stackCount > 0)
                {
                    var unchanged = self.baseAttackSpeed + self.levelAttackSpeed * (self.level - 1);
                    float increase = cachedAttackSpeed / unchanged - 1;
                    totalSpeedIncrease += increase * Thunderbolt.ThunderboltASToMS;
                    totalCdr *= GeneralUtils.HyperbolicScaling(increase * 0.5f) * Thunderbolt.ThunderboltASToCD * Thunderbolt.ThunderboltCDMultiplier;
                }
            }
            if (NarrowMagnifier.IsEnabled)
            {
                var stackCount = NarrowMagnifier.instance.GetCount(self);
                if (stackCount > 0)
                {
                    var bodyCrit = cachedCrit >= 100 ? 100 : cachedCrit;
                    totalDamageIncrease += (cachedCritMult - 1) * bodyCrit * NarrowMagnifier.NarrowMagnifierCCDmgConversion / 100;
                }
            }
            if (SharpVinegar.IsEnabled)
            {
                if (self.HasBuff(Sharp.instance.BuffDef))
                {
                    var bodyCrit = cachedCrit >= 100 ? 100 : cachedCrit;
                    totalAttackSpeedIncrease += (cachedCritMult - 1) * bodyCrit * SharpVinegar.SharpVinegarCCASConversion / 100;
                }
            }
            if (QuicksilverVest.IsEnabled)
            {
                var stackCount = QuicksilverVest.instance.GetCount(self);
                if (stackCount > 0)
                {
                    var unchangedHealth = self.baseMaxHealth + self.levelMaxHealth * (self.level - 1);
                    var healthBarIncrease = (cachedHealth + cachedShield) / unchangedHealth - 1;
                    if (healthBarIncrease > 0) 
                    {
                        totalSpeedIncrease += healthBarIncrease * QuicksilverVest.QuicksilverVestConversionRate;
                    }
                }
            }
            if (totalSpeedIncrease > 0)
            {
                self.moveSpeed *= 1 + totalSpeedIncrease;
            }
            if (totalDamageIncrease > 0)
            {
                self.damage *= 1 + totalDamageIncrease;
            }
            if (totalAttackSpeedIncrease > 0)
            {
                self.attackSpeed *= 1 + totalAttackSpeedIncrease;
            }
            if (self.skillLocator && totalCdr < 1)
            {
                if (self.skillLocator.primaryBonusStockSkill)
                {
                    self.skillLocator.primaryBonusStockSkill.cooldownScale *= 1 - totalCdr;
                }
                if (self.skillLocator.secondaryBonusStockSkill)
                {
                    self.skillLocator.secondaryBonusStockSkill.cooldownScale *= 1 - totalCdr;
                }
                if (self.skillLocator.utilityBonusStockSkill)
                {
                    self.skillLocator.utilityBonusStockSkill.cooldownScale *= 1 - totalCdr;
                }
                if (self.skillLocator.specialBonusStockSkill)
                {
                    self.skillLocator.specialBonusStockSkill.cooldownScale *= 1 - totalCdr;
                }
            }
        }

        private void Corrupt(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            foreach(Item item in VoidItemList)
            {
                if (item.ItemToCorrupt)
                {
                    ItemDef.Pair transformation = new ItemDef.Pair()
                    {
                        itemDef1 = item.ItemToCorrupt,
                        itemDef2 = item.ItemDef
                    };
                    ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
                }

                else if (item.ItemsToCorrupt != null)
                {
                    foreach (var target in item.ItemsToCorrupt)
                    {
                        ItemDef.Pair transformation = new ItemDef.Pair()
                        {
                            itemDef1 = target,
                            itemDef2 = item.ItemDef
                        };
                        ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
                    }
                }

                else if (item.ItemTierToCorrupt)
                {
                    foreach (var each in ContentManager.itemDefs)
                    {
                        if (each.tier == item.ItemTierToCorrupt.tier)
                        {
                            ItemDef.Pair transformation = new ItemDef.Pair()
                            {
                                itemDef1 = each,
                                itemDef2 = item.ItemDef
                            };
                            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
                        }
                    }
                }
            }
            orig();
        }
    }
}
