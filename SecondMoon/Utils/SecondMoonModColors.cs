using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.Utils;

internal static class SecondMoonModColors
{
    public static DamageColorIndex NarrowMagnifierStrongHitColor { get; private set; }
    public static DamageColorIndex NarrowMagnifierWeakHitColor { get; private set; }

    public static ColorCatalog.ColorIndex PrototypeColorIndex { get; private set; }
    public static ColorCatalog.ColorIndex PrototypeDarkColorIndex { get; private set; }

    public static void Init()
    {
        NarrowMagnifierStrongHitColor = ColorsAPI.RegisterDamageColor(new Color32(0, 129, 255, 255));
        NarrowMagnifierWeakHitColor = ColorsAPI.RegisterDamageColor(new Color32(107, 181, 255, 255));

        PrototypeColorIndex = ColorsAPI.RegisterColor(new Color32(124, 253, 234, 255));
        PrototypeDarkColorIndex = ColorsAPI.RegisterColor(new Color32(124, 253, 234, 255));      
    }
}
