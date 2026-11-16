using UnityEngine;

public class PlayerHurtBoxCollider : HurtBoxCollider
{
    [Header("Knockback VFX")]
    [SerializeField] private GameObject knockbackVfxPrefab;
    [SerializeField] private Transform knockbackVfxTransform;

    private PlayerInputController playerInput;
    private PlayerMovement playerMovement;
    private PlayerAnimator playerAnimator;
    private PlayerFightingController playerFighting;
    private bool cachedCanMove;
    private bool cachedPlayerInputEnabled;

    protected override bool CanReceiveHitBox(HitBoxController hitBox)
    {
        if (!base.CanReceiveHitBox(hitBox))
            return false;

        return IsEnemyStats(hitBox.OwnerStats);
    }

    protected override void SpawnKnockbackVfx(HitBoxController hitBox)
    {
        if (knockbackVfxPrefab == null)
            return;

        Transform spawnPoint = knockbackVfxTransform != null ? knockbackVfxTransform : transform;
        MyPoolManager poolManager = MyPoolManager.Instance;
        if (poolManager == null)
        {
            Debug.LogWarning("MyPoolManager.Instance is null, cannot spawn knockback VFX from pool.", this);
            return;
        }

        GameObject spawnedVfx = poolManager.Get(knockbackVfxPrefab);
        if (spawnedVfx == null)
            return;

        Transform spawnedTransform = spawnedVfx.transform;
        spawnedTransform.SetParent(spawnPoint, false);
        spawnedTransform.localPosition = Vector3.zero;
        spawnedTransform.localRotation = Quaternion.identity;
        spawnedTransform.localScale = knockbackVfxPrefab.transform.localScale;
    }

    protected override void CacheSpecificReferences()
    {
        playerInput = GetComponentInParent<PlayerInputController>();
        playerMovement = GetComponentInParent<PlayerMovement>();
        playerAnimator = GetComponentInParent<PlayerAnimator>();
        playerFighting = GetComponentInParent<PlayerFightingController>();
    }

    protected override void OnPrepareDeathFlight()
    {
        if (playerMovement != null)
            playerMovement.SetBodyTrigger(true);
    }

    protected override void OnRespawnAfterDeathFlight()
    {
        if (playerMovement != null)
        {
            playerMovement.Respawn();
            return;
        }

        base.OnRespawnAfterDeathFlight();
    }

    protected override void OnLockControls()
    {
        bool wasAttacking = playerFighting != null && playerFighting.IsAttacking;

        if (playerFighting != null)
            playerFighting.SetAttackLocked(true);

        if (playerInput != null)
        {
            cachedPlayerInputEnabled = playerInput.enabled;
            playerInput.ConsumeAttack();
            playerInput.enabled = false;
        }

        if (playerMovement != null)
        {
            cachedCanMove = playerMovement.CanMove || wasAttacking;
            playerMovement.CanMove = false;
        }
    }

    protected override void OnUnlockControls()
    {
        if (playerMovement != null)
            playerMovement.CanMove = cachedCanMove;

        if (playerInput != null)
            playerInput.enabled = cachedPlayerInputEnabled;

        if (playerFighting != null)
            playerFighting.SetAttackLocked(false);
    }

    protected override bool ApplySpecificKnockback(Vector3 knockbackVelocity, float knockbackDuration)
    {
        if (playerMovement == null)
            return false;

        playerMovement.ApplyKnockback(knockbackVelocity, knockbackDuration);
        return true;
    }

    protected override bool IsSpecificDeathAnimationFinished(string stateName)
    {
        if (playerAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return true;

        return playerAnimator.IsStateFinished(stateName);
    }
}
