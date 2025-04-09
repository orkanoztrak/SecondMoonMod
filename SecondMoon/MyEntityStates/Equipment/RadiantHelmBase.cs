using EntityStates;
using RoR2;
using SecondMoon.MyEntityStates.Items.Tier2;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace SecondMoon.MyEntityStates.Equipment;

public class RadiantHelmBase : BaseState
{
    private static readonly List<RadiantHelmBase> instancesList = new List<RadiantHelmBase>();

    protected NetworkedBodyAttachment networkedBodyAttachment;

    protected GameObject bodyGameObject;

    protected CharacterBody body;

    protected CharacterMotor bodyMotor;

    protected InputBankTest bodyInputBank;

    protected CharacterDirection bodyCharacterDirection;

    protected Transform bodyModelTransform;

    public static RadiantHelmBase FindForBody(CharacterBody body)
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
            bodyModelTransform = body.modelLocator?.modelTransform;
            if ((bool)bodyGameObject)
            {
                bodyMotor = bodyGameObject.GetComponent<CharacterMotor>();
                bodyInputBank = bodyGameObject.GetComponent<InputBankTest>();
                bodyCharacterDirection = bodyGameObject.GetComponent<CharacterDirection>();
            }
        }
    }

    public override void OnExit()
    {
        instancesList.Remove(this);
        base.OnExit();
    }
}
