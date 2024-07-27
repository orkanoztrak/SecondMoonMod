using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.Compatibility;

public static class Compatibilities
{
    public static class LookingGlassCompatibility
    {
        private static bool? _IsLookingGlassInstalled;

        public static bool IsLookingGlassInstalled
        {
            get
            {
                _IsLookingGlassInstalled ??= BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("droppod.lookingglass");
                return (bool)_IsLookingGlassInstalled;
            }
        }
    }
}
