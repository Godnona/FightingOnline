using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public sealed class CinemachineSwitcher : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("The normal gameplay camera in this scene.")]
    [SerializeField] private CinemachineCamera defaultCamera;

    [Tooltip("The camera used after the player lands a jump on a high platform.")]
    [SerializeField] private CinemachineCamera jumpCamera;

    [Tooltip("The PlayerMovement instance from this scene. No reference is stored on the player prefab.")]
    [SerializeField] private PlayerMovement playerMovement;

    [Header("Jump Landing")]
    [Tooltip("The player must land at or above this world-space Y coordinate.")]
    [SerializeField] private float minimumLandingY = 28f;

    [Header("Priority")]
    [SerializeField] private int activePriority = 20;
    [SerializeField] private int inactivePriority = 0;
    [SerializeField] private bool resetToDefaultOnEnable = true;

    [Tooltip("After a non-jump event ends, use Jump Camera when the player is still above Minimum Landing Y.")]
    [SerializeField] private bool restoreJumpCameraAfterEvent = true;

    [Header("Jump Camera Position Follow")]
    [Tooltip("Keep the player center locked to the middle of the image without follow lag.")]
    [SerializeField] private bool keepPlayerExactlyCentered = true;

    [Tooltip("While Jump Camera is active, follow the player horizontally.")]
    [SerializeField] private bool followPlayerHorizontally = true;

    [Tooltip("Additional world-space X framing offset.")]
    [SerializeField] private float horizontalOffset;

    [Tooltip("0 follows immediately. Higher values add horizontal damping.")]
    [SerializeField, Min(0f)] private float horizontalFollowSmoothTime = 0.12f;

    [Tooltip("Move to the player's X immediately when Jump Camera becomes active.")]
    [SerializeField] private bool snapHorizontallyOnActivation = true;

    [Tooltip("While Jump Camera is active, follow the player vertically.")]
    [SerializeField] private bool followPlayerVertically = true;

    [Tooltip("Additional world-space Y framing offset.")]
    [SerializeField] private float verticalOffset;

    [Tooltip("Fine-tune the point on the player's body that appears at screen center.")]
    [SerializeField] private Vector3 playerCenterOffset;

    [Tooltip("0 follows immediately. Higher values add vertical damping.")]
    [SerializeField, Min(0f)] private float verticalFollowSmoothTime = 0.12f;

    [Tooltip("Move to the player's Y immediately when Jump Camera becomes active.")]
    [SerializeField] private bool snapVerticallyOnActivation = true;

    [Header("Optional Event Detection")]
    [Tooltip("These references are resolved from Player Movement automatically when left empty.")]
    [SerializeField] private PlayerInputController playerInput;
    [SerializeField] private PlayerFightingController playerFighting;
    [SerializeField] private PlayerCameraController playerCameraController;
    [SerializeField] private StatsController playerStats;

    [Header("Runtime Debug")]
    [SerializeField] private bool isUsingJumpCamera;
    [SerializeField] private bool jumpInProgress;
    [SerializeField] private bool isInterruptingEvent;

    private bool wasGrounded;
    private bool wasStandingOnPlatform;
    private bool groundStateInitialized;
    private bool hasCachedJumpPose;
    private float horizontalFollowVelocity;
    private float verticalFollowVelocity;
    private Quaternion jumpCameraFixedRotation;
    private Vector3 jumpCameraCenteredOffset;
    private Collider playerBodyCollider;

    public bool IsUsingJumpCamera => isUsingJumpCamera;

    private void Awake()
    {
        ResolvePlayerReferences();
        InitializeGroundState();
        CacheJumpCameraFixedPose();
        WarnIfCamerasAreManagedByAnotherCamera();
    }

    private void OnEnable()
    {
        ResolvePlayerReferences();
        InitializeGroundState();
        CacheJumpCameraFixedPose();

        if (resetToDefaultOnEnable)
            SwitchToDefault();
    }

    private void LateUpdate()
    {
        ResolvePlayerReferences();
        if (playerMovement == null)
            return;

        bool isGrounded = playerMovement.IsGrounded;
        if (!groundStateInitialized)
        {
            wasGrounded = isGrounded;
            wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
            groundStateInitialized = true;
        }

        bool hasNonJumpEvent = HasNonJumpEvent();
        if (hasNonJumpEvent)
        {
            isInterruptingEvent = true;
            jumpInProgress = false;
            SwitchToDefault();
            wasGrounded = isGrounded;
            wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
            return;
        }

        if (isInterruptingEvent)
        {
            isInterruptingEvent = false;
            bool shouldRestore =
                restoreJumpCameraAfterEvent &&
                playerMovement.transform.position.y >= minimumLandingY;

            if (shouldRestore)
            {
                SwitchToJump();
                jumpInProgress = !isGrounded;
                wasGrounded = isGrounded;
                wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
                return;
            }
        }

        // Once Jump mode has been entered, grounding transitions between the
        // upper platforms must not switch the camera back to Default.  Height is
        // the only positional exit condition; gameplay events above still have
        // priority and may force Default at any height.
        if (isUsingJumpCamera)
        {
            if (playerMovement.transform.position.y < minimumLandingY)
            {
                jumpInProgress = false;
                SwitchToDefault();
            }
            else
            {
                jumpInProgress = !isGrounded;
                UpdateJumpCameraPositionFollow(false, false);
                wasGrounded = isGrounded;
                wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
                return;
            }
        }

        if (wasGrounded && !isGrounded)
        {
            // Moving upward starts a normal jump.  When the high-platform camera
            // is already active, leaving a platform downward is also a valid
            // platform-to-platform traversal and must keep the Jump camera live.
            jumpInProgress =
                playerMovement.IsMovingUpward ||
                (isUsingJumpCamera && wasStandingOnPlatform);
            if (!jumpInProgress)
                SwitchToDefault();
        }
        else if (!wasGrounded && isGrounded)
        {
            bool landedOnPlatformAfterJump =
                jumpInProgress && playerMovement.IsStandingOnPlatform;

            bool canEnterJumpCamera =
                playerMovement.transform.position.y >= minimumLandingY;

            // Default can enter Jump mode only after a qualifying landing at or
            // above the configured height.
            bool isValidJumpLanding =
                landedOnPlatformAfterJump && canEnterJumpCamera;

            jumpInProgress = false;
            if (isValidJumpLanding)
                SwitchToJump();
            else
                SwitchToDefault();
        }

        UpdateJumpCameraPositionFollow(false, false);
        wasGrounded = isGrounded;
        wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
    }

    [ContextMenu("Switch To Default")]
    public void SwitchToDefault()
    {
        SetPriorities(defaultCamera, jumpCamera);
        isUsingJumpCamera = false;
        horizontalFollowVelocity = 0f;
        verticalFollowVelocity = 0f;
    }

    [ContextMenu("Switch To Jump")]
    public void SwitchToJump()
    {
        CacheJumpCameraFixedPose();
        SetPriorities(jumpCamera, defaultCamera);
        isUsingJumpCamera = true;
        horizontalFollowVelocity = 0f;
        verticalFollowVelocity = 0f;
        UpdateJumpCameraPositionFollow(
            snapHorizontallyOnActivation,
            snapVerticallyOnActivation);
    }

    /// <summary>
    /// Call this from a Timeline Signal, UnityEvent, or Animation Event for any
    /// scene event that is not covered by the automatic checks below.
    /// </summary>
    public void NotifyNonJumpEvent()
    {
        jumpInProgress = false;
        SwitchToDefault();
    }

    private bool HasNonJumpEvent()
    {
        if (playerMovement.IsUnderKnockback ||
            playerMovement.IsRespawnPending)
            return true;

        if (playerFighting != null && playerFighting.IsAttacking)
            return true;

        // Reset on the input frame too, before an attack animation/timeline begins.
        if (playerInput != null &&
            playerInput.BufferedAttack != PlayerInputController.AttackType.None)
            return true;

        if (playerStats != null && playerStats.IsDead)
            return true;

        // Train, point zoom, end game, and knock-off sequences use a locked
        // custom target on the normal gameplay camera.
        return playerCameraController != null &&
               playerCameraController.IsFollowingCustomTarget;
    }

    private void SetPriorities(CinemachineCamera activeCamera, CinemachineCamera inactiveCamera)
    {
        if (activeCamera != null)
            activeCamera.Priority = activePriority;

        if (inactiveCamera != null)
            inactiveCamera.Priority = inactivePriority;
    }

    private void CacheJumpCameraFixedPose()
    {
        // Cache the authored shot only once. Recalculating this after the camera
        // has already followed the player would make its distance shrink a little
        // on every Default -> Jump transition.
        if (hasCachedJumpPose)
            return;

        if (jumpCamera == null)
        {
            return;
        }

        Transform jumpTransform = jumpCamera.transform;
        jumpCameraFixedRotation = jumpTransform.rotation;

        Vector3 playerCenter = GetPlayerCenterPosition();
        Vector3 cameraForward = jumpCameraFixedRotation * Vector3.forward;
        Vector3 cameraToPlayer = playerCenter - jumpTransform.position;
        float distanceAlongView = Vector3.Dot(cameraToPlayer, cameraForward);
        float cameraDistance = distanceAlongView > 0.01f
            ? distanceAlongView
            : Mathf.Max(0.01f, cameraToPlayer.magnitude);

        // Keeping this offset parallel to camera-forward guarantees that the
        // selected point on the player is exactly at the center of the image
        // while preserving the authored viewing angle and distance.
        jumpCameraCenteredOffset = -cameraForward.normalized * cameraDistance;
        hasCachedJumpPose = true;
    }

    private void UpdateJumpCameraPositionFollow(bool snapX, bool snapY)
    {
        if (!isUsingJumpCamera || jumpCamera == null || playerMovement == null)
            return;

        if (!hasCachedJumpPose)
            CacheJumpCameraFixedPose();

        Transform jumpTransform = jumpCamera.transform;
        Vector3 position = jumpTransform.position;
        Vector3 followPosition = GetPlayerCenterPosition();
        Vector3 targetPosition = followPosition + jumpCameraCenteredOffset;

        if (followPlayerHorizontally)
        {
            float targetX = targetPosition.x + horizontalOffset;
            if (keepPlayerExactlyCentered || snapX || horizontalFollowSmoothTime <= 0f)
            {
                position.x = targetX;
                horizontalFollowVelocity = 0f;
            }
            else
            {
                position.x = Mathf.SmoothDamp(
                    position.x,
                    targetX,
                    ref horizontalFollowVelocity,
                    horizontalFollowSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime);
            }
        }

        if (followPlayerVertically)
        {
            float targetY = targetPosition.y + verticalOffset;
            if (keepPlayerExactlyCentered || snapY || verticalFollowSmoothTime <= 0f)
            {
                position.y = targetY;
                verticalFollowVelocity = 0f;
            }
            else
            {
                position.y = Mathf.SmoothDamp(
                    position.y,
                    targetY,
                    ref verticalFollowVelocity,
                    verticalFollowSmoothTime,
                    Mathf.Infinity,
                    Time.deltaTime);
            }
        }

        // Depth follows the centered view geometry; the authored angle remains fixed.
        position.z = targetPosition.z;
        jumpTransform.SetPositionAndRotation(position, jumpCameraFixedRotation);
    }

    private Vector3 GetPlayerCenterPosition()
    {
        if (playerMovement == null)
            return Vector3.zero;

        if (playerBodyCollider == null)
            playerBodyCollider = playerMovement.GetComponent<Collider>();

        Vector3 center = playerBodyCollider != null
            ? playerBodyCollider.bounds.center
            : playerMovement.transform.position;

        return center + playerCenterOffset;
    }

    private void InitializeGroundState()
    {
        if (playerMovement == null)
        {
            groundStateInitialized = false;
            return;
        }

        wasGrounded = playerMovement.IsGrounded;
        wasStandingOnPlatform = playerMovement.IsStandingOnPlatform;
        groundStateInitialized = true;
        jumpInProgress = false;
        isInterruptingEvent = false;
    }

    private void ResolvePlayerReferences()
    {
        if (playerMovement == null)
            return;

        if (playerInput == null)
            playerInput = playerMovement.GetComponent<PlayerInputController>();

        if (playerFighting == null)
            playerFighting = playerMovement.GetComponent<PlayerFightingController>();

        if (playerCameraController == null)
            playerCameraController = playerMovement.GetComponent<PlayerCameraController>();

        if (playerStats == null)
            playerStats = playerMovement.GetComponent<StatsController>();

        if (playerBodyCollider == null)
            playerBodyCollider = playerMovement.GetComponent<Collider>();
    }

    private void WarnIfCamerasAreManagedByAnotherCamera()
    {
        WarnIfCameraIsManaged(defaultCamera);
        WarnIfCameraIsManaged(jumpCamera);
    }

    private void WarnIfCameraIsManaged(CinemachineCamera cameraToCheck)
    {
        if (cameraToCheck == null)
            return;

        CinemachineCameraManagerBase manager =
            cameraToCheck.GetComponentInParent<CinemachineCameraManagerBase>(true);
        if (manager == null)
            return;

        Debug.LogWarning(
            $"{cameraToCheck.name} is a child of {manager.name}. " +
            "A State Driven/Mixing camera chooses its children itself, so child Priority " +
            "cannot perform this switch. Move both CinemachineCamera objects outside the " +
            "manager hierarchy (they can remain under a plain GameObject).",
            this);
    }

    private void OnValidate()
    {
        activePriority = Mathf.Max(activePriority, inactivePriority + 1);
    }
}
