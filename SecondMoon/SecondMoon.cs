using BepInEx;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SecondMoon.BuffsAndDebuffs;
using SecondMoon.Items;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static Facepunch.Steamworks.Inventory;
using static Facepunch.Steamworks.Workshop;

namespace SecondMoon
{
    [BepInDependency(ItemAPI.PluginGUID)]
    [BepInDependency(LanguageAPI.PluginGUID)]
    [BepInDependency(DotAPI.PluginGUID)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]

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

        public static HashSet<ItemDef> BlacklistedFromPrinter = new HashSet<ItemDef>();
        public void Awake()
        {
            Log.Init(Logger);

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
                item.Init();
                Logger.LogInfo("Item: " + item.ItemName + " Initialized!");
            }

#pragma warning disable Publicizer001
#pragma warning restore Publicizer001
        }
        // The Update() method is run on every frame of the game.
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.Info($"Player pressed Z. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.Feather.itemIndex), transform.position, transform.forward * 20f);
            }
            if (Input.GetKeyDown(KeyCode.T))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.Info($"Player pressed Z. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.Hoof.itemIndex), transform.position, transform.forward * 20f);
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                var transform = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;
                Log.Info($"Player pressed Z. Spawning our custom item at coordinates {transform.position}");
                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(RoR2Content.Items.Clover.itemIndex), transform.position, transform.forward * 20f);
            }

        }
    }
}
