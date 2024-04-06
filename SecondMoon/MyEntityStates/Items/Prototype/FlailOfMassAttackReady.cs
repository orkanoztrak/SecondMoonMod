using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking.NetworkSystem;

namespace SecondMoon.MyEntityStates.Items.Prototype;

public class FlailOfMassAttackReady : FlailOfMassBase
{
    Indicator targetIndicator;
    private HurtBox trackingTarget;
    private Transform trackingIndicatorTransform;

    public static float maxTrackingDistance = 20f;
    public static float maxTrackingAngle = 20f;
    public override void OnEnter()
    {
        base.OnEnter();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (characterBody)
        {
            float extraRaycastDistance = 0f;
            Ray ray = CameraRigController.ModifyAimRayIfApplicable(GetAimRay(), base.gameObject, out extraRaycastDistance);
            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = ray.origin;
            bullseyeSearch.searchDirection = ray.direction;
            bullseyeSearch.maxDistanceFilter = maxTrackingDistance + extraRaycastDistance;
            bullseyeSearch.maxAngleFilter = maxTrackingAngle;
            bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
            bullseyeSearch.teamMaskFilter.RemoveTeam(TeamComponent.GetObjectTeam(base.gameObject));
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
            bullseyeSearch.RefreshCandidates();
            trackingTarget = bullseyeSearch.GetResults().FirstOrDefault();
        }
        if (trackingTarget)
        {
            if (!trackingIndicatorTransform)
            {
                trackingIndicatorTransform = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/ShieldTransferIndicator"), trackingTarget.transform.position, Quaternion.identity).transform;
            }
            trackingIndicatorTransform.position = trackingTarget.transform.position;
        }
        else if (trackingIndicatorTransform)
        {
            Destroy(trackingIndicatorTransform.gameObject);
            trackingIndicatorTransform = null;
        }
    }
}
