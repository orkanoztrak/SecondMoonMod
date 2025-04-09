using EntityStates;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Skills;
using RoR2.UI;
using SecondMoon.BuffsAndDebuffs;
using SecondMoon.EquipmentlessElites.Lost;
using SecondMoon.Items.Void.TwistedRegrets;
using SecondMoon.Utils;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.UI.HealthBar;
using static SecondMoon.Items.Void.TwistedRegrets.TwistedRegrets;

namespace SecondMoon.BuffsAndDebuffs.Buffs.Item.Void;

public class LostBuff : Buff<LostBuff>
{
    public static Color HealthBarColor = new Color32(124, 253, 234, 255);
    public static Color ShieldBarColor = Color.yellow;
    public override string Name => "Lost";

    public override Sprite IconSprite => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/ElementalRingVoid/texBuffElementalRingVoidReadyIcon.tif").WaitForCompletion();

    public override Color BuffColor => new Color32(0, 0, 0, 255);

    public override bool CanStack => false;

    public override EliteDef EliteDef => Lost.instance.EliteDef;

    public Material EliteMaterial = Addressables.LoadAssetAsync<Material>("RoR2/Base/InvadingDoppelganger/matDoppelganger.mat").WaitForCompletion();

    public override void Hooks()
    {
        On.RoR2.CharacterBody.OnBuffFirstStackGained += AddLostController;
        On.RoR2.CharacterBody.OnBuffFinalStackLost += RemoveLostController;
        SkillSprintingFixes();
        IL.RoR2.CharacterBody.RecalculateStats += ModifyStats;
        On.RoR2.GlobalEventManager.ProcessHitEnemy += AddDebuffsAndLaunchMissiles;
        IL.RoR2.HealthComponent.TakeDamageProcess += SplitOsp;
        IL.RoR2.UI.HealthBar.UpdateBarInfos += SetupBarColors;
        IL.RoR2.HealthComponent.GetHealthBarValues += ChangeOspFractionDisplayCalc;
        //On.RoR2.CharacterBody.FixedUpdate += OverlayManager;
    }

