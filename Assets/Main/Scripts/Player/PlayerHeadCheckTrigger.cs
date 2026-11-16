using System.Collections.Generic;
using UnityEngine;

public class PlayerHeadCheckTrigger : MonoBehaviour
{
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private LayerMask platformLayer = -1;
    [SerializeField] private float enemyJumpPushBackDistance = 2f;

    private readonly HashSet<EnemyMovement> pushedJumpingEnemies = new HashSet<EnemyMovement>();

    private void Reset()
    {
        playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryEnablePassThrough(other);
        TryPushBackFromJumpingEnemy(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryEnablePassThrough(other);
        TryPushBackFromJumpingEnemy(other);
    }

    private void OnTriggerExit(Collider other)
    {
        EnemyMovement enemyMovement = other == null ? null : other.GetComponentInParent<EnemyMovement>();
        if (enemyMovement == null)
            return;

        pushedJumpingEnemies.Remove(enemyMovement);
    }

    private void TryEnablePassThrough(Collider other)
    {
        if (playerMovement == null || other == null)
            return;

        if (!playerMovement.IsMovingUpward)
            return;

        if (!IsInLayerMask(other.gameObject.layer, platformLayer))
            return;

        playerMovement.SetBodyTrigger(true);
    }

    private void TryPushBackFromJumpingEnemy(Collider other)
    {
        if (playerMovement == null || other == null)
            return;

        EnemyMovement enemyMovement = other.GetComponentInParent<EnemyMovement>();
        if (enemyMovement == null || !enemyMovement.IsJumping)
            return;

        if (pushedJumpingEnemies.Contains(enemyMovement))
            return;

        float pushDirection = ResolvePushDirection(enemyMovement.transform.position.x);
        playerMovement.MoveHorizontally(pushDirection * enemyJumpPushBackDistance);
        pushedJumpingEnemies.Add(enemyMovement);
    }

    private float ResolvePushDirection(float enemyXPosition)
    {
        if (playerMovement == null)
            return enemyXPosition >= transform.position.x ? -1f : 1f;

        float horizontalDelta = transform.position.x - enemyXPosition;
        if (Mathf.Abs(horizontalDelta) > 0.01f)
            return Mathf.Sign(horizontalDelta);

        return playerMovement.FacingRight ? -1f : 1f;
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
