using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerInputController input;
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Collider bodyCollider;
    

    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private bool canMove = true;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 14f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] private float dashCooldown = 0.35f;
    [SerializeField] private float dashJumpForce = 4f;
    [SerializeField] private GameObject dashVfx;
    [SerializeField] private Transform dashTransformVfx;

    [Header("Jump")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private LayerMask groundLayer = -1;
    [SerializeField] private LayerMask platformLayer = 0;
    [SerializeField] private float groundedDistance = 0.12f;
    [SerializeField] private float groundedWidthPadding = 0.05f;
    [SerializeField] private float groundedRayInset = 0.02f;

    [SerializeField] private GameObject jumpVfx;
    [SerializeField] private GameObject jumpLandingVfx;
    [SerializeField] private Transform jumpTransformVfx;

    [SerializeField] private float jumpGroundIgnoreTime = 0.08f;
    [SerializeField] private float minAirborneTimeForLandingVfx = 0.08f;
    [SerializeField] private float minAirborneHeightForLandingVfx = 0.4f;
    [SerializeField] private float lowJumpMultiplier = 2.5f;
    [SerializeField] private float fallMultiplier = 3.5f;

    [Header("Respawn")]
    [SerializeField] private float respawnBelowY = -10f;
    [SerializeField] private bool respawnWhenBelowY = true;
    [SerializeField] private bool canRespawn = true;

    [SerializeField] private GameObject respawnVfx;
    [SerializeField] private Transform respawnTransformVfx;
    [SerializeField] private GameObject respawnPosition;


    [FormerlySerializedAs("fallRespawnYThreshold")]
    [SerializeField] private float fallRespawnDelay = 1f;
    [SerializeField] private int fallOffPlatformScorePenalty = 5;
    [SerializeField] private GameObject fallOfScreenGameObject;


    [SerializeField] private float freezeFrameBelowY = 25f;
    [SerializeField] private GameObject freezedFrame;


    private float jumpGroundIgnoreTimer;
    private float dashTimer;
    private float dashCooldownTimer;
    private float knockbackTimer;
    private Vector3 knockbackVelocity;
    private bool hasInitializedGroundState;
    private bool isRespawnPending;
    private bool suppressNextLandingVfx;
    private bool canSpawnLandingVfxOnNextGrounded;
    private Coroutine fallRespawnCoroutine;
    private float airborneTimer;
    private float airborneStartY;
    private readonly List<Collider> ignoredEnemyCollidersDuringDash = new List<Collider>();
    private readonly List<Collider> ignoredEnemyCollidersDuringJump = new List<Collider>();
    private readonly List<Collider> ignoredPlatformCollidersDuringJump = new List<Collider>();

    public bool IsGrounded { get; private set; }
    public bool IsStandingOnPlatform { get; private set; }
    public bool FacingRight { get; private set; } = true;
    public bool IsDashing => dashTimer > 0f;
    public bool IsUnderKnockback => knockbackTimer > 0f;
    public bool IsRespawnPending => isRespawnPending;
    public bool IsMovingUpward => rb != null && rb.linearVelocity.y > 0.01f;
    public bool CanRespawn
    {
        get => canRespawn;
        set => canRespawn = value;
    }
    public bool CanMove
    {
        get => canMove;
        set => canMove = value;
    }

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
        input = GetComponent<PlayerInputController>();
        scoreController = GetComponent<ScoreController>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        bodyCollider = GetComponent<Collider>();
        if (platformLayer == 0)
            platformLayer = LayerMask.GetMask("Platform");
    }

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (input == null) input = GetComponent<PlayerInputController>();
        if (scoreController == null) scoreController = GetComponent<ScoreController>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider>();

        FacingRight = transform.localScale.x >= 0f;

        SetFreezedFrameVisible(false);
    }

    public void Setup(PlayerInputController inputController)
    {
        input = inputController;
    }

    private void Update()
    {
        if (jumpGroundIgnoreTimer > 0f)
            jumpGroundIgnoreTimer = Mathf.Max(0f, jumpGroundIgnoreTimer - Time.deltaTime);

        if (dashCooldownTimer > 0f)
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

        bool wasGrounded = IsGrounded;
        CheckGround();
        UpdateJumpEnemyPassThrough();
        UpdateAirborneTracking(wasGrounded);
        HandleLanding(wasGrounded);
        UpdateFallVisualState();
        HandleFallRespawn();

        if (isRespawnPending)
            return;

        HandleJump();
        HandleDash();
        HandleFacing();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isRespawnPending)
        {
            ClampToGameplayPlane();
            return;
        }

        if (IsUnderKnockback)
        {
            ApplyKnockbackMovement();
        }
        else if (IsDashing)
        {
            ApplyDashMovement();
        }
        else
        {
            ApplyHorizontalMovement();
        }

        ApplyBetterJumpGravity();
        UpdateJumpPlatformPassThrough();
        ClampToGameplayPlane();
    }

    public void ApplyKnockback(Vector3 velocity, float duration)
    {
        if (rb == null)
            return;

        EndDashEnemyPassThrough();
        EndJumpPlatformPassThrough();
        dashTimer = 0f;

        knockbackVelocity = velocity;
        knockbackVelocity.z = 0f;
        knockbackTimer = Mathf.Max(duration, Time.fixedDeltaTime);
        jumpGroundIgnoreTimer = Mathf.Max(jumpGroundIgnoreTimer, jumpGroundIgnoreTime);
        IsGrounded = false;
        suppressNextLandingVfx = true;
        BeginAirborneTracking();

        Vector3 nextVelocity = rb.linearVelocity;
        nextVelocity.x = knockbackVelocity.x;
        nextVelocity.y = knockbackVelocity.y;
        nextVelocity.z = 0f;
        rb.linearVelocity = nextVelocity;
    }

    private void ApplyHorizontalMovement()
    {
        float moveX = canMove && input != null ? input.MoveX : 0f;
        Vector3 velocity = rb.linearVelocity;
        velocity.x = moveX * moveSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;
    }

    private void ApplyDashMovement()
    {
        float dashDirection = FacingRight ? 1f : -1f;
        Vector3 velocity = rb.linearVelocity;
        velocity.x = dashDirection * dashSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
        {
            dashTimer = 0f;
            EndDashEnemyPassThrough();
        }
    }

    private void ApplyKnockbackMovement()
    {
        Vector3 velocity = rb.linearVelocity;
        float horizontalSpeed = Mathf.MoveTowards(
            velocity.x,
            0f,
            Mathf.Abs(knockbackVelocity.x) * Time.fixedDeltaTime / Mathf.Max(knockbackTimer, Time.fixedDeltaTime));

        velocity.x = horizontalSpeed;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        knockbackTimer = Mathf.Max(0f, knockbackTimer - Time.fixedDeltaTime);
        if (knockbackTimer <= 0f)
            knockbackVelocity = Vector3.zero;
    }

    private void CheckGround()
    {
        if (groundCheck == null || bodyCollider == null)
        {
            IsGrounded = false;
            IsStandingOnPlatform = false;
            return;
        }

        Bounds bounds = bodyCollider.bounds;
        float halfWidth = Mathf.Max(0.025f, (bounds.size.x - groundedWidthPadding) * 0.5f);
        float rayInset = Mathf.Clamp(groundedRayInset, 0f, halfWidth);
        Vector3 originCenter = new Vector3(bounds.center.x, groundCheck.position.y, bounds.center.z);
        Vector3 originLeft = originCenter + Vector3.left * Mathf.Max(0f, halfWidth - rayInset);
        Vector3 originRight = originCenter + Vector3.right * Mathf.Max(0f, halfWidth - rayInset);

        RaycastHit groundHit;
        bool groundedNow =
            TryGetGroundHit(originCenter, out groundHit) ||
            TryGetGroundHit(originLeft, out groundHit) ||
            TryGetGroundHit(originRight, out groundHit);

        if (jumpGroundIgnoreTimer > 0f && rb != null && rb.linearVelocity.y > 0f)
            groundedNow = false;

        IsGrounded = groundedNow;
        IsStandingOnPlatform = groundedNow && IsInLayerMask(groundHit.collider != null ? groundHit.collider.gameObject.layer : -1, platformLayer);

        if (IsGrounded && bodyCollider != null && bodyCollider.isTrigger)
            bodyCollider.isTrigger = false;
    }

    private void HandleJump()
    {
        if (!canMove || input == null || rb == null || !input.JumpPressed || IsDashing)
            return;

        if (!IsGrounded)
            return;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = jumpForce;
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        BeginJumpPlatformPassThrough();
        BeginJumpEnemyPassThrough();

        jumpGroundIgnoreTimer = jumpGroundIgnoreTime;
        IsGrounded = false;
        IsStandingOnPlatform = false;
        BeginAirborneTracking();
        SpawnEffect(jumpVfx, jumpTransformVfx);
    }

    private void HandleDash()
    {
        if (!canMove || input == null || rb == null || !input.DashPressed)
            return;

        if (!IsGrounded)
            return;

        if (IsDashing || dashCooldownTimer > 0f)
            return;

        float dashDirection = ResolveDashDirection();
        if (dashDirection > 0f && !FacingRight)
            SetFacing(true);
        else if (dashDirection < 0f && FacingRight)
            SetFacing(false);

        Vector3 velocity = rb.linearVelocity;
        velocity.x = dashDirection * dashSpeed;
        velocity.y = Mathf.Max(velocity.y, dashJumpForce);
        velocity.z = 0f;
        rb.linearVelocity = velocity;

        BeginDashEnemyPassThrough();
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        jumpGroundIgnoreTimer = Mathf.Max(jumpGroundIgnoreTimer, jumpGroundIgnoreTime);
        IsGrounded = false;
        IsStandingOnPlatform = false;
        BeginAirborneTracking();
        SpawnEffect(dashVfx, dashTransformVfx);
    }

    private void ApplyBetterJumpGravity()
    {
        if (rb == null)
            return;

        Vector3 velocity = rb.linearVelocity;

        if (velocity.y < 0f)
        {
            velocity.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (velocity.y > 0f && (input == null || !input.JumpHeld))
        {
            velocity.y += Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }

        rb.linearVelocity = velocity;
    }

    private float ResolveDashDirection()
    {
        if (input != null)
        {
            if (input.DashDirection > 0.01f)
                return 1f;

            if (input.DashDirection < -0.01f)
                return -1f;

            if (input.MoveX > 0.01f)
                return 1f;

            if (input.MoveX < -0.01f)
                return -1f;
        }

        return FacingRight ? 1f : -1f;
    }

    private void HandleFacing()
    {
        if (input == null || IsDashing)
            return;

        if (input.MoveX > 0.01f)
            SetFacing(true);
        else if (input.MoveX < -0.01f)
            SetFacing(false);
    }

    public void FaceRight()
    {
        SetFacing(true);
    }

    private void SetFacing(bool faceRight)
    {
        if (FacingRight == faceRight)
            return;

        FacingRight = faceRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    private void ClampToGameplayPlane()
    {
        float gameplayPlaneZ = GetGameplayPlaneZ();

        if (rb != null)
        {
            Vector3 velocity = rb.linearVelocity;
            if (Mathf.Abs(velocity.z) > 0.0001f)
            {
                velocity.z = 0f;
                rb.linearVelocity = velocity;
            }
        }

        Vector3 position = transform.position;
        if (Mathf.Abs(position.z - gameplayPlaneZ) <= 0.0001f)
            return;

        position.z = gameplayPlaneZ;
        transform.position = position;
    }

    private float GetGameplayPlaneZ()
    {
        return respawnPosition != null ? respawnPosition.transform.position.z : 0f;
    }

    private void HandleFallRespawn()
    {
        if (!respawnWhenBelowY || !canRespawn || isRespawnPending)
            return;

        if (transform.position.y >= respawnBelowY)
            return;

        BeginFallRespawn();
    }

    public void Respawn()
    {
        if (!canRespawn)
            return;

        CancelPendingRespawn();
        EndDashEnemyPassThrough();
        EndJumpEnemyPassThrough();
        EndJumpPlatformPassThrough();

        dashTimer = 0f;
        dashCooldownTimer = 0f;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        jumpGroundIgnoreTimer = 0f;
        IsGrounded = false;
        IsStandingOnPlatform = false;
        suppressNextLandingVfx = true;
        ResetAirborneTracking();

        if (bodyCollider != null && bodyCollider.isTrigger)
            bodyCollider.isTrigger = false;

        Vector3 _respawnPosition = respawnPosition.transform.position;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.position = _respawnPosition;
            transform.position = _respawnPosition;
        }
        else
        {
            transform.position = _respawnPosition;
        }

        SetSpriteVisible(true);
        SetFreezedFrameVisible(false);
        SpawnEffect(respawnVfx, respawnTransformVfx);
        CheckGround();
        if (IsGrounded)
        {
            suppressNextLandingVfx = false;
            ResetAirborneTracking();
        }

        ClampToGameplayPlane();
    }

    public void SetBodyTrigger(bool enabled)
    {
        if (bodyCollider == null)
            return;

        bodyCollider.isTrigger = enabled;
    }

    public void MoveHorizontally(float offset)
    {
        if (Mathf.Approximately(offset, 0f))
            return;

        Vector3 nextPosition = rb != null ? rb.position : transform.position;
        nextPosition.x += offset;
        nextPosition.z = GetGameplayPlaneZ();

        if (rb != null)
        {
            rb.position = nextPosition;
            transform.position = nextPosition;
            return;
        }

        transform.position = nextPosition;
    }

    private void BeginDashEnemyPassThrough()
    {
        if (bodyCollider == null)
            return;

        EndDashEnemyPassThrough();

        EnemyMovement[] enemies = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < enemies.Length; i++)
        {
            EnemyMovement enemy = enemies[i];
            if (enemy == null)
                continue;

            Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>(true);
            for (int j = 0; j < enemyColliders.Length; j++)
            {
                Collider enemyCollider = enemyColliders[j];
                if (enemyCollider == null || enemyCollider == bodyCollider)
                    continue;

                Physics.IgnoreCollision(bodyCollider, enemyCollider, true);
                ignoredEnemyCollidersDuringDash.Add(enemyCollider);
            }
        }
    }

    private void EndDashEnemyPassThrough()
    {
        if (bodyCollider == null)
            return;

        for (int i = ignoredEnemyCollidersDuringDash.Count - 1; i >= 0; i--)
        {
            Collider enemyCollider = ignoredEnemyCollidersDuringDash[i];
            if (enemyCollider != null)
                Physics.IgnoreCollision(bodyCollider, enemyCollider, false);
        }

        ignoredEnemyCollidersDuringDash.Clear();
    }

    private void BeginJumpPlatformPassThrough()
    {
        if (bodyCollider == null)
            return;

        EndJumpPlatformPassThrough();

        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider platformCollider = colliders[i];
            if (platformCollider == null ||
                platformCollider == bodyCollider ||
                !IsInLayerMask(platformCollider.gameObject.layer, platformLayer))
            {
                continue;
            }

            Physics.IgnoreCollision(bodyCollider, platformCollider, true);
            ignoredPlatformCollidersDuringJump.Add(platformCollider);
        }
    }

    private void UpdateJumpPlatformPassThrough()
    {
        if (ignoredPlatformCollidersDuringJump.Count == 0 || rb == null || rb.linearVelocity.y > 0f)
            return;

        EndJumpPlatformPassThrough();
    }

    private void EndJumpPlatformPassThrough()
    {
        if (bodyCollider == null)
            return;

        for (int i = ignoredPlatformCollidersDuringJump.Count - 1; i >= 0; i--)
        {
            Collider platformCollider = ignoredPlatformCollidersDuringJump[i];
            if (platformCollider != null)
                Physics.IgnoreCollision(bodyCollider, platformCollider, false);
        }

        ignoredPlatformCollidersDuringJump.Clear();
    }

    private void BeginJumpEnemyPassThrough()
    {
        if (bodyCollider == null)
            return;

        EndJumpEnemyPassThrough();

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider enemyCollider = colliders[i];
            if (enemyCollider == null ||
                enemyCollider == bodyCollider ||
                enemyCollider.gameObject.layer != enemyLayer)
            {
                continue;
            }

            Physics.IgnoreCollision(bodyCollider, enemyCollider, true);
            ignoredEnemyCollidersDuringJump.Add(enemyCollider);
        }
    }

    private void UpdateJumpEnemyPassThrough()
    {
        if (ignoredEnemyCollidersDuringJump.Count > 0 && IsGrounded)
            EndJumpEnemyPassThrough();
    }

    private void EndJumpEnemyPassThrough()
    {
        if (bodyCollider == null)
            return;

        for (int i = ignoredEnemyCollidersDuringJump.Count - 1; i >= 0; i--)
        {
            Collider enemyCollider = ignoredEnemyCollidersDuringJump[i];
            if (enemyCollider != null)
                Physics.IgnoreCollision(bodyCollider, enemyCollider, false);
        }

        ignoredEnemyCollidersDuringJump.Clear();
    }

    private void OnDisable()
    {
        CancelPendingRespawn();
        EndDashEnemyPassThrough();
        EndJumpEnemyPassThrough();
        EndJumpPlatformPassThrough();
    }

    private void HandleLanding(bool wasGrounded)
    {
        if (!hasInitializedGroundState)
        {
            hasInitializedGroundState = true;
            return;
        }

        if (wasGrounded || !IsGrounded)
            return;

        if (suppressNextLandingVfx)
        {
            suppressNextLandingVfx = false;
            ResetAirborneTracking();
            return;
        }

        if (!canSpawnLandingVfxOnNextGrounded)
        {
            ResetAirborneTracking();
            return;
        }

        SpawnEffect(jumpLandingVfx, jumpTransformVfx);
        ResetAirborneTracking();
    }

    private void BeginFallRespawn()
    {
        if (isRespawnPending)
            return;

        isRespawnPending = true;
        ApplyFallOffPlatformScorePenalty();
        suppressNextLandingVfx = true;
        EndDashEnemyPassThrough();
        EndJumpEnemyPassThrough();
        EndJumpPlatformPassThrough();
        dashTimer = 0f;
        knockbackTimer = 0f;
        knockbackVelocity = Vector3.zero;
        jumpGroundIgnoreTimer = 0f;
        IsGrounded = false;
        IsStandingOnPlatform = false;
        BeginAirborneTracking();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SetSpriteVisible(false);
        SetFreezedFrameVisible(true);
        SpawnEffect(fallOfScreenGameObject, respawnTransformVfx);
        fallRespawnCoroutine = StartCoroutine(RespawnAfterDelay());
    }

    private void ApplyFallOffPlatformScorePenalty()
    {
        if (scoreController == null)
            return;

        int penalty = Mathf.Abs(fallOffPlatformScorePenalty);
        if (penalty <= 0)
            return;

        scoreController.AddScore(-penalty);
    }

    private void UpdateAirborneTracking(bool wasGrounded)
    {
        if (wasGrounded && !IsGrounded)
            BeginAirborneTracking();

        if (IsGrounded)
        {
            if (wasGrounded)
                airborneStartY = transform.position.y;

            airborneTimer = 0f;
            return;
        }

        airborneTimer += Time.deltaTime;
        float airborneHeight = Mathf.Abs(transform.position.y - airborneStartY);
        if (airborneTimer >= minAirborneTimeForLandingVfx || airborneHeight >= minAirborneHeightForLandingVfx)
            canSpawnLandingVfxOnNextGrounded = true;
    }

    private void BeginAirborneTracking()
    {
        airborneTimer = 0f;
        airborneStartY = transform.position.y;
        canSpawnLandingVfxOnNextGrounded = false;
    }

    private void ResetAirborneTracking()
    {
        airborneTimer = 0f;
        airborneStartY = transform.position.y;
        canSpawnLandingVfxOnNextGrounded = false;
    }

    private System.Collections.IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(fallRespawnDelay);
        fallRespawnCoroutine = null;
        isRespawnPending = false;
        Respawn();
    }

    private void CancelPendingRespawn()
    {
        if (fallRespawnCoroutine != null)
        {
            StopCoroutine(fallRespawnCoroutine);
            fallRespawnCoroutine = null;
        }

        isRespawnPending = false;
    }

    private void SpawnEffect(GameObject effectGameObject, Transform effectTransform)
    {
        if (effectGameObject == null)
            return;

        Transform spawnTransform = effectTransform != null ? effectTransform : transform;
        if (!spawnTransform.gameObject.activeInHierarchy)
            return;

        MyPoolManager poolManager = MyPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogWarning("MyPoolManager.Instance is null, cannot spawn movement VFX from pool.", this);
            return;
        }

        GameObject spawnedEffect = poolManager.Get(effectGameObject);
        if (spawnedEffect == null)
            return;

        Transform spawnedTransform = spawnedEffect.transform;
        spawnedTransform.SetParent(null, false);
        spawnedTransform.SetPositionAndRotation(spawnTransform.position, spawnTransform.rotation);
        spawnedTransform.localScale = effectGameObject.transform.localScale;
    }

    private bool TryGetGroundHit(Vector3 origin, out RaycastHit hit)
    {
        return Physics.Raycast(origin, Vector3.down, out hit, groundedDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        if (layer < 0)
            return false;

        return (mask.value & (1 << layer)) != 0;
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
    }

    private void SetFreezedFrameVisible(bool visible)
    {
        if (freezedFrame != null && freezedFrame.activeSelf != visible)
            freezedFrame.SetActive(visible);
    }

    private void UpdateFallVisualState()
    {
        bool shouldShowFreezeFrame =
            !IsGrounded &&
            transform.position.y < freezeFrameBelowY;

        if (!isRespawnPending)
            SetSpriteVisible(!shouldShowFreezeFrame);

        SetFreezedFrameVisible(shouldShowFreezeFrame);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null || bodyCollider == null)
            return;

        Bounds bounds = bodyCollider.bounds;
        float halfWidth = Mathf.Max(0.025f, (bounds.size.x - groundedWidthPadding) * 0.5f);
        float rayInset = Mathf.Clamp(groundedRayInset, 0f, halfWidth);
        Vector3 originCenter = new Vector3(bounds.center.x, groundCheck.position.y, bounds.center.z);
        Vector3 originLeft = originCenter + Vector3.left * Mathf.Max(0f, halfWidth - rayInset);
        Vector3 originRight = originCenter + Vector3.right * Mathf.Max(0f, halfWidth - rayInset);

        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(originCenter, originCenter + Vector3.down * groundedDistance);
        Gizmos.DrawLine(originLeft, originLeft + Vector3.down * groundedDistance);
        Gizmos.DrawLine(originRight, originRight + Vector3.down * groundedDistance);

        
    }

    
}
