using EntityStates;
using RoR2;
using SecondMoon.MyEntityStates.Items.Tier2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SecondMoon.MyEntityStates.Items.Prototype;

public class FlailOfMassBase : BaseState
{
    private static readonly List<FlailOfMassBase> instancesList = new List<FlailOfMassBase>();

    protected NetworkedBodyAttachment networkedBodyAttachment;

    protected GameObject bodyGameObject;

    protected CharacterBody body;

    protected CharacterMotor bodyMotor;

    public static FlailOfMassBase FindForBody(CharacterBody body)
    {
        for (int i = 0; i < instancesList.Count; i++)
        {
            if ((object)instancesList[i].body == body)
            {
                return instancesList[i];
            }
        }
        return null;
    }

    public override void OnEnter()
    {
        base.OnEnter();
        instancesList.Add(this);
        networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
        if ((bool)networkedBodyAttachment)
        {
            bodyGameObject = networkedBodyAttachment.attachedBodyObject;
            body = networkedBodyAttachment.attachedBody;
            if ((bool)bodyGameObject)
            {
                bodyMotor = bodyGameObject.GetComponent<CharacterMotor>();
            }
        }
    }
    public override void OnExit()
    {
        instancesList.Remove(this);
        base.OnExit();
    }
}
