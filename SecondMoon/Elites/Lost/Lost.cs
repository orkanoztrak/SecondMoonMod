using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.Elites.Lost;

public class Lost : Elite<Lost>
{
    public override string EliteName => "Lost";

    public override string EliteToken => "AFFIX_LOST";

    public override Color32 Color => new Color32(124, 253, 234, 255);

    public override float HealthBoostCoefficient => 1f;

    public override float DamageBoostCoefficient => 1f;

    public override Texture2D EliteRamp => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/ColorRamps/texRampMagmaWorm.png").WaitForCompletion();

    public override void Init()
    {
        CreateLang();
        CreateElite();
    }
}
