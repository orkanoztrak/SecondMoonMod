using R2API;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;
using RoR2;

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

    // dt_bind 1 "no_enemies; give_item hoof 30; give_item feather 5; kill_all;"
}
