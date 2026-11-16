using UnityEngine;

public class EnemyHurtBoxCollider : HurtBoxCollider
{
    [Header("Knockback VFX")]
    [SerializeField] private GameObject knockbackVfxPrefab;
    [SerializeField] private Transform knockbackVfxTransform;

    private EnemyMovement enemyMovement;
    private EnemyAnimator enemyAnimator;
    private bool disabledNavigationForDeathFlight;

    protected override bool CanReceiveHitBox(HitBoxController hitBox)
    {
        if (!base.CanReceiveHitBox(hitBox))
            return false;

        return IsPlayerStats(hitBox.OwnerStats);
    }

    protected override void SpawnKnockbackVfx(HitBoxController hitBox)
    {
        SpawnAttachedVfx(knockbackVfxPrefab, knockbackVfxTransform);
    }

    protected override void CacheSpecificReferences()
    {
        enemyMovement = GetComponentInParent<EnemyMovement>();
        enemyAnimator = GetComponentInParent<EnemyAnimator>();
    }

    protected override void OnPrepareDeathFlight()
    {
        if (enemyMovement == null)
            return;

        enemyMovement.SetNavigationActive(false);
        disabledNavigationForDeathFlight = true;
    }

    protected override void OnRespawnAfterDeathFlight()
    {
        base.OnRespawnAfterDeathFlight();
        RestoreNavigationAfterRespawn();
    }

    protected override void OnLockControls()
    {
        if (enemyMovement != null)
            enemyMovement.SetMovementLocked(true);

        if (enemyAnimator != null)
            enemyAnimator.SetAttackLocked(true);
    }

    protected override void OnUnlockControls()
    {
        if (enemyMovement != null)
            enemyMovement.SetMovementLocked(false);

        if (enemyAnimator != null)
            enemyAnimator.SetAttackLocked(false);
    }

    protected override void OnSpecificDisable()
    {
        RestoreNavigationAfterRespawn();
    }

    protected override bool ApplySpecificKnockback(Vector3 knockbackVelocity, float knockbackDuration)
    {
        if (enemyMovement == null)
            return false;

        enemyMovement.ApplyKnockback(knockbackVelocity, knockbackDuration);
        return true;
    }

    protected override bool IsSpecificDeathAnimationFinished(string stateName)
    {
        if (enemyAnimator == null || string.IsNullOrWhiteSpace(stateName))
            return true;

        return enemyAnimator.IsStateFinished(stateName);
    }

    private void RestoreNavigationAfterRespawn()
    {
        if (!disabledNavigationForDeathFlight || enemyMovement == null)
            return;

        enemyMovement.SetNavigationActive(true);
        disabledNavigationForDeathFlight = false;
    }
}
