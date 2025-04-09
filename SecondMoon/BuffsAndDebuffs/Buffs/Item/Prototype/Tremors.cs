using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;
using static SecondMoon.Items.Prototype.TremorKnuckles.TremorKnuckles;
using static UnityEngine.ParticleSystem.PlaybackState;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;

public class Tremors : Buff<Tremors>
{
    public override string Name => "Tremors";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/BearVoid/texBuffBearVoidReady.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(127, 99, 50, 255);

    public override void Hooks()
    {
        On.RoR2.GlobalEventManager.OnHitAllProcess += TremorsBombs;
    }

    private void TremorsBombs(On.RoR2.GlobalEventManager.orig_OnHitAllProcess orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
    {
        if (damageInfo.attacker && damageInfo.procCoefficient > 0)
        {
            var attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody)
            {
                if (attackerBody.HasBuff(BuffDef))
                {
                    var damage = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, TremorKnucklesBombDamage);
                    ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                    {
                        projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LightningStake"),
                        position = damageInfo.position,
                        rotation = Quaternion.identity,
                        owner = damageInfo.attacker,
                        damage = damage,
                        force = 0f,
                        crit = damageInfo.crit,
                        damageColorIndex = DamageColorIndex.Item,
                        target = null,
                        speedOverride = -1f,
                        damageTypeOverride = default
                    });
                }
            }
        }
        orig(self, damageInfo, hitObject);
    }

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }
}
