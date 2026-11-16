using System.Collections;
using UnityEngine;
using CameraShake;
using UnityEngine.Serialization;

public class HurtBoxCollider : MonoBehaviour
{
    [Header("Hit Reaction")]
    [SerializeField] private string hitTriggerName = "Hit";
    [SerializeField] private string endDizzyBoolName = "EndDizzy";
    [SerializeField] private string getHitSoundName = "GetHit";
    [SerializeField] private Transform getHitTransform;
    [FormerlySerializedAs("getHitVfxParent")]
    [SerializeField, HideInInspector] private GameObject legacyGetHitVfxParent;

    [Header("Death Flight")]
    [SerializeField] private string deathTriggerName = "Knockoff";
    [SerializeField] private string alternateDeathTriggerName = "KnockOffScreen";
    [SerializeField] private Transform deathFlightTarget;
    [SerializeField] private float deathFlightDuration = 0.35f;
    [SerializeField] private float deathFlightSpeed = 18f;
    [SerializeField] private float deathFlightHorizontalDistance = 8f;
    [SerializeField] private float deathFlightVerticalOffset = 0f;
    [SerializeField] private float deathFlightDepthOffset = 0f;

    private StatsController targetStats;
    private Animator targetAnimator;
    private Rigidbody targetBody3D;
    private Rigidbody2D targetBody2D;
    private SoundController soundController;
    private PlayerCameraController playerCameraController;
    private FlashController hitFlashController;
    private Coroutine dizzyRoutine;
    private Coroutine deathFlightRoutine;
    private bool controlsLocked;
    private bool isDeathSequenceActive;
    private bool cachedTargetBody3DIsKinematic;
    private bool cachedTargetBody2DIsKinematic;
    private bool changedTargetBody3DKinematic;
    private bool changedTargetBody2DKinematic;
    private bool ownsRuntimeDeathFlightTarget;
    private string activeDeathAnimationStateName;
    private float deathFlightStartedAt = -1f;

    protected StatsController TargetStats => targetStats;
    protected Animator TargetAnimator => targetAnimator;
    protected Rigidbody TargetBody3D => targetBody3D;
    protected Rigidbody2D TargetBody2D => targetBody2D;
    protected SoundController SoundController => soundController;

    private void Awake()
    {
        CacheReferences();
    }

