using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SecondMoon.Equipment.RadiantHelm.RadiantHelm;

namespace SecondMoon.MyEntityStates.Equipment;

public class RadiantHelmBlink : RadiantHelmBase
{
    public static GameObject blinkPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressBlinkEffect.prefab").WaitForCompletion();

    private float stopwatch;

    private float unchangedSpeed;

    private Vector3 blinkVector = Vector3.zero;

    [SerializeField]
    public float duration = 0.2f;

    [SerializeField]
    public float speedCoefficient;

    [SerializeField]
    public string beginSoundString = "Play_huntress_shift_start";

    [SerializeField]
    public string endSoundString = "Play_huntress_shift_end";

    private CharacterModel characterModel;

    private HurtBoxGroup hurtboxGroup;

    public override void OnEnter()
    {
        base.OnEnter();
        if (body.notMovingStopwatch <= 0f)
        {
            body.isSprinting = true;
        }
        Util.PlaySound(beginSoundString, gameObject);
        if (bodyModelTransform)
        {
            characterModel = bodyModelTransform.GetComponent<CharacterModel>();
            hurtboxGroup = bodyModelTransform.GetComponent<HurtBoxGroup>();
        }
        if (characterModel)
        {
            characterModel.invisibilityCount++;
        }
        if (hurtboxGroup)
        {
            HurtBoxGroup hurtBoxGroup = hurtboxGroup;
            int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
            hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
        }
        blinkVector = bodyInputBank.aimDirection;
        speedCoefficient = RadiantHelmBaseBlinkDistance / (7 * duration);
        CreateBlinkEffect(Util.GetCorePosition(bodyGameObject));
    }

    private void CreateBlinkEffect(Vector3 origin)
    {
        if (blinkVector != Vector3.zero)
        {
            EffectData effectData = new EffectData();
            effectData.rotation = Util.QuaternionSafeLookRotation(blinkVector);
            effectData.origin = origin;
            EffectManager.SpawnEffect(blinkPrefab, effectData, transmit: false);
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        stopwatch += Time.fixedDeltaTime;
        if (bodyMotor && bodyCharacterDirection)
        {
            bodyMotor.velocity = Vector3.zero;
            bodyMotor.rootMotion += blinkVector * (body.moveSpeed * speedCoefficient * Time.fixedDeltaTime);
        }
        if (stopwatch >= duration && isAuthority)
        {
            outer.SetNextStateToMain();
        }
    }

    public override void OnExit()
    {
        if (!outer.destroying)
        {
            Util.PlaySound(endSoundString, gameObject);
            CreateBlinkEffect(Util.GetCorePosition(gameObject));
            if (bodyModelTransform)
            {
                TemporaryOverlayInstance temporaryOverlayInstance = TemporaryOverlayManager.AddOverlay(bodyModelTransform.gameObject);
                temporaryOverlayInstance.duration = 0.6f;
                temporaryOverlayInstance.animateShaderAlpha = true;
                temporaryOverlayInstance.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlayInstance.destroyComponentOnEnd = true;
                temporaryOverlayInstance.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashBright");
                temporaryOverlayInstance.AddToCharacterModel(bodyModelTransform.GetComponent<CharacterModel>());
                TemporaryOverlayInstance temporaryOverlayInstance2 = TemporaryOverlayManager.AddOverlay(bodyModelTransform.gameObject);
                temporaryOverlayInstance2.duration = 0.7f;
                temporaryOverlayInstance2.animateShaderAlpha = true;
                temporaryOverlayInstance2.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                temporaryOverlayInstance2.destroyComponentOnEnd = true;
                temporaryOverlayInstance2.originalMaterial = LegacyResourcesAPI.Load<Material>("Materials/matHuntressFlashExpanded");
                temporaryOverlayInstance2.AddToCharacterModel(bodyModelTransform.GetComponent<CharacterModel>());
            }
        }
        if (characterModel)
        {
            characterModel.invisibilityCount--;
        }
        if (hurtboxGroup)
        {
            HurtBoxGroup hurtBoxGroup = hurtboxGroup;
            int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
            hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
        }
        if (bodyMotor)
        {
            bodyMotor.disableAirControlUntilCollision = false;
        }
        base.OnExit();
    }

}
