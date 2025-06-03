using RoR2;
using SecondMoon.Interactables.Purchase.TakesItem.AwakeningShrine;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.MyEntityStates.Interactables;

public class AwakeningShrinePrepareBossSpawn : AwakeningShrineBaseState
{
    private float duration;
    private float stopwatch;
    private ShakeEmitter shaker;

    public override void OnEnter()
    {
        base.OnEnter();
        shaker = outer.gameObject.transform.Find("BossSpawnFX").gameObject.GetComponent<ShakeEmitter>();
        duration = shaker.duration;
        shaker.gameObject.SetActive(true);
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        stopwatch += Time.fixedDeltaTime;
        if (stopwatch >= duration)
        {
            shaker.gameObject.SetActive(false);
            stopwatch = 0f;
            outer.SetNextState(new AwakeningShrineBossFight());
        }
    }
}