    private void OverlayManager(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
    {
        if (self.modelLocator && self.modelLocator.modelTransform && self.HasBuff(BuffDef) && !self.GetComponent<EliteOverlayManager>())
        {
            var overlay = TemporaryOverlayManager.AddOverlay(self.modelLocator.modelTransform.gameObject);
            overlay.duration = float.PositiveInfinity;
            overlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
            overlay.animateShaderAlpha = true;
            overlay.destroyComponentOnEnd = true;
            overlay.originalMaterial = EliteMaterial;
            overlay.AddToCharacterModel(self.modelLocator.modelTransform.GetComponent<CharacterModel>());
            var EliteOverlayManager = self.gameObject.AddComponent<EliteOverlayManager>();
            EliteOverlayManager.Overlay = overlay;
            EliteOverlayManager.Body = self;
            EliteOverlayManager.EliteBuffDef = BuffDef;
        }
        orig(self);
    }

    public class EliteOverlayManager : MonoBehaviour
    {
        public TemporaryOverlayInstance Overlay;
        public CharacterBody Body;
        public BuffDef EliteBuffDef;

        public void FixedUpdate()
        {
            if (!Body.HasBuff(EliteBuffDef))
            {
                Destroy(this);
                Overlay.CleanupEffect();
            }
        }
    }

    private void SkillSprintingFixes()
    {
        IL.RoR2.Skills.SkillDef.OnFixedUpdate += SkillSprintFix;
        IL.RoR2.Skills.SkillDef.OnExecute += MakeAgile;
        IL.EntityStates.Bandit2.Weapon.BaseSidearmState.FixedUpdate += BanditSpecialSprintFix;
        IL.EntityStates.Railgunner.Scope.BaseActive.FixedUpdate += RailgunnerSecondarySprintFix;
        IL.EntityStates.Toolbot.FireNailgun.FixedUpdate += MULTPrimaryNailgunSprintFix;
        IL.EntityStates.Toolbot.ToolbotDualWieldBase.FixedUpdate += MULTSpecialDualWieldSprintFix;
        IL.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.FixedUpdate += VoidFiendCorruptedPrimarySprintFix;
        IL.RoR2.UI.CrosshairManager.UpdateCrosshair += SprintingVanishingCrosshairFix;
    }

    private void SprintingVanishingCrosshairFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>("get_isSprinting")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<bool, CharacterBody, bool>>((isSprinting, targetBody) =>
            {
                return isSprinting && !targetBody.HasBuff(BuffDef);
            });
        }
    }

    private void VoidFiendCorruptedPrimarySprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>("get_isSprinting")))
        {
            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam, bool>>((isSprinting, state) =>
            {
                return isSprinting && !state.characterBody.HasBuff(BuffDef);
            });
        }
    }

    private void MULTSpecialDualWieldSprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchRet()))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(EntityState), nameof(EntityState.outer))))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<EntityStates.Toolbot.ToolbotDualWieldBase, bool>>((state) =>
                {
                    if (state.characterBody.HasBuff(BuffDef))
                    {
                        return true;
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }

    }

    private void MULTPrimaryNailgunSprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchRet()))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(EntityState), nameof(EntityState.outer))))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<EntityStates.Toolbot.FireNailgun, bool>>((state) =>
                {
                    if (state.characterBody.HasBuff(BuffDef))
                    {
                        if (state.IsKeyDownAuthority())
                        {
                            return true;
                        }
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }
    }

    private void RailgunnerSecondarySprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchRet()))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(EntityState), nameof(EntityState.outer))))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<EntityStates.Railgunner.Scope.BaseActive, bool>>((state) =>
                {
                    if (state.characterBody.HasBuff(BuffDef))
                    {
                        if (state.IsKeyDownAuthority())
                        {
                            return true;
                        }
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }
    }

    private void BanditSpecialSprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchRet()))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(EntityState), nameof(EntityState.outer))))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<EntityStates.Bandit2.Weapon.BaseSidearmState, bool>>((state) =>
                {
                    if (state.characterBody.HasBuff(BuffDef))
                    {
                        return true;
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }
    }

    private void ChangeOspFractionDisplayCalc(ILContext il)
    {
        var numIndex = 0;
        var num2Index = 1;
        var num3Index = 2;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(num2Index)))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(num3Index)))
            {
                ILLabel target2 = cursor.MarkLabel();
                if (cursor.TryGotoPrev(MoveType.After, x => x.MatchStloc(num2Index)))
                {
                    cursor.MoveBeforeLabels();
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<HealthComponent, bool>>((component) =>
                    {
                        if (component.body.HasBuff(BuffDef))
                        {
                            return true;
                        }
                        return false;
                    });
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, target.Target);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, numIndex);
                    cursor.EmitDelegate<Func<HealthComponent, float, float>>((component, num) =>
                    {
                        return component.body.oneShotProtectionFraction * component.fullHealth - (component.fullHealth - (component.health - component.barrier));
                    });
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, num3Index);
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Br, target2.Target);
                }
            }
        }
    }

    private void SetupBarColors(ILContext il)
    {
        var ptr3Index = 4;
        var healthBarValuesIndex = 1;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdflda(typeof(HealthBar), nameof(HealthBar.barInfoCollection)),
                               x => x.MatchLdflda(typeof(BarInfoCollection), nameof(BarInfoCollection.shieldBarInfo))))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoNext(x => x.MatchLdloc(ptr3Index),
                                   x => x.MatchLdloc(healthBarValuesIndex),
                                   x => x.MatchLdfld(typeof(HealthComponent.HealthBarValues), nameof(HealthComponent.HealthBarValues.shieldFraction)),
                                   x => x.MatchLdloca(0)))
            {
                ILLabel target2 = cursor.MarkLabel();
                if (cursor.TryGotoPrev(x => x.MatchLdloc(healthBarValuesIndex),
                                       x => x.MatchLdfld(typeof(HealthComponent.HealthBarValues), nameof(HealthComponent.HealthBarValues.hasVoidShields))))
                {
                    cursor.MoveBeforeLabels();
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<HealthBar, bool>>((healthBar) =>
                    {
                        if (healthBar.source.body.HasBuff(BuffDef))
                        {
                            healthBar.barInfoCollection.shieldBarInfo.color = ShieldBarColor;
                            return true;
                        }
                        return false;
                    });
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target2.Target);
                    if (cursor.TryGotoPrev(x => x.MatchLdloc(healthBarValuesIndex),
                                           x => x.MatchLdfld(typeof(HealthComponent.HealthBarValues), nameof(HealthComponent.HealthBarValues.isVoid))))
                    {
                        cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                        cursor.EmitDelegate<Func<HealthBar, bool>>((healthBar) =>
                        {
                            if (healthBar.source.body.HasBuff(BuffDef))
                            {
                                healthBar.barInfoCollection.trailingOverHealthbarInfo.color = HealthBarColor;
                                return true;
                            }
                            return false;
                        });
                        cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
                    }
                }
            }
        }
    }

    private void SplitOsp(ILContext il)
    {
        var damageIndex = 7;
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
                               x => x.MatchCallOrCallvirt<CharacterBody>("get_hasOneShotProtection")))
        {
            if (cursor.TryGotoNext(x => x.MatchLdarg(0)))
            {
                ILLabel target = cursor.MarkLabel(); //if no buff, jump here
                if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<HealthComponent>("TriggerOneShotProtection")))
                {
                    ILLabel target2 = cursor.MarkLabel(); //jump here after code
                    if (cursor.TryGotoPrev(x => x.MatchLdarg(0),
                                   x => x.MatchLdfld(typeof(HealthComponent), nameof(HealthComponent.body)),
                                   x => x.MatchCallOrCallvirt<CharacterBody>("get_hasOneShotProtection")))
                    {
                        if (cursor.TryGotoNext(x => x.MatchLdarg(0)))
                        {
                            cursor.MoveBeforeLabels();
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                            cursor.EmitDelegate<Func<HealthComponent, bool>>((component) =>
                            {
                                if (component.body.HasBuff(BuffDef))
                                {
                                    return true;
                                }
                                return false;
                            });
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Brfalse, target.Target);
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldloc, damageIndex);
                            cursor.EmitDelegate<Func<HealthComponent, DamageInfo, float, float>>((component, damageInfo, incoming) =>
                            {
                                var body = component.body;
                                if (body.hasOneShotProtection && (damageInfo.damageType & DamageType.BypassOneShotProtection) != DamageType.BypassOneShotProtection && body.oneShotProtectionFraction > 0)
                                {
                                    if (component.shield + component.barrier >= component.fullShield * (1 - body.oneShotProtectionFraction))
                                    {
                                        float threshold = (component.fullShield + component.barrier) * (1f - body.oneShotProtectionFraction);
                                        float damageTaken = Mathf.Max(0f, threshold - component.serverDamageTakenThisUpdate);
                                        float num = incoming;
                                        incoming = Mathf.Min(incoming, damageTaken);
                                        if (incoming != num)
                                        {
                                            component.TriggerOneShotProtection();
                                        }
                                    }
                                    else
                                    {
                                        float threshold = (component.fullHealth + component.shield + component.barrier) * (1f - body.oneShotProtectionFraction);
                                        float damageTaken = Mathf.Max(0f, threshold - component.serverDamageTakenThisUpdate);
                                        float num = incoming;
                                        incoming = Mathf.Min(incoming, damageTaken);
                                        if (incoming != num)
                                        {
                                            component.TriggerOneShotProtection();
                                        }
                                    }
                                }
                                return incoming;
                            });
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Stloc, damageIndex);
                            cursor.Emit(Mono.Cecil.Cil.OpCodes.Br, target2.Target);
                        }
                    }
                }
            }
        }
    }

    private void AddDebuffsAndLaunchMissiles(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
    {
        CharacterBody attackerBody = null;
        HealthComponent victimComponent = null;
        bool secondHalf = false;
        if (damageInfo.attacker && damageInfo.procCoefficient > 0 && NetworkServer.active && !damageInfo.rejected)
        {
            attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            victimComponent = victim.GetComponent<HealthComponent>();
            if (attackerBody && victimComponent)
            {
                if (attackerBody.HasBuff(BuffDef))
                {
                    secondHalf = true;
                    var attacker = attackerBody.master;
                    if (attacker)
                    {
                        damageInfo.procChainMask.AddProc(ProcType.FractureOnHit);
                        DotController.DotDef dotDef = DotController.GetDotDef(DotController.DotIndex.Fracture);
                        DotController.InflictDot(victim, damageInfo.attacker, DotController.DotIndex.Fracture, dotDef.interval, 1f);
                        victimComponent.body.AddTimedBuff(RoR2Content.Buffs.Cripple, 3f);
                    }
                }
            }
        }
        orig(self, damageInfo, victim);
        if (secondHalf)
        {
            var debuffs = 0;

            BuffIndex[] debuffBuffIndices = BuffCatalog.debuffBuffIndices;
            foreach (BuffIndex buffType in debuffBuffIndices)
            {
                if (victimComponent.body.HasBuff(buffType))
                {
                    debuffs++;
                }
            }
            DotController dotController = DotController.FindDotController(victim.gameObject);
            if ((bool)dotController)
            {
                for (DotController.DotIndex dotIndex = 0; dotIndex < (DotController.DotIndex)(DotAPI.VanillaDotCount + DotAPI.CustomDotCount); dotIndex++)
                {
                    if (dotController.HasDotActive(dotIndex))
                    {
                        debuffs++;
                    }
                }
            }

            for (int i = 0; i < debuffs; i++)
            {
                LostOrb lostOrb = new LostOrb
                {
                    origin = attackerBody.aimOrigin,
                    damageValue = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, TwistedRegretsLostOrbDamage) * damageInfo.procCoefficient,
                    isCrit = damageInfo.crit,
                    attacker = damageInfo.attacker,
                    procChainMask = default,
                    procCoefficient = 0,
                    damageColorIndex = DamageColorIndex.Void
                };
                if (attackerBody.teamComponent)
                {
                    lostOrb.teamIndex = attackerBody.teamComponent.teamIndex;
                }
                else
                {
                    lostOrb.teamIndex = TeamIndex.Neutral;
                }

                HurtBox mainHurtBox = victimComponent.body.mainHurtBox;
                if ((bool)mainHurtBox)
                {
                    lostOrb.target = mainHurtBox;
                    OrbManager.instance.AddOrb(lostOrb);
                }
            }
        }
    }

    private void RemoveLostController(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
    {
        if (buffDef.Equals(BuffDef) && self.gameObject.GetComponent<LostController>())
        {
            var controller = self.gameObject.GetComponent<LostController>();
            controller.enabled = false;
            controller.body = null;
            UnityEngine.Object.Destroy(controller);
        }
        orig(self, buffDef);
    }

    private void AddLostController(On.RoR2.CharacterBody.orig_OnBuffFirstStackGained orig, CharacterBody self, BuffDef buffDef)
    {
        if (buffDef.Equals(BuffDef) && !self.gameObject.GetComponent<LostController>())
        {
            var controller = self.gameObject.AddComponent<LostController>();
            controller.body = self;
            controller.enabled = true;
        }
        orig(self, buffDef);
    }

    private void SkillSprintFix(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt(typeof(EntityStateMachine), nameof(EntityStateMachine.SetNextStateToMain))))
        {
            ILLabel target = cursor.MarkLabel();
            if (cursor.TryGotoPrev(x => x.MatchLdarg(1),
                       x => x.MatchCallOrCallvirt<GenericSkill>("get_stateMachine"),
                       x => x.MatchCallOrCallvirt(typeof(EntityStateMachine), nameof(EntityStateMachine.SetNextStateToMain))))
            {
                cursor.MoveBeforeLabels();
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<GenericSkill, bool>>((skillSlot) =>
                {
                    if (skillSlot.characterBody.HasBuff(BuffDef) && skillSlot.skillDef.skillName != "CrocoSlash")
                    {
                        return true;
                    }
                    return false;
                });
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
            }
        }
    }

    private void ModifyStats(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchCallOrCallvirt<CharacterBody>("get_oneShotProtectionFraction")))
        {
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>("set_oneShotProtectionFraction")))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<CharacterBody>>((body) =>
                {
                    if (body.HasBuff(BuffDef))
                    {
                        var half = (body.maxHealth + body.maxShield) / 2;
                        half *= 1 + TwistedRegretsLostHealthAndShieldBoost;
                        body.maxHealth = body.maxShield = half;
                        body.moveSpeed *= 1 + TwistedRegretsLostMovement;
                    }
                });
            }
        }
    }

    private void MakeAgile(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (cursor.TryGotoNext(x => x.MatchLdarg(0),
                               x => x.MatchLdfld<SkillDef>("cancelSprintingOnActivation")))
        {
            if (cursor.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<CharacterBody>("set_isSprinting")))
            {
                ILLabel target = cursor.MarkLabel();
                if (cursor.TryGotoPrev(x => x.MatchLdarg(1)))
                {
                    cursor.MoveBeforeLabels();
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_1);
                    cursor.EmitDelegate<Func<GenericSkill, bool>>((skillSlot) =>
                    {
                        if (skillSlot.characterBody.HasBuff(BuffDef) && skillSlot.skillDef.skillName != "CrocoSlash")
                        {
                            return true;
                        }
                        return false;
                    });
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Brtrue, target.Target);
                }
            }
        }
    }

    public override void Init()
    {
        CreateBuff();
        ExtraHealthBarSegments.AddType<ShieldOspBarData>();
        Hooks();
    }

    public class LostController : MonoBehaviour
    {
        public CharacterBody body;
        bool omniSprint;
        bool fallImmunity;
        private void Awake()
        {
            enabled = false;
        }

        private void OnEnable()
        {
            if (body)
            {
                omniSprint = body.bodyFlags.HasFlag(CharacterBody.BodyFlags.SprintAnyDirection);
                if (!omniSprint)
                {
                    body.bodyFlags |= CharacterBody.BodyFlags.SprintAnyDirection;
                }
                fallImmunity = body.bodyFlags.HasFlag(CharacterBody.BodyFlags.IgnoreFallDamage);
                if (!fallImmunity)
                {
                    body.bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage;
                }
            }
        }

        private void OnDisable()
        {
            if (body)
            {
                if (!omniSprint)
                {
                    body.bodyFlags &= ~CharacterBody.BodyFlags.SprintAnyDirection;
                }
                if (!fallImmunity)
                {
                    body.bodyFlags &= ~CharacterBody.BodyFlags.IgnoreFallDamage;
                }
                if (body.HasBuff(LostBarrier.instance.BuffDef))
                {
                    body.RemoveBuff(LostBarrier.instance.BuffDef);
                }
                if (body.HasBuff(LostBarrierCooldown.instance.BuffDef))
                {
                    body.RemoveBuff(LostBarrierCooldown.instance.BuffDef);
                }
            }
        }

        private void FixedUpdate()
        {
            if (NetworkServer.active && body && !body.HasBuff(LostBarrier.instance.BuffDef) && !body.HasBuff(LostBarrierCooldown.instance.BuffDef))
            {
                body.AddBuff(LostBarrier.instance.BuffDef);
            }
        }
    }

    public class ShieldOspBarData : ExtraHealthBarSegments.BarData
    {
        public override HealthBarStyle.BarStyle GetStyle()
        {
            var style = bar.style.ospStyle;
            style.sizeDelta = bar.style.ospStyle.sizeDelta;
            return style;
        }

        public override void UpdateInfo(ref BarInfo info, HealthComponent.HealthBarValues healthBarValues)
        {
            var component = bar.source;
            var threshold = component.fullShield * (1 - component.body.oneShotProtectionFraction);
            var curse = 1 - healthBarValues.curseFraction;
            info.enabled = bar.source.body.HasBuff(instance.BuffDef) && component.shield + component.barrier >= threshold;
            info.normalizedXMin = bar.source.health / bar.source.fullCombinedHealth * curse;
            float num = curse / component.fullCombinedHealth;
            float num2 = component.body.oneShotProtectionFraction * component.fullShield - (component.fullShield - (component.shield - component.barrier));
            info.normalizedXMax = info.normalizedXMin + num * num2;
            if (info.normalizedXMin >= info.normalizedXMax)
            {
                info.normalizedXMax = info.normalizedXMin;
            }
            /*if (info.enabled)
            {
                Debug.Log("Bar positions:\nMin x: " + info.normalizedXMin + "\nMax x: " + info.normalizedXMax);
            }*/
            base.UpdateInfo(ref info, healthBarValues);
        }
    }
}
