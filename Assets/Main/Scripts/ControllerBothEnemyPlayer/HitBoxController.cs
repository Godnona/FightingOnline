using System.Collections.Generic;
using UnityEngine;

public class HitBoxController : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private PlayerInputController.AttackType attackType;
    [SerializeField] private int damage = 10;
    [SerializeField] private int rippCost;
    [SerializeField] private int rippDamage;
    [SerializeField] private int rippReward = 5;
    [SerializeField] private int scoreOverride;
    [SerializeField] private float dizzyDuration = 0.2f;
    [SerializeField] private float knockbackForce = 8f;
    [SerializeField] private Vector2 knockbackDirection = Vector2.right;
    [SerializeField] private float knockbackDuration = 0.12f;

    [Header("Camera Shake")]
    [SerializeField] private bool useCustomShake;
    [SerializeField] private float shakePositionStrength = 0.18f;
    [SerializeField] private float shakeRotationStrength = 0.18f;
    [SerializeField] private float shakeFrequency = 25f;
    [SerializeField] private int shakeBounces = 4;

    private readonly HashSet<StatsController> hitTargets = new HashSet<StatsController>();
    private readonly Collider[] overlapResults3D = new Collider[16];
    private readonly Collider2D[] overlapResults2D = new Collider2D[16];

    private PlayerFightingController ownerFighting;
    private StatsController ownerStats;
    private ScoreController ownerScore;
    private Collider hitboxCollider3D;
    private SphereCollider sphereCollider3D;
    private BoxCollider boxCollider3D;
    private CapsuleCollider capsuleCollider3D;
    private CircleCollider2D circleCollider2D;

    public int Damage => damage;
    public int RippCost => Mathf.Max(0, rippCost);
    public int RippDamage => rippDamage;
    public float DizzyDuration => Mathf.Max(0f, dizzyDuration);
    public float KnockbackForce => Mathf.Max(0f, knockbackForce);
    public Vector2 KnockbackDirection => knockbackDirection;
    public float KnockbackDuration => knockbackDuration > 0f ? knockbackDuration : 0.12f;
    public StatsController OwnerStats => ownerStats;
    public PlayerInputController.AttackType AttackType => attackType;
    public bool UseCustomShake => useCustomShake;
    public float ShakePositionStrength => Mathf.Max(0f, shakePositionStrength);
    public float ShakeRotationStrength => Mathf.Max(0f, shakeRotationStrength);
    public float ShakeFrequency => Mathf.Max(0f, shakeFrequency);
    public int ShakeBounces => Mathf.Max(0, shakeBounces);

    private void Awake()
    {
        CacheColliders();
        ConfigureHitboxColliders();
    }

    private void OnEnable()
    {
        hitTargets.Clear();
        CacheColliders();
        ConfigureHitboxColliders();
        ApplyToCurrentOverlaps();
    }

    private void OnDisable()
    {
        hitTargets.Clear();
    }

    public void BindOwner(PlayerFightingController fightingController, StatsController statsController, ScoreController scoreController)
    {
        ownerFighting = fightingController;
        ownerStats = statsController;
        ownerScore = scoreController;
    }

    public void BindOwner(StatsController statsController, ScoreController scoreController)
    {
        ownerStats = statsController;
        ownerScore = scoreController;
    }

    public void Activate()
    {
        hitTargets.Clear();

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            return;
        }

        ApplyToCurrentOverlaps();
    }

    public void Deactivate()
    {
        hitTargets.Clear();

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    public bool TryRegisterTarget(StatsController target)
    {
        if (target == null || target == ownerStats || hitTargets.Contains(target))
            return false;

        hitTargets.Add(target);
        return true;
    }

    public void RewardOwner()
    {
        RewardOwner(null, 0);
    }

    public void RewardOwner(StatsController target)
    {
        RewardOwner(target, damage);
    }

    public void RewardOwner(StatsController target, int damageDealt)
    {
        if (ownerStats != null && rippReward != 0)
            ownerStats.ChangeRipp(rippReward);

        if (ownerScore != null && scoreOverride != 0)
            ownerScore.AddScore(scoreOverride);

        if (!ShouldCountCombo(target))
            return;

        if (ComboManager.Instance != null)
            ComboManager.Instance.AddHit();

        if (CombatInfoDisplay.Instance != null)
            CombatInfoDisplay.Instance.RecordHit(damageDealt);
    }

    private bool ShouldCountCombo(StatsController target)
    {
        if (ownerStats == null || target == null || target == ownerStats)
            return false;

        bool ownerIsPlayer = ownerStats.GetComponent<PlayerController1>() != null;
        if (!ownerIsPlayer)
            return false;

        bool targetIsPlayer = target.GetComponent<PlayerController1>() != null;
        return !targetIsPlayer;
    }

    private void ApplyToCurrentOverlaps()
    {
        if (sphereCollider3D != null)
        {
            ApplyToCurrentOverlaps3D();
            return;
        }

        if (boxCollider3D != null)
        {
            ApplyToCurrentOverlapsBox3D();
            return;
        }

        if (capsuleCollider3D != null)
        {
            ApplyToCurrentOverlapsCapsule3D();
            return;
        }

        if (circleCollider2D != null)
            ApplyToCurrentOverlaps2D();
    }

    private void ApplyToCurrentOverlaps3D()
    {
        Vector3 lossyScale = sphereCollider3D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z));
        float worldRadius = Mathf.Max(0.01f, sphereCollider3D.radius * maxScale);
        Vector3 worldCenter = sphereCollider3D.transform.TransformPoint(sphereCollider3D.center);

        int overlapCount = Physics.OverlapSphereNonAlloc(
            worldCenter,
            worldRadius,
            overlapResults3D,
            ~0,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider other = overlapResults3D[i];
            if (other == null || other == hitboxCollider3D)
                continue;

            HurtBoxCollider hurtBox = other.GetComponent<HurtBoxCollider>();
            if (hurtBox == null)
                hurtBox = other.GetComponentInParent<HurtBoxCollider>();

            if (hurtBox != null)
                hurtBox.ReceiveHitBox(this);
        }
    }

    private void ApplyToCurrentOverlapsBox3D()
    {
        Vector3 lossyScale = boxCollider3D.transform.lossyScale;
        Vector3 absoluteScale = new Vector3(
            Mathf.Abs(lossyScale.x),
            Mathf.Abs(lossyScale.y),
            Mathf.Abs(lossyScale.z));
        Vector3 halfExtents = Vector3.Scale(boxCollider3D.size * 0.5f, absoluteScale);
        Vector3 worldCenter = boxCollider3D.transform.TransformPoint(boxCollider3D.center);

        int overlapCount = Physics.OverlapBoxNonAlloc(
            worldCenter,
            halfExtents,
            overlapResults3D,
            boxCollider3D.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide);

        ProcessOverlapResults3D(overlapCount);
    }

    private void ApplyToCurrentOverlapsCapsule3D()
    {
        Vector3 lossyScale = capsuleCollider3D.transform.lossyScale;
        Vector3 absoluteScale = new Vector3(
            Mathf.Abs(lossyScale.x),
            Mathf.Abs(lossyScale.y),
            Mathf.Abs(lossyScale.z));
        Vector3 worldCenter = capsuleCollider3D.transform.TransformPoint(capsuleCollider3D.center);

        Vector3 worldAxis;
        float heightScale;
        float radiusScale;

        switch (capsuleCollider3D.direction)
        {
            case 0:
                worldAxis = capsuleCollider3D.transform.right;
                heightScale = absoluteScale.x;
                radiusScale = Mathf.Max(absoluteScale.y, absoluteScale.z);
                break;
            case 2:
                worldAxis = capsuleCollider3D.transform.forward;
                heightScale = absoluteScale.z;
                radiusScale = Mathf.Max(absoluteScale.x, absoluteScale.y);
                break;
            default:
                worldAxis = capsuleCollider3D.transform.up;
                heightScale = absoluteScale.y;
                radiusScale = Mathf.Max(absoluteScale.x, absoluteScale.z);
                break;
        }

        float worldRadius = Mathf.Max(0.01f, capsuleCollider3D.radius * radiusScale);
        float worldHeight = Mathf.Max(capsuleCollider3D.height * heightScale, worldRadius * 2f);
        float halfSegmentLength = Mathf.Max(0f, (worldHeight * 0.5f) - worldRadius);
        Vector3 segmentOffset = worldAxis.normalized * halfSegmentLength;
        Vector3 point0 = worldCenter + segmentOffset;
        Vector3 point1 = worldCenter - segmentOffset;

        int overlapCount = Physics.OverlapCapsuleNonAlloc(
            point0,
            point1,
            worldRadius,
            overlapResults3D,
            ~0,
            QueryTriggerInteraction.Collide);

        ProcessOverlapResults3D(overlapCount);
    }

    private void ApplyToCurrentOverlaps2D()
    {
        Vector3 lossyScale = circleCollider2D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        float worldRadius = Mathf.Max(0.01f, circleCollider2D.radius * maxScale);
        Vector2 worldCenter = circleCollider2D.transform.TransformPoint(circleCollider2D.offset);

        int overlapCount = Physics2D.OverlapCircleNonAlloc(
            worldCenter,
            worldRadius,
            overlapResults2D);

        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D other = overlapResults2D[i];
            if (other == null || other == circleCollider2D)
                continue;

            HurtBoxCollider hurtBox = other.GetComponent<HurtBoxCollider>();
            if (hurtBox == null)
                hurtBox = other.GetComponentInParent<HurtBoxCollider>();

            if (hurtBox != null)
                hurtBox.ReceiveHitBox(this);
        }
    }

    private void ProcessOverlapResults3D(int overlapCount)
    {
        for (int i = 0; i < overlapCount; i++)
        {
            Collider other = overlapResults3D[i];
            if (other == null || other == hitboxCollider3D)
                continue;

            HurtBoxCollider hurtBox = other.GetComponent<HurtBoxCollider>();
            if (hurtBox == null)
                hurtBox = other.GetComponentInParent<HurtBoxCollider>();

            if (hurtBox != null)
                hurtBox.ReceiveHitBox(this);
        }
    }

    private void CacheColliders()
    {
        if (hitboxCollider3D == null) hitboxCollider3D = GetComponent<Collider>();
        if (sphereCollider3D == null) sphereCollider3D = GetComponent<SphereCollider>();
        if (boxCollider3D == null) boxCollider3D = GetComponent<BoxCollider>();
        if (capsuleCollider3D == null) capsuleCollider3D = GetComponent<CapsuleCollider>();
        if (circleCollider2D == null) circleCollider2D = GetComponent<CircleCollider2D>();
    }

    private void ConfigureHitboxColliders()
    {
        if (sphereCollider3D != null)
            sphereCollider3D.isTrigger = true;
        else if (hitboxCollider3D != null)
            hitboxCollider3D.isTrigger = true;

        if (circleCollider2D != null)
            circleCollider2D.isTrigger = true;
    }
}
