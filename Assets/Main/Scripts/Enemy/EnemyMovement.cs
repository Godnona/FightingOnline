using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float updateSpeed = 0.1f;
    [SerializeField] private float facingDeadZone = 0.01f;
    [SerializeField] private float facingTargetOffset = 1f;
    [Header("Off Mesh Jump")]
    [SerializeField] private float offMeshJumpHeight = 40.0f;
    [SerializeField] private float offMeshJumpDuration = 0.5f;

    private NavMeshAgent agent;
    private Coroutine knockbackRoutine;
    private float stopDistance;
    private bool facingRight;
    private bool isTraversingOffMeshLink;
    private bool movementLocked;

    private float updateTimer;
    private Quaternion initialLocalRotation;

    public bool IsJumping => isTraversingOffMeshLink;
    public bool IsUnderKnockback => knockbackRoutine != null;
    public bool IsMovementLocked => movementLocked;

    private void Awake()
    {
        facingRight = transform.localScale.x >= 0f;
        initialLocalRotation = transform.localRotation;
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        ConfigureAgent();
    }

    private void ConfigureAgent()
    {
        if (agent == null)
            return;

        agent.autoBraking = false;
        agent.autoTraverseOffMeshLink = false;
        stopDistance = agent.stoppingDistance;
        agent.updateRotation = false;
        agent.updateUpAxis = false;
    }

    private void Update()
    {
        if (agent == null || !agent.enabled || target == null)
            return;

        if (movementLocked || IsUnderKnockback)
        {
            StopImmediately();
            return;
        }

        if (!isTraversingOffMeshLink && agent.isOnOffMeshLink)
        {
            StartCoroutine(TraverseOffMeshLinkJump());
            return;
        }

        if (isTraversingOffMeshLink)
            return;

        float distanceToTarget = Vector3.Distance(transform.position, target.position);
        if (distanceToTarget <= stopDistance)
        {
            FaceTargetHorizontally();
            StopImmediately();
            return;
        }

        if (agent.isStopped)
            agent.isStopped = false;

        UpdateFacing();

        updateTimer -= Time.deltaTime;
        if (updateTimer > 0f)
            return;

        updateTimer = updateSpeed;
        agent.SetDestination(target.position);
    }

    private void StopImmediately()
    {
        if (agent == null || !agent.enabled)
            return;

        if (!agent.isStopped)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        agent.velocity = Vector3.zero;
        agent.nextPosition = transform.position;
    }

    public void SetMovementLocked(bool locked)
    {
        movementLocked = locked;
        if (locked)
        {
            StopImmediately();
            return;
        }

        SyncAgentToTransform();
    }

    public void SetNavigationActive(bool active)
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
            return;

        if (!active)
        {
            if (agent.enabled)
            {
                StopImmediately();
                agent.enabled = false;
            }

            return;
        }

        if (!agent.enabled)
            agent.enabled = true;

        ConfigureAgent();
        SyncAgentToTransform();
    }

    public void ApplyKnockback(Vector3 velocity, float duration)
    {
        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(velocity, duration));
    }

    private void LateUpdate()
    {
        if (transform.localRotation != initialLocalRotation)
            transform.localRotation = initialLocalRotation;
    }

    private void UpdateFacing()
    {
        float targetHorizontalDelta = GetTargetHorizontalDelta();
        if (Mathf.Abs(targetHorizontalDelta) <= facingTargetOffset)
            return;

        float horizontalDirection = 0f;

        if (agent != null && agent.enabled)
        {
            Vector3 movementDirection = agent.desiredVelocity;
            if (movementDirection.sqrMagnitude <= facingDeadZone * facingDeadZone && agent.hasPath)
                movementDirection = agent.steeringTarget - transform.position;

            horizontalDirection = movementDirection.x;
        }

        if (Mathf.Abs(horizontalDirection) <= facingDeadZone)
            horizontalDirection = targetHorizontalDelta;

        ApplyFacing(horizontalDirection);
    }

    private void FaceTargetHorizontally()
    {
        if (target == null)
            return;

        float targetHorizontalDelta = GetTargetHorizontalDelta();
        if (Mathf.Abs(targetHorizontalDelta) <= facingTargetOffset)
            return;

        ApplyFacing(targetHorizontalDelta);
    }

    private float GetTargetHorizontalDelta()
    {
        if (target == null)
            return 0f;

        return target.position.x - transform.position.x;
    }

    private void ApplyFacing(float horizontalDirection)
    {
        if (Mathf.Abs(horizontalDirection) <= facingDeadZone)
            return;

        bool shouldFaceRight = horizontalDirection > 0f;
        if (shouldFaceRight == facingRight)
            return;

        facingRight = shouldFaceRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    public void FaceRight()
    {
        ApplyFacing(1f);
    }

    private IEnumerator KnockbackRoutine(Vector3 velocity, float duration)
    {
        StopImmediately();

        float totalDuration = Mathf.Max(0.01f, duration);
        float remainingDuration = totalDuration;
        float initialHorizontalSpeed = velocity.x;

        while (remainingDuration > 0f)
        {
            float normalizedTime = remainingDuration / totalDuration;
            float horizontalSpeed = initialHorizontalSpeed * normalizedTime;

            Vector3 nextPosition = transform.position;
            nextPosition.x += horizontalSpeed * Time.deltaTime;
            nextPosition.z = 0f;
            nextPosition = ResolveKnockbackPosition(nextPosition);

            transform.position = nextPosition;
            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.nextPosition = nextPosition;

            remainingDuration -= Time.deltaTime;
            yield return null;
        }

        SyncAgentToTransform();
        knockbackRoutine = null;
    }

    private Vector3 ResolveKnockbackPosition(Vector3 desiredPosition)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return desiredPosition;

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit navMeshHit, Mathf.Max(agent.radius * 2f, 0.5f), agent.areaMask))
        {
            desiredPosition.x = navMeshHit.position.x;
            desiredPosition.y = navMeshHit.position.y;
        }

        return desiredPosition;
    }

    private void SyncAgentToTransform()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        agent.Warp(transform.position);
        agent.nextPosition = transform.position;
        agent.velocity = Vector3.zero;
        updateTimer = 0f;
    }

    private IEnumerator TraverseOffMeshLinkJump()
    {
        isTraversingOffMeshLink = true;

        OffMeshLinkData linkData = agent.currentOffMeshLinkData;
        Vector3 startPosition = transform.position;
        Vector3 endPosition = linkData.endPos;
        endPosition.z = startPosition.z;

        float landingYOffset = startPosition.y - linkData.startPos.y;
        if (Mathf.Abs(landingYOffset) < 0.01f)
            landingYOffset = agent.baseOffset;

        endPosition.y += landingYOffset;

        ApplyFacing(endPosition.x - startPosition.x);

        float duration = Mathf.Max(0.05f, offMeshJumpDuration);
        float jumpHeight = Mathf.Max(0.05f, offMeshJumpHeight);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 nextPosition = Vector3.Lerp(startPosition, endPosition, t);
            nextPosition.y += 4f * jumpHeight * t * (1f - t);

            transform.position = nextPosition;
            agent.nextPosition = nextPosition;

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = endPosition;
        agent.nextPosition = endPosition;
        agent.CompleteOffMeshLink();
        isTraversingOffMeshLink = false;
    }
}
