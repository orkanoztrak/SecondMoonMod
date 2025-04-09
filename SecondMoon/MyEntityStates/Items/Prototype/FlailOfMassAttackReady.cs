using RoR2;
using RoR2.Orbs;
using SecondMoon.BuffsAndDebuffs.Buffs.Item.Prototype;
using SecondMoon.Items.Prototype.FlailOfMass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

namespace SecondMoon.MyEntityStates.Items.Prototype;

public class FlailOfMassAttackReady : FlailOfMassBase
{
    private Indicator targetIndicator;
    private HurtBox trackingTarget;
    private Transform trackingIndicatorTransform;

    private float trackerUpdateStopwatch;
    public float trackerUpdateFrequency = 10f;

    public static float maxTrackingDistance = 60f;
    public static float maxTrackingAngle = 20f;
    public override void OnEnter()
    {
        base.OnEnter();
        targetIndicator = new Indicator(gameObject, LegacyResourcesAPI.Load<GameObject>("Prefabs/HuntressTrackingIndicator"));
        if (body)
        {
            body.onSkillActivatedServer += GIGABONK;
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        targetIndicator = null;
        if (body)
        {
            body.onSkillActivatedServer -= GIGABONK;
        }
    }

    private void GIGABONK(GenericSkill skill)
    {
        if (body)
        {
            if (body.GetBuffCount(Momentum.instance.BuffDef.buffIndex) >= FlailOfMass.FlailOfMassMomentumLimit)
            {
                SkillLocator skillLocator = body.skillLocator;
                if ((skillLocator?.primary) == skill)
                {
                    HurtBox hurtBox = trackingTarget;
                    if (hurtBox)
                    {
                        var stackCount = body.inventory.GetItemCount(FlailOfMass.instance.ItemDef) > 0 ? body.inventory.GetItemCount(FlailOfMass.instance.ItemDef) : 1;
                        FlailOrb flail = new FlailOrb
                        {
                            damageValue = body.damage * (FlailOfMass.FlailOfMassDamageMultiplierInit + ((stackCount - 1) * FlailOfMass.FlailOfMassDamageMultiplierStack)),
                            isCrit = Util.CheckRoll(body.crit, body.master),
                            teamIndex = TeamComponent.GetObjectTeam(body.gameObject),
                            attacker = body.gameObject,
                            procCoefficient = FlailOfMass.FlailOfMassProcCoefficient,
                            damageColorIndex = DamageColorIndex.Item,
                            scale = 1f
                        };
                        if (hurtBox)
                        {
                            flail.origin = body.aimOriginTransform.position;
                            flail.target = trackingTarget;
                            OrbManager.instance.AddOrb(flail);
                            while (body.GetBuffCount(Momentum.instance.BuffDef.buffIndex) > 0)
                            {
                                body.RemoveBuff(Momentum.instance.BuffDef);
                            }
                        }
                    }
                }
            }
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        trackerUpdateStopwatch += Time.fixedDeltaTime;
        if (body && isAuthority)
        {
            if (body.GetBuffCount(Momentum.instance.BuffDef.buffIndex) < FlailOfMass.FlailOfMassMomentumLimit)
            {
                outer.SetNextState(new FlailOfMassBuildingMomentum());
                return;
            }
            if (trackerUpdateStopwatch >= 1f / trackerUpdateFrequency)
            {
                trackerUpdateStopwatch -= 1f / trackerUpdateFrequency;
                Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), body.gameObject, out float num);
                BullseyeSearch bullseyeSearch = new BullseyeSearch
                {
                    searchOrigin = ray.origin,
                    searchDirection = ray.direction,
                    maxDistanceFilter = maxTrackingDistance + num,
                    maxAngleFilter = maxTrackingAngle,
                    teamMaskFilter = TeamMask.allButNeutral,
                    viewer = body
                };
                bullseyeSearch.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(body.gameObject));
                bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
                bullseyeSearch.RefreshCandidates();
                bullseyeSearch.FilterOutGameObject(body.gameObject);
                trackingTarget = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
            }
        }
        if (trackingTarget)
        {
            if (!trackingIndicatorTransform)
            {
                trackingIndicatorTransform = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/ShieldTransferIndicator"), trackingTarget.transform.position, Quaternion.identity).transform;
            }
            trackingIndicatorTransform.position = trackingTarget.transform.position;
            targetIndicator.targetTransform = trackingIndicatorTransform;
        }
        else if (trackingIndicatorTransform)
        {
            Destroy(trackingIndicatorTransform.gameObject);
            trackingIndicatorTransform = null;
            targetIndicator.targetTransform = null;
        }
    }
}
