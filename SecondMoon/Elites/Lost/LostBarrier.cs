using MonoMod.Cil;
using RoR2;
using SecondMoon.BuffsAndDebuffs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Items.Void.TwistedRegrets.TwistedRegrets;

namespace SecondMoon.Elites.Lost;

public class LostBarrier : Buff<LostBarrier>
{
    public override string Name => "Lost Barrier";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/BearVoid/texBuffBearVoidReady.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(124, 253, 234, 255);

    public override void Init()
    {
        CreateBuff();
        Hooks();
    }

    public override void Hooks()
    {
        IL.RoR2.HealthComponent.TakeDamage += PopBarrier;
    }

    private void PopBarrier(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdflda(typeof(HealthComponent), nameof(HealthComponent.itemCounts)),
                               x => x.MatchLdfld<HealthComponent.ItemCounts>("bear")))
        {
            if (cursor.TryGotoPrev(x => x.MatchDup()))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.EmitDelegate<Action<HealthComponent, DamageInfo>>((component, damageInfo) =>
                {
                    var victimBody = component.body;
                    if (victimBody.HasBuff(BuffDef) && damageInfo.attacker && !damageInfo.rejected && damageInfo.damage > 0)
                    {
                        var characterBody = damageInfo.attacker.GetComponent<CharacterBody>();
                        if (characterBody)
                        {
                            if (!(characterBody.teamComponent.teamIndex == victimBody.teamComponent.teamIndex) && victimBody.master && characterBody.master)
                            {
                                EffectData effectData = new EffectData
                                {
                                    origin = damageInfo.position,
                                    rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                                };
                                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BearVoidProc"), effectData, transmit: true);
                                damageInfo.rejected = true;
                                victimBody.RemoveBuff(BuffDef);
                                victimBody.AddTimedBuff(LostBarrierCooldown.instance.BuffDef, TwistedRegretsLostBarrierCooldown);
                                var retaliation = new DamageInfo
                                {
                                    damage = Util.OnHitProcDamage(victimBody.damage, victimBody.damage, TwistedRegretsLostBarrierDummyDamage),
                                    damageColorIndex = DamageColorIndex.Void,
                                    damageType = DamageType.Generic,
                                    attacker = victimBody.gameObject,
                                    crit = Util.CheckRoll(victimBody.crit, victimBody.master),
                                    force = damageInfo.force,
                                    inflictor = null,
                                    position = characterBody.corePosition,
                                    procCoefficient = 1,
                                    procChainMask = default
                                };
                                EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/VoidChestPurchaseEffect"), retaliation.position, Vector3.up, transmit: true);
                                GlobalEventManager.instance.OnHitEnemy(retaliation, characterBody.gameObject);
                                GlobalEventManager.instance.OnHitAll(retaliation, characterBody.gameObject);
                            }
                        }
                    }
                });
                if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(16)))
                {
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    cursor.EmitDelegate<Action<HealthComponent, DamageInfo>>((component, damageInfo) =>
                    {
                        var victimBody = component.body;
                        if (victimBody.HasBuff(BuffDef) && !damageInfo.rejected && damageInfo.damage > 0)
                        {
                            EffectData effectData2 = new EffectData
                            {
                                origin = damageInfo.position,
                                rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                            };
                            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BearVoidProc"), effectData2, transmit: true);
                            damageInfo.rejected = true;
                            victimBody.RemoveBuff(BuffDef);
                            victimBody.AddTimedBuff(LostBarrierCooldown.instance.BuffDef, TwistedRegretsLostBarrierCooldown / 2);
                        }
                    });
                }
            }
        }
    }
}