    private void OnDisable()
    {
        if (dizzyRoutine != null)
        {
            StopCoroutine(dizzyRoutine);
            dizzyRoutine = null;
        }

        if (deathFlightRoutine != null)
        {
            StopCoroutine(deathFlightRoutine);
            deathFlightRoutine = null;
        }

        isDeathSequenceActive = false;
        RestoreDeathFlightPhysicsState();
        OnSpecificDisable();
        UnlockControls();
        SetEndDizzyBool(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        HitBoxController hitBox = other.GetComponent<HitBoxController>();
        if (hitBox == null)
            hitBox = other.GetComponentInParent<HitBoxController>();

        if (hitBox != null)
            ReceiveHitBox(hitBox);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HitBoxController hitBox = other.GetComponent<HitBoxController>();
        if (hitBox == null)
            hitBox = other.GetComponentInParent<HitBoxController>();

        if (hitBox != null)
            ReceiveHitBox(hitBox);
    }

    public void ReceiveHitBox(HitBoxController hitBox)
    {
        if (hitBox == null)
            return;

        if (targetStats == null)
            CacheReferences();

        if (!CanReceiveHitBox(hitBox))
            return;

        if (targetStats != null && !hitBox.TryRegisterTarget(targetStats))
            return;

        ApplyHit(hitBox);
    }

    public void ReceiveFatalEnvironmentHit(float horizontalDirection = 1f, bool moveCameraToPointZoom = true)
    {
        if (targetStats == null)
            CacheReferences();

        if (isDeathSequenceActive || targetStats == null)
            return;

        if (targetStats.CurrentHealth > 0)
            targetStats.TakeDamage(targetStats.CurrentHealth);

        if (!targetStats.IsDead)
            return;

        if (hitFlashController != null)
            hitFlashController.PlayHitFlash();

        PlayGetHitSound();
        SpawnKnockbackVfx(null);
        StartDeathFlight(null, horizontalDirection, moveCameraToPointZoom);
    }

    public void SpawnGetHitVfx(GameObject getHitVfxPrefab)
    {
        if (getHitVfxPrefab == null)
            return;

        Transform spawnPoint = ResolveGetHitTransform();
        Instantiate(getHitVfxPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
    }

    protected void SpawnAttachedVfx(GameObject vfxPrefab, Transform parentTransform)
    {
        if (vfxPrefab == null)
            return;

        Transform spawnPoint = parentTransform != null ? parentTransform : transform;
        Instantiate(vfxPrefab, spawnPoint.position, spawnPoint.rotation, spawnPoint);
    }

    private void CacheReferences()
    {
        targetStats = GetComponentInParent<StatsController>();
        targetAnimator = GetComponentInParent<Animator>();
        targetBody3D = GetComponentInParent<Rigidbody>();
        targetBody2D = GetComponentInParent<Rigidbody2D>();
        soundController = GetComponentInParent<SoundController>();
        playerCameraController = FindFirstObjectByType<PlayerCameraController>();
        hitFlashController = GetComponent<FlashController>();
        if (hitFlashController == null)
            hitFlashController = GetComponentInParent<FlashController>();
        if (hitFlashController == null && targetStats != null)
            hitFlashController = targetStats.GetComponentInChildren<FlashController>(true);
        CacheSpecificReferences();

        if (getHitTransform == null && legacyGetHitVfxParent != null)
            getHitTransform = legacyGetHitVfxParent.transform;
    }

    protected virtual void CacheSpecificReferences()
    {
    }

    private Transform ResolveGetHitTransform()
    {
        if (getHitTransform != null)
            return getHitTransform;

        if (legacyGetHitVfxParent != null)
            return legacyGetHitVfxParent.transform;

        return transform;
    }

    private void ApplyHit(HitBoxController hitBox)
    {
        int dealtDamage = 0;

        if (targetStats != null)
        {
            if (hitBox.Damage != 0)
            {
                int healthBeforeHit = targetStats.CurrentHealth;
                targetStats.TakeDamage(hitBox.Damage);
                dealtDamage = Mathf.Max(0, healthBeforeHit - targetStats.CurrentHealth);
            }

            if (hitBox.RippDamage != 0)
                targetStats.ChangeRipp(-hitBox.RippDamage);
        }

        hitBox.RewardOwner(targetStats, dealtDamage);
        if (hitFlashController != null)
            hitFlashController.PlayHitFlash();
        PlayGetHitSound();
        TriggerCameraShake(hitBox);

        if (targetStats != null && targetStats.IsDead)
        {
            SpawnKnockbackVfx(hitBox);
            StartDeathFlight(hitBox);
            return;
        }

        StartDizzyReaction(hitBox.DizzyDuration);
        ApplyKnockback(hitBox);
        SpawnKnockbackVfx(hitBox);
    }

    private void StartDeathFlight(
        HitBoxController hitBox,
        float forcedHorizontalDirection = 0f,
        bool moveCameraToPointZoom = true)
    {
        if (isDeathSequenceActive)
            return;

        isDeathSequenceActive = true;

        if (dizzyRoutine != null)
        {
            StopCoroutine(dizzyRoutine);
            dizzyRoutine = null;
        }

        LockControls();
        ResetLocomotionAnimatorState();
        SetEndDizzyBool(false);
        ResetHitTrigger();
        deathFlightStartedAt = Time.time;
        activeDeathAnimationStateName = TrySetDeathTrigger();
        if (moveCameraToPointZoom && playerCameraController != null)
            playerCameraController.MoveToPointZoomCamera();
        PrepareForDeathFlight();

        if (deathFlightRoutine != null)
            StopCoroutine(deathFlightRoutine);

        deathFlightRoutine = StartCoroutine(DeathFlightRoutine(hitBox, forcedHorizontalDirection));
    }

    private IEnumerator DeathFlightRoutine(HitBoxController hitBox, float forcedHorizontalDirection)
    {
        Transform flightTarget = ResolveDeathFlightTarget(hitBox, forcedHorizontalDirection);
        Vector3 startPosition = targetStats != null ? targetStats.transform.position : transform.position;
        Vector3 endPosition = flightTarget != null ? flightTarget.position : startPosition;
        endPosition.z = startPosition.z + deathFlightDepthOffset;
        Vector3 currentPosition = startPosition;

        float duration = ResolveDeathFlightDuration();

        while (true)
        {
            float elapsed = Mathf.Max(0f, Time.time - deathFlightStartedAt);
            float maxStep = Mathf.Max(0.01f, deathFlightSpeed) * Time.deltaTime;
            currentPosition = Vector3.MoveTowards(currentPosition, endPosition, maxStep);
            MoveTargetTo(currentPosition);

            if (elapsed >= duration || IsDeathAnimationFinished())
                break;

            yield return null;
        }

        MoveTargetTo(endPosition);
        FinishDeathFlight();
    }

    private void PrepareForDeathFlight()
    {
        if (targetBody3D != null)
        {
            cachedTargetBody3DIsKinematic = targetBody3D.isKinematic;
            if (!targetBody3D.isKinematic)
            {
                targetBody3D.linearVelocity = Vector3.zero;
                targetBody3D.angularVelocity = Vector3.zero;
                targetBody3D.isKinematic = true;
                changedTargetBody3DKinematic = true;
            }
        }

        if (targetBody2D != null)
        {
            cachedTargetBody2DIsKinematic = targetBody2D.isKinematic;
            if (!targetBody2D.isKinematic)
            {
                targetBody2D.linearVelocity = Vector2.zero;
                targetBody2D.angularVelocity = 0f;
                targetBody2D.isKinematic = true;
                changedTargetBody2DKinematic = true;
            }
        }

        OnPrepareDeathFlight();
    }

    protected virtual void OnPrepareDeathFlight()
    {
    }

    private void FinishDeathFlight()
    {
        deathFlightRoutine = null;
        RestoreDeathFlightPhysicsState();

        if (targetStats != null)
            targetStats.RestoreFullStats();

        OnRespawnAfterDeathFlight();

        if (playerCameraController != null)
            playerCameraController.RestoreTrackingCamera();

        isDeathSequenceActive = false;
        activeDeathAnimationStateName = null;
        deathFlightStartedAt = -1f;
        UnlockControls();
        SetEndDizzyBool(false);
    }

    private void RestoreDeathFlightPhysicsState()
    {
        if (targetBody3D != null && changedTargetBody3DKinematic)
            targetBody3D.isKinematic = cachedTargetBody3DIsKinematic;

        if (targetBody2D != null && changedTargetBody2DKinematic)
            targetBody2D.isKinematic = cachedTargetBody2DIsKinematic;

        changedTargetBody3DKinematic = false;
        changedTargetBody2DKinematic = false;
    }

    protected virtual void OnRespawnAfterDeathFlight()
    {
        if (targetStats != null)
            targetStats.RespawnAtSpawn();
    }

    private Transform ResolveDeathFlightTarget(HitBoxController hitBox, float forcedHorizontalDirection)
    {
        if (deathFlightTarget == null)
        {
            GameObject runtimeTarget = new GameObject($"{gameObject.name}_DeathFlightTarget");
            deathFlightTarget = runtimeTarget.transform;
            ownsRuntimeDeathFlightTarget = true;
        }

        if (ownsRuntimeDeathFlightTarget)
            deathFlightTarget.position = ResolveDeathFlightTargetPosition(hitBox, forcedHorizontalDirection);

        if (deathFlightTarget != null)
            return deathFlightTarget;

        return null;
    }

    private Vector3 ResolveDeathFlightTargetPosition(HitBoxController hitBox, float forcedHorizontalDirection)
    {
        Vector3 origin = targetStats != null ? targetStats.transform.position : transform.position;
        float horizontalDirection = ResolveDeathFlightHorizontalDirection(hitBox, forcedHorizontalDirection);
        if (Mathf.Approximately(horizontalDirection, 0f))
            horizontalDirection = 1f;

        origin.x += horizontalDirection * Mathf.Max(0.5f, deathFlightHorizontalDistance);
        origin.y += deathFlightVerticalOffset;
        origin.z += deathFlightDepthOffset;
        return origin;
    }

    private float ResolveDeathFlightHorizontalDirection(HitBoxController hitBox, float forcedHorizontalDirection)
    {
        if (Mathf.Abs(forcedHorizontalDirection) > 0.001f)
            return Mathf.Sign(forcedHorizontalDirection);

        if (hitBox == null)
            return 1f;

        Vector3 knockbackDirection = ResolveKnockbackDirection(hitBox);
        if (Mathf.Abs(knockbackDirection.x) > 0.001f)
            return Mathf.Sign(knockbackDirection.x);

        return ResolveFallbackHorizontalDirection(hitBox);
    }

    private void MoveTargetTo(Vector3 position)
    {
        position.z = 0f;

        if (targetBody3D != null)
        {
            targetBody3D.position = position;
            return;
        }

        if (targetBody2D != null)
        {
            targetBody2D.position = position;
            return;
        }

        if (targetStats != null)
        {
            targetStats.transform.position = position;
            return;
        }

        transform.position = position;
    }

    private void StartDizzyReaction(float dizzyDuration)
    {
        if (dizzyRoutine != null)
            StopCoroutine(dizzyRoutine);

        LockControls();
        ResetLocomotionAnimatorState();
        SetEndDizzyBool(false);
        ResetHitTrigger();
        SetHitTrigger();

        dizzyRoutine = StartCoroutine(DizzyRoutine(Mathf.Max(0f, dizzyDuration)));
    }

    private IEnumerator DizzyRoutine(float dizzyDuration)
    {
        if (dizzyDuration > 0f)
            yield return new WaitForSeconds(dizzyDuration);
        else
            yield return null;

        SetEndDizzyBool(true);
        yield return null;

        UnlockControls();
        dizzyRoutine = null;
    }

    private void LockControls()
    {
        if (controlsLocked)
            return;

        controlsLocked = true;
        OnLockControls();

        if (targetBody3D != null && !targetBody3D.isKinematic)
        {
            Vector3 velocity = targetBody3D.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            targetBody3D.linearVelocity = velocity;
        }

        if (targetBody2D != null && !targetBody2D.isKinematic)
        {
            Vector2 velocity = targetBody2D.linearVelocity;
            velocity.x = 0f;
            targetBody2D.linearVelocity = velocity;
        }
    }

    protected virtual void OnLockControls()
    {
    }

    private void UnlockControls()
    {
        if (!controlsLocked)
            return;

        controlsLocked = false;
        OnUnlockControls();
    }

    protected virtual void OnUnlockControls()
    {
    }

    protected virtual void OnSpecificDisable()
    {
    }

    private void PlayGetHitSound()
    {
        if (soundController != null && !string.IsNullOrWhiteSpace(getHitSoundName))
            soundController.PlaySound(getHitSoundName);
    }

    private void TriggerCameraShake(HitBoxController hitBox)
    {
        if (hitBox == null || CameraShaker.Presets == null)
            return;

        bool isHeavy = hitBox.AttackType == PlayerInputController.AttackType.Heavy;
        float minimumStrength = isHeavy ? 0.58f : 0.24f;
        float minimumDuration = isHeavy ? 0.18f : 0.13f;
        float minimumFrequency = isHeavy ? 95f : 70f;

        if (hitBox.UseCustomShake)
        {
            float customStrength = Mathf.Max(
                hitBox.ShakePositionStrength * 1.25f,
                hitBox.ShakeRotationStrength * 0.7f);
            float customDuration = Mathf.Max(
                hitBox.ShakeBounces / Mathf.Max(hitBox.ShakeFrequency, 1f),
                0.08f);

            CameraShaker.Presets.LegacyRandom2D(
                Mathf.Max(customStrength, minimumStrength),
                Mathf.Max(customDuration, minimumDuration),
                Mathf.Max(hitBox.ShakeFrequency, minimumFrequency));
            return;
        }

        if (hitBox.DizzyDuration > 0f)
        {
            minimumStrength = Mathf.Max(minimumStrength, 0.62f);
            minimumDuration = Mathf.Max(minimumDuration, 0.2f);
            minimumFrequency = Mathf.Max(minimumFrequency, 95f);
        }

        CameraShaker.Presets.LegacyRandom2D(
            minimumStrength,
            minimumDuration,
            minimumFrequency);
    }

    private void ApplyKnockback(HitBoxController hitBox)
    {
        if (hitBox == null || hitBox.KnockbackForce <= 0f)
            return;

        Vector3 knockbackDirection = ResolveKnockbackDirection(hitBox);
        if (knockbackDirection.sqrMagnitude <= 0.0001f)
            return;

        Vector3 knockbackVelocity = knockbackDirection * hitBox.KnockbackForce;

        if (ApplySpecificKnockback(knockbackVelocity, hitBox.KnockbackDuration))
            return;

        if (targetBody3D != null && !targetBody3D.isKinematic)
        {
            Vector3 velocity = targetBody3D.linearVelocity;
            velocity.x = knockbackVelocity.x;
            velocity.y = knockbackVelocity.y;
            velocity.z = 0f;
            targetBody3D.linearVelocity = velocity;
            return;
        }

        if (targetBody2D != null && !targetBody2D.isKinematic)
        {
            Vector2 velocity = targetBody2D.linearVelocity;
            velocity.x = knockbackVelocity.x;
            velocity.y = knockbackVelocity.y;
            targetBody2D.linearVelocity = velocity;
        }
    }

    protected virtual bool ApplySpecificKnockback(Vector3 knockbackVelocity, float knockbackDuration)
    {
        return false;
    }

    private Vector3 ResolveKnockbackDirection(HitBoxController hitBox)
    {
        Vector2 localDirection = hitBox.KnockbackDirection;
        if (localDirection.sqrMagnitude <= 0.0001f)
            localDirection = ResolveFallbackKnockbackDirection(hitBox);
        else
            localDirection.x *= ResolveOwnerFacingSign(hitBox);

        if (Mathf.Abs(localDirection.x) <= 0.0001f)
            localDirection.x = ResolveFallbackHorizontalDirection(hitBox);

        if (localDirection.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        return new Vector3(localDirection.x, localDirection.y, 0f).normalized;
    }

    private Vector2 ResolveFallbackKnockbackDirection(HitBoxController hitBox)
    {
        return new Vector2(ResolveFallbackHorizontalDirection(hitBox), 0f);
    }

    private float ResolveFallbackHorizontalDirection(HitBoxController hitBox)
    {
        if (targetStats != null && hitBox != null && hitBox.OwnerStats != null)
        {
            float horizontalDelta = targetStats.transform.position.x - hitBox.OwnerStats.transform.position.x;
            if (Mathf.Abs(horizontalDelta) > 0.001f)
                return Mathf.Sign(horizontalDelta);
        }

        return ResolveOwnerFacingSign(hitBox);
    }

    private float ResolveOwnerFacingSign(HitBoxController hitBox)
    {
        Transform ownerTransform = hitBox != null && hitBox.OwnerStats != null
            ? hitBox.OwnerStats.transform
            : hitBox != null ? hitBox.transform : null;

        if (ownerTransform == null)
            return 1f;

        float sign = Mathf.Sign(ownerTransform.lossyScale.x);
        return Mathf.Approximately(sign, 0f) ? 1f : sign;
    }

    private void ResetLocomotionAnimatorState()
    {
        SetBoolIfExists("IsIdle", false);
        SetBoolIfExists("IsRunning", false);
        SetBoolIfExists("IsJumping", false);
        SetBoolIfExists("MidAirFall", false);
    }

    private void SetHitTrigger()
    {
        SetTriggerIfExists(hitTriggerName);
    }

    private void ResetHitTrigger()
    {
        if (HasParameter(hitTriggerName, AnimatorControllerParameterType.Trigger))
            targetAnimator.ResetTrigger(hitTriggerName);
    }

    private string TrySetDeathTrigger()
    {
        if (SetTriggerIfExists(deathTriggerName))
            return deathTriggerName;

        if (SetTriggerIfExists(alternateDeathTriggerName))
            return alternateDeathTriggerName;

        return string.Empty;
    }

    private float ResolveDeathFlightDuration()
    {
        return Mathf.Max(0.01f, deathFlightDuration);
    }

    private bool IsDeathAnimationFinished()
    {
        if (string.IsNullOrWhiteSpace(activeDeathAnimationStateName))
            return true;

        return IsSpecificDeathAnimationFinished(activeDeathAnimationStateName);
    }

    protected virtual bool IsSpecificDeathAnimationFinished(string stateName)
    {
        return true;
    }

    private bool SetTriggerIfExists(string parameterName)
    {
        if (!HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
            return false;

        targetAnimator.ResetTrigger(parameterName);
        targetAnimator.SetTrigger(parameterName);
        return true;
    }

    private void SetEndDizzyBool(bool value)
    {
        if (HasParameter(endDizzyBoolName, AnimatorControllerParameterType.Bool))
            targetAnimator.SetBool(endDizzyBoolName, value);
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            targetAnimator.SetBool(parameterName, value);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (targetAnimator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        for (int i = 0; i < targetAnimator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = targetAnimator.parameters[i];
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }

    protected virtual bool CanReceiveHitBox(HitBoxController hitBox)
    {
        if (hitBox == null)
            return false;

        if (isDeathSequenceActive || (targetStats != null && targetStats.IsDead))
            return false;

        StatsController ownerStats = hitBox.OwnerStats;
        if (ownerStats == null || targetStats == null)
            return ownerStats != targetStats;

        return ownerStats != targetStats;
    }

    protected virtual void SpawnKnockbackVfx(HitBoxController hitBox)
    {
    }

    protected bool IsPlayerStats(StatsController stats)
    {
        return stats != null && stats.GetComponent<PlayerController1>() != null;
    }

    protected bool IsEnemyStats(StatsController stats)
    {
        return stats != null && stats.GetComponent<EnemyMovement>() != null;
    }
}
