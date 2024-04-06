using R2API;
using RoR2;
using SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Prototype;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3.Gash;

namespace SecondMoon.BuffsAndDebuffs.Debuffs.Dots.Item.Tier3;

public class Gash : DOT<Gash>
{
    public override float Interval => 0.6f;

    public override float DamageCoefficient => 0.2f;

    public override DamageColorIndex DamageColorIndex => DamageColorIndex.SuperBleed;

    public override string AssociatedBuffName => "GashDebuff";

    public override void Hooks()
    {
        On.RoR2.DotController.Awake += DotController_Awake;
    }

    private static void DotController_Awake(On.RoR2.DotController.orig_Awake orig, DotController self)
    {
        orig(self);
        if (!self.GetComponent<DotVisualTracker>())
            self.gameObject.AddComponent<DotVisualTracker>();
    }

    public override void Init()
    {
        CreateCustomDotVisual();
        SetAssociatedBuff();
        Hooks();
        CreateDOT();
    }

    public override void SetAssociatedBuff()
    {
        this.AssociatedBuff = GashDebuff.instance.BuffDef;
    }

    public override void CreateCustomDotVisual()
    {
        CustomDotVisual = new DotAPI.CustomDotVisual((target) => 
        {
            var dotVisualTracker = target.GetComponent<DotVisualTracker>();
            if (!dotVisualTracker)
                return;
            if (target.HasDotActive(instance.DotIndex))
            {
                if (!dotVisualTracker.GashEffect)
                {
                    dotVisualTracker.GashEffect = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/BleedEffect"), target.transform);
                }
            }
            else if (dotVisualTracker.GashEffect)
            {
                UnityEngine.Object.Destroy(dotVisualTracker.GashEffect);
                dotVisualTracker.GashEffect = null;
            }
        });
    }

    public class DotVisualTracker : MonoBehaviour
    {
        public GameObject GashEffect;
    }
}
