using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrainCollider : MonoBehaviour
{
    [SerializeField] private GameEvent gameEvent;

    private void Awake()
    {
        CacheGameEvent();
    }

    private void Reset()
    {
        Collider trainCollider = GetComponent<Collider>();
        if (trainCollider != null)
            trainCollider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null)
            return;

        StatsController stats = other.GetComponentInParent<StatsController>();
        if (stats == null || stats.IsDead)
            return;

        PlayerMovement playerMovement = stats.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            CacheGameEvent();
            if (gameEvent != null)
                gameEvent.NotifyPlayerHitByTrain();

            playerMovement.FaceRight();
            KnockFighterOffScreen(stats);
            return;
        }

        EnemyMovement enemyMovement = stats.GetComponent<EnemyMovement>();
        if (enemyMovement == null)
            return;

        // Trigger callbacks still work with a kinematic enemy Rigidbody as long
        // as at least one object in the pair has a Rigidbody.
        enemyMovement.FaceRight();
        KnockFighterOffScreen(stats);
    }

    private static void KnockFighterOffScreen(StatsController stats)
    {
        if (stats == null)
            return;

        HurtBoxCollider hurtBox = stats.GetComponentInChildren<HurtBoxCollider>(true);
        if (hurtBox != null)
        {
            hurtBox.ReceiveFatalEnvironmentHit(1f, false);
            return;
        }

        // Keep the lethal damage behavior even if a fighter prefab has no hurt box.
        if (stats.CurrentHealth > 0)
            stats.TakeDamage(stats.CurrentHealth);
    }

    private void CacheGameEvent()
    {
        if (gameEvent == null)
            gameEvent = FindFirstObjectByType<GameEvent>();
    }
}
