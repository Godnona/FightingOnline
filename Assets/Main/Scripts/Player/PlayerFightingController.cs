using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Playables;

public class PlayerFightingController : MonoBehaviour
{
    [System.Serializable]
    public class AttackData
    {
        public PlayerInputController.AttackType attackType;
        public string animatorTrigger;
        public float cooldown = 0.1f;
        public int rippCost;
        public HitBoxController hitBox;
    }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerInputController input;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private StatsController stats;
    [SerializeField] private ScoreController score;
    [SerializeField] private Rigidbody body;

    [Header("Timeline Vfx")]
    [SerializeField] private PlayableDirector timelineVfxSpecialRight;
    [SerializeField] private PlayableDirector timelineVfxSpecialLeft;
    [SerializeField] private PlayableDirector timelineVfxSpecialQuakeRight;
    [SerializeField] private PlayableDirector timelineVfxSpecialQuakeLeft;

    [Header("Direct Attack VFX Spawn")]
    [SerializeField] private Transform defaultAttackVfxSpawnPoint;

    [Header("Enemy Components Disabled By Animation Event")]
    [Tooltip("Exact component type names on enemy objects to disable, for example: EnemyAnimator, EnemyMovement, NavMeshAgent")]
    [SerializeField] private List<string> enemyComponentTypeNamesToDisable = new List<string>();

    [Header("Attacks")]
    [SerializeField] private List<AttackData> attacks = new List<AttackData>();

    private readonly Dictionary<PlayerInputController.AttackType, AttackData> attackMap = new Dictionary<PlayerInputController.AttackType, AttackData>();
    private readonly Dictionary<PlayerInputController.AttackType, float> readyTimes = new Dictionary<PlayerInputController.AttackType, float>();
    private readonly Dictionary<Behaviour, bool> suspendedEnemyBehaviours = new Dictionary<Behaviour, bool>();

    private AttackData currentAttack;
    private PlayableDirector currentTimelineDirector;
    private bool attackLocked;

    public bool IsAttacking { get; private set; }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (input == null) input = GetComponent<PlayerInputController>();
        if (movement == null) movement = GetComponent<PlayerMovement>();
        if (stats == null) stats = GetComponent<StatsController>();
        if (score == null) score = GetComponent<ScoreController>();
        if (body == null) body = GetComponent<Rigidbody>();

