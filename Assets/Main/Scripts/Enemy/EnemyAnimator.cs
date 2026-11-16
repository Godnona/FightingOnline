using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAnimator : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public PlayerInputController.AttackType attackType;
        public string animatorTrigger;
        public HitBoxController hitBox;
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private EnemyMovement movement;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private StatsController stats;
    [SerializeField] private ScoreController score;

    [Header("Thresholds")]
    [SerializeField] private float runSpeedThreshold = 0.05f;
    [SerializeField] private float attackRangeBuffer = 0.15f;

    [Header("Attacks")]
    [SerializeField] private List<AttackData> attacks = new List<AttackData>();
    [SerializeField] private float attackDelay = 1f;

    private readonly HashSet<string> boolParameters = new HashSet<string>();
    private readonly HashSet<string> triggerParameters = new HashSet<string>();

    private AttackData currentAttack;
    private bool attackLocked;
    private bool isAttacking;
    private float nextAttackTime;

    private void Reset()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        movement = GetComponent<EnemyMovement>();
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<StatsController>();
        score = GetComponent<ScoreController>();
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (movement == null) movement = GetComponent<EnemyMovement>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (stats == null) stats = GetComponent<StatsController>();
        if (score == null) score = GetComponent<ScoreController>();

        CacheParameters();
        RebuildAttackCache();
    }

    private void Update()
    {
        TryStartRandomAttack();
    }

    private void LateUpdate()
    {
        ApplyLocomotionState();
    }

    public void FinishAnimator()
    {
        DeactivateCurrentHitBox();
        isAttacking = false;
        nextAttackTime = Time.time + Mathf.Max(0f, attackDelay);
        currentAttack = null;

        ApplyLocomotionState();
    }

    public void SetAttackLocked(bool locked)
    {
        attackLocked = locked;
        if (!locked)
            return;

        DeactivateCurrentHitBox();
        currentAttack = null;
        isAttacking = false;
    }

    public bool IsStatePlaying(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName);
    }

    public bool IsStateFinished(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return true;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (!stateInfo.IsName(stateName))
            return false;

        if (animator.IsInTransition(0))
            return false;

        return stateInfo.normalizedTime >= 1f;
    }

    public float GetStateDuration(string stateName)
    {
        if (animator == null || string.IsNullOrWhiteSpace(stateName))
            return 0f;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.IsName(stateName) ? stateInfo.length : 0f;
    }

    public void ActivateCurrentHitBox()
    {
        if (currentAttack != null && currentAttack.hitBox != null)
            currentAttack.hitBox.Activate();
    }

    public void DeactivateCurrentHitBox()
    {
        if (currentAttack != null && currentAttack.hitBox != null)
            currentAttack.hitBox.Deactivate();
    }

    private void ApplyLocomotionState()
    {
        if (animator == null)
            return;

        bool isJumping = movement != null && movement.IsJumping;
        bool isRunning = !isAttacking && !isJumping && HasHorizontalMovement();
        bool isIdle = !isAttacking && !isJumping && !isRunning;

        SetBoolIfExists("IsGrounded", !isJumping);
        SetBoolIfExists("IsRunning", isRunning);
        SetBoolIfExists("IsIdle", isIdle);
        SetBoolIfExists("IsJumping", isJumping);
    }

    private void TryStartRandomAttack()
    {
        if (animator == null || attackLocked || isAttacking || Time.time < nextAttackTime)
            return;

        if (!CanAttackTarget())
            return;

        AttackData attack = GetRandomAttack();
        if (attack == null)
            return;

        StartAttack(attack);
    }

    private bool CanAttackTarget()
    {
        if (movement == null || movement.IsJumping || agent == null)
            return false;

        if (!agent.enabled)
            return false;

        if (!agent.isOnNavMesh)
            return false;

        if (HasHorizontalMovement())
            return false;

        if (agent.pathPending)
            return false;

        if (!agent.isStopped && agent.remainingDistance > agent.stoppingDistance + attackRangeBuffer)
            return false;

        if (agent.hasPath && agent.remainingDistance > agent.stoppingDistance + attackRangeBuffer)
            return false;

        return true;
    }

    private AttackData GetRandomAttack()
    {
        List<AttackData> validAttacks = null;

        for (int i = 0; i < attacks.Count; i++)
        {
            AttackData attack = attacks[i];
            if (attack == null || !HasTrigger(attack.animatorTrigger))
                continue;

            if (stats != null && !stats.CanUseRipp(ResolveAttackRippCost(attack)))
                continue;

            if (validAttacks == null)
                validAttacks = new List<AttackData>();

            validAttacks.Add(attack);
        }

        if (validAttacks == null || validAttacks.Count == 0)
            return null;

        return validAttacks[Random.Range(0, validAttacks.Count)];
    }

    private int ResolveAttackRippCost(AttackData attack)
    {
        if (attack == null || attack.hitBox == null)
            return 0;

        return attack.hitBox.RippCost;
    }

    private void StartAttack(AttackData attack)
    {
        if (attackLocked)
            return;

        int attackRippCost = ResolveAttackRippCost(attack);
        if (stats != null && !stats.SpendRipp(attackRippCost))
            return;

        currentAttack = attack;
        isAttacking = true;

        ResetLocomotionAnimatorState();
        animator.SetTrigger(attack.animatorTrigger);
    }

    private void OnDisable()
    {
        DisableAllHitBoxes();
        currentAttack = null;
        isAttacking = false;
    }

    private void RebuildAttackCache()
    {
        for (int i = 0; i < attacks.Count; i++)
        {
            AttackData attack = attacks[i];
            if (attack == null || attack.hitBox == null)
                continue;

            attack.hitBox.BindOwner(stats, score);
            attack.hitBox.Deactivate();
        }
    }

    private void DisableAllHitBoxes()
    {
        for (int i = 0; i < attacks.Count; i++)
        {
            AttackData attack = attacks[i];
            if (attack != null && attack.hitBox != null)
                attack.hitBox.Deactivate();
        }
    }

    private void ResetLocomotionAnimatorState()
    {
        SetBoolIfExists("IsIdle", false);
        SetBoolIfExists("IsRunning", false);
        SetBoolIfExists("IsJumping", false);
    }

    private bool HasHorizontalMovement()
    {
        if (agent == null || !agent.enabled || agent.isStopped)
            return false;

        Vector3 movementVelocity = agent.desiredVelocity.sqrMagnitude > 0.0001f
            ? agent.desiredVelocity
            : agent.velocity;

        return Mathf.Abs(movementVelocity.x) > runSpeedThreshold;
    }

    private void CacheParameters()
    {
        boolParameters.Clear();
        triggerParameters.Clear();

        if (animator == null)
            return;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.type == AnimatorControllerParameterType.Bool)
                boolParameters.Add(parameter.name);
            else if (parameter.type == AnimatorControllerParameterType.Trigger)
                triggerParameters.Add(parameter.name);
        }
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (animator == null || !boolParameters.Contains(parameterName))
            return;

        animator.SetBool(parameterName, value);
    }

    private bool HasTrigger(string triggerName)
    {
        return !string.IsNullOrWhiteSpace(triggerName) && triggerParameters.Contains(triggerName);
    }
}
