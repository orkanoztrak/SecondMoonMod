using System;
using System.Collections.Generic;
using System.Text;

namespace SecondMoon.Compatibility;

public static class Compatibilities
{
    public static class BetterUICompatibility
    {
        public static bool IsBetterUIInstalled => BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.xoxfaby.BetterUI");
    }
}