        RebuildAttackCache();
    }

    public void Setup(PlayerInputController inputController, PlayerMovement movementController, StatsController statsController, ScoreController scoreController)
    {
        input = inputController;
        movement = movementController;
        stats = statsController;
        score = scoreController;
        if (body == null) body = GetComponent<Rigidbody>();

        RebuildAttackCache();
    }

    private void Update()
    {
        if (attackLocked || input == null || IsAttacking || input.BufferedAttack == PlayerInputController.AttackType.None)
            return;

        AttackData attack = GetAttackForState(input.BufferedAttack);
        if (attack == null || !CanStartAttack(attack))
            return;

        StartAttack(attack);
    }

    public void SetAttackLocked(bool locked)
    {
        attackLocked = locked;

        if (locked)
            FinishAttack();
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

    public void DisableAllHitBoxes()
    {
        for (int i = 0; i < attacks.Count; i++)
        {
            AttackData attack = attacks[i];
            if (attack != null && attack.hitBox != null)
                attack.hitBox.Deactivate();
        }
    }

    public void FinishAttack()
    {
        if (!IsAttacking)
            return;

        RestoreDisabledEnemyComponents();

        if (currentAttack != null && currentAttack.hitBox != null)
            currentAttack.hitBox.Deactivate();

        StopCurrentTimeline();

        if (movement != null && !attackLocked)
            movement.CanMove = true;

        currentAttack = null;
        IsAttacking = false;
    }

    public void SpawnVfxAttack(GameObject vfxPrefab)
    {
        if (vfxPrefab == null)
            return;

        Transform spawnPoint = defaultAttackVfxSpawnPoint != null ? defaultAttackVfxSpawnPoint : transform;
        MyPoolManager poolManager = MyPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogWarning("MyPoolManager.Instance is null, cannot spawn attack VFX from pool.", this);
            return;
        }

        GameObject spawnedVfx = poolManager.Get(vfxPrefab);
        if (spawnedVfx == null)
            return;

        Transform spawnedTransform = spawnedVfx.transform;
        spawnedTransform.SetParent(spawnPoint, false);
        spawnedTransform.localPosition = Vector3.zero;
        spawnedTransform.localRotation = Quaternion.identity;
        spawnedTransform.localScale = vfxPrefab.transform.localScale;
    }

    private void OnDisable()
    {
        RestoreDisabledEnemyComponents();
        DisableAllHitBoxes();
        StopCurrentTimeline();
        currentAttack = null;
        IsAttacking = false;
    }

    /// Call this from an Animation Event when the attack reaches the frame that should freeze enemy logic.
    public void DisableConfiguredEnemyComponents()
    {
        RestoreDisabledEnemyComponents();

        if (enemyComponentTypeNamesToDisable == null || enemyComponentTypeNamesToDisable.Count == 0)
            return;

        HashSet<string> targetTypeNames = BuildEnemyComponentTypeNameSet();
        if (targetTypeNames.Count == 0)
            return;

        List<GameObject> enemyRoots = CollectEnemyRoots();
        for (int i = 0; i < enemyRoots.Count; i++)
        {
            GameObject enemyRoot = enemyRoots[i];
            if (enemyRoot == null)
                continue;

            Behaviour[] enemyBehaviours = enemyRoot.GetComponentsInChildren<Behaviour>(true);
            for (int j = 0; j < enemyBehaviours.Length; j++)
            {
                Behaviour behaviour = enemyBehaviours[j];
                if (behaviour == null || behaviour == this)
                    continue;

                string typeName = behaviour.GetType().Name;
                if (!targetTypeNames.Contains(typeName))
                    continue;

                if (suspendedEnemyBehaviours.ContainsKey(behaviour))
                    continue;

                suspendedEnemyBehaviours[behaviour] = behaviour.enabled;

                if (behaviour is NavMeshAgent navMeshAgent && navMeshAgent.enabled)
                {
                    navMeshAgent.ResetPath();
                    navMeshAgent.velocity = Vector3.zero;
                }

                behaviour.enabled = false;
            }
        }
    }

    /// Call this from an Animation Event, or let FinishAttack restore everything automatically.
    public void RestoreDisabledEnemyComponents()
    {
        foreach (KeyValuePair<Behaviour, bool> pair in suspendedEnemyBehaviours)
        {
            if (pair.Key != null)
                pair.Key.enabled = pair.Value;
        }

        suspendedEnemyBehaviours.Clear();
    }

    private void RebuildAttackCache()
    {
        attackMap.Clear();

        for (int i = 0; i < attacks.Count; i++)
        {
            AttackData attack = attacks[i];
            if (attack == null)
                continue;

            if (string.IsNullOrWhiteSpace(attack.animatorTrigger))
                attack.animatorTrigger = GetDefaultTriggerName(attack.attackType);

            attackMap[attack.attackType] = attack;

            if (attack.hitBox != null)
            {
                attack.hitBox.BindOwner(this, stats, score);
                attack.hitBox.Deactivate();
            }
        }
    }

    private AttackData GetAttackForState(PlayerInputController.AttackType attackType)
    {
        if (movement != null && !movement.IsGrounded)
        {
            if (attackType == PlayerInputController.AttackType.Light)
                attackType = PlayerInputController.AttackType.AirLight;
            else if (attackType == PlayerInputController.AttackType.Heavy)
                attackType = PlayerInputController.AttackType.AirHeavy;
        }
        else
        {
            if (attackType == PlayerInputController.AttackType.AirLight)
                attackType = PlayerInputController.AttackType.Light;
            else if (attackType == PlayerInputController.AttackType.AirHeavy)
                attackType = PlayerInputController.AttackType.Heavy;
        }

        attackMap.TryGetValue(attackType, out AttackData attack);
        return attack;
    }

    private int ResolveAttackRippCost(AttackData attack)
    {
        if (attack == null)
            return 0;

        if (attack.rippCost > 0)
            return attack.rippCost;

        return attack.hitBox != null ? attack.hitBox.RippCost : 0;
    }

    private bool CanStartAttack(AttackData attack)
    {
        if (attack == null)
            return false;

        if (movement != null && movement.IsDashing)
            return false;

        if (readyTimes.TryGetValue(attack.attackType, out float readyTime) && Time.time < readyTime)
            return false;

        return stats == null || stats.CanUseRipp(ResolveAttackRippCost(attack));
    }

    private void StartAttack(AttackData attack)
    {
        if (attackLocked)
            return;

        int attackRippCost = ResolveAttackRippCost(attack);
        if (stats != null && !stats.SpendRipp(attackRippCost))
            return;

        currentAttack = attack;
        IsAttacking = true;
        readyTimes[attack.attackType] = Time.time + Mathf.Max(0f, attack.cooldown);

        if (input != null)
            input.ConsumeAttack();

        if (movement != null)
            movement.CanMove = false;

        if (body != null)
        {
            Vector3 velocity = body.linearVelocity;
            velocity.x = 0f;
            velocity.z = 0f;
            body.linearVelocity = velocity;
        }

        DisableAllHitBoxes();
        ResetLocomotionAnimatorState();

        if (animator != null && HasTrigger(attack.animatorTrigger))
            animator.SetTrigger(attack.animatorTrigger);

        PlayAttackTimeline(attack.attackType);
    }

    private void PlayAttackTimeline(PlayerInputController.AttackType attackType)
    {
        PlayableDirector timelineDirector = GetTimelineDirector(attackType);
        if (timelineDirector == null)
            return;

        StopCurrentTimeline();
        currentTimelineDirector = timelineDirector;
        ResetTimelineDirector(currentTimelineDirector);
        currentTimelineDirector.time = 0d;
        currentTimelineDirector.Evaluate();
        currentTimelineDirector.Play();
    }

    private void StopCurrentTimeline()
    {
        if (currentTimelineDirector == null)
            return;

        ResetTimelineDirector(currentTimelineDirector);
        currentTimelineDirector = null;
    }

    private void ResetTimelineDirector(PlayableDirector director)
    {
        if (director == null)
            return;

        director.Stop();
        director.RebuildGraph();
        director.time = 0d;
        director.Evaluate();
    }

    private PlayableDirector GetTimelineDirector(PlayerInputController.AttackType attackType)
    {
        bool facingRight = movement == null ? transform.localScale.x >= 0f : movement.FacingRight;

        switch (attackType)
        {
            case PlayerInputController.AttackType.Special:
                return facingRight ? timelineVfxSpecialRight : timelineVfxSpecialLeft;
            case PlayerInputController.AttackType.SpecialQuake:
                return facingRight ? timelineVfxSpecialQuakeRight : timelineVfxSpecialQuakeLeft;
            default:
                return null;
        }
    }

    private void ResetLocomotionAnimatorState()
    {
        SetBoolIfExists("IsIdle", false);
        SetBoolIfExists("IsRunning", false);
        SetBoolIfExists("IsJumping", false);
        SetBoolIfExists("MidAirFall", false);
    }

    private string GetDefaultTriggerName(PlayerInputController.AttackType attackType)
    {
        switch (attackType)
        {
            case PlayerInputController.AttackType.Light: return "LightAttack";
            case PlayerInputController.AttackType.Heavy: return "HeavyAttack";
            case PlayerInputController.AttackType.DownLight: return "DownLightAttack";
            case PlayerInputController.AttackType.Punch: return "Punch";
            case PlayerInputController.AttackType.Kick: return "AttackKick";
            case PlayerInputController.AttackType.Parry: return "Parry";
            case PlayerInputController.AttackType.Special: return "Special";
            case PlayerInputController.AttackType.SpecialQuake: return "AttackSpecialQuake";
            case PlayerInputController.AttackType.AirLight: return "AttackLightAir";
            case PlayerInputController.AttackType.AirHeavy: return "AttackHeavyAir";
            case PlayerInputController.AttackType.Cyclone: return "AttackCyclone";
            default: return string.Empty;
        }
    }

    private void SetBoolIfExists(string parameterName, bool value)
    {
        if (animator == null || !HasParameter(parameterName, AnimatorControllerParameterType.Bool))
            return;

        animator.SetBool(parameterName, value);
    }

    private bool HasTrigger(string triggerName)
    {
        return !string.IsNullOrWhiteSpace(triggerName) && HasParameter(triggerName, AnimatorControllerParameterType.Trigger);
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null)
            return false;

        for (int i = 0; i < animator.parameters.Length; i++)
        {
            AnimatorControllerParameter parameter = animator.parameters[i];
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }

    private HashSet<string> BuildEnemyComponentTypeNameSet()
    {
        HashSet<string> typeNames = new HashSet<string>();

        for (int i = 0; i < enemyComponentTypeNamesToDisable.Count; i++)
        {
            string typeName = enemyComponentTypeNamesToDisable[i];
            if (string.IsNullOrWhiteSpace(typeName))
                continue;

            typeNames.Add(typeName.Trim());
        }

        return typeNames;
    }

    private List<GameObject> CollectEnemyRoots()
    {
        HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();
        List<GameObject> enemyRoots = new List<GameObject>();

        EnemyMovement[] enemyMovements = FindObjectsByType<EnemyMovement>(FindObjectsSortMode.None);
        for (int i = 0; i < enemyMovements.Length; i++)
        {
            EnemyMovement enemyMovement = enemyMovements[i];
            if (enemyMovement == null)
                continue;

            GameObject enemyRoot = enemyMovement.transform.root.gameObject;
            if (uniqueRoots.Add(enemyRoot))
                enemyRoots.Add(enemyRoot);
        }

        EnemyAnimator[] enemyAnimators = FindObjectsByType<EnemyAnimator>(FindObjectsSortMode.None);
        for (int i = 0; i < enemyAnimators.Length; i++)
        {
            EnemyAnimator enemyAnimator = enemyAnimators[i];
            if (enemyAnimator == null)
                continue;

            GameObject enemyRoot = enemyAnimator.transform.root.gameObject;
            if (uniqueRoots.Add(enemyRoot))
                enemyRoots.Add(enemyRoot);
        }

        return enemyRoots;
    }
}
