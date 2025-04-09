using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SecondMoon.EquipmentlessElites.Lost;

public class Lost : EquipmentlessElite<Lost>
{
    public override string EliteName => "Lost";

    public override string EliteToken => "AFFIX_LOST";

    public override float HealthBoostCoefficient => 1f;

    public override float DamageBoostCoefficient => 1f;

    public override Texture2D EliteRamp => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/Common/ColorRamps/texRampBeamSphere.png").WaitForCompletion();

    public override void Init()
    {
        CreateLang();
        CreateElite();
    }
}
