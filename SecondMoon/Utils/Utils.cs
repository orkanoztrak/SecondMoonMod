using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;
using BepInEx.Configuration;

namespace SecondMoon.Utils;

public static class Utils
{
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

    /*[ConCommand(commandName = "test_command", flags = ConVarFlags.None, helpText = "Scrap command to do whatever I want.")]
    private static void TestCommand(ConCommandArgs args)
    {
        foreach (var projectile in ProjectileCatalog.projectilePrefabs)
        {
            if (projectile.GetComponent<RoR2.Projectile.ProjectileSingleTargetImpact>() != null)
            {
                Debug.Log(projectile.name);
            }
        }
    }*/

    // dt_bind 1 ";"
    public static float HyperbolicScaling(float value)
    {
        return 1 - 1 / (1 + value);
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
