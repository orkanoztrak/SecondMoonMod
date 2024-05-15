using BepInEx;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SecondMoon.BuffsAndDebuffs;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.AddressableAssets;

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
    //[BepInDependency(ColorsAPI.PluginGUID)]
    [BepInDependency("com.xoxfaby.BetterUI", BepInDependency.DependencyFlags.SoftDependency)]

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

        internal static BepInEx.Configuration.ConfigFile MainConfig;

        public static HashSet<ItemDef> BlacklistedFromPrinter = new HashSet<ItemDef>();
        public static List<Items.Item> VoidItemList = new List<Items.Item>();
        public static ExpansionDef DLC1;
        public void Awake()
        {
            Log.Init(Logger);
            MainConfig = Config;

            DLC1 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            var BuffTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Buff)));
            Logger.LogInfo("----------------------BUFFS--------------------");

            foreach (var buffType in BuffTypes)
            {
                Buff buff = (Buff)System.Activator.CreateInstance(buffType);
                buff.Init();
                Logger.LogInfo("Buff: " + buff.Name + " Initialized!");
            }

            var DotTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(DOT)));
            Logger.LogInfo("----------------------DOTS---------------------");

            foreach (var dotType in DotTypes)
            {
                DOT dot = (DOT)System.Activator.CreateInstance(dotType);
                dot.Init();
                Logger.LogInfo("DOT: " + dot.AssociatedBuffName + " Initialized!");
            }


            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Items.Item)));

            Logger.LogInfo("----------------------ITEMS--------------------");

            foreach (var itemType in ItemTypes)
            {
                Items.Item item = (Items.Item)System.Activator.CreateInstance(itemType);
                item.Init(Config);
                if (item.EnableCheck)
                {
                    Logger.LogInfo("Item: " + item.ItemName + " Initialized!");
                    if (item.ItemToCorrupt)
                    {
                        VoidItemList.Add(item);
                    }
                }
            }

            On.RoR2.Items.ContagiousItemManager.Init += Corrupt;

            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(Equipment.Equipment)));

            Logger.LogInfo("--------------------EQUIPMENT------------------");

            foreach (var equipmentType in EquipmentTypes)
            {
                Equipment.Equipment equipment = (Equipment.Equipment)System.Activator.CreateInstance(equipmentType);
                equipment.Init();
                Logger.LogInfo("Equipment: " + equipment.EquipmentName + " Initialized!");
            }
        }

        private void Corrupt(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            foreach(Items.Item item in VoidItemList)
            {
                ItemDef.Pair transformation = new ItemDef.Pair()
                {
                    itemDef1 = item.ItemToCorrupt,
                    itemDef2 = item.ItemDef
                };
                ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            }
            orig();
        }
    }
}
