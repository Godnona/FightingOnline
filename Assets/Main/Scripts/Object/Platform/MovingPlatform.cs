using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class MovingPlatform : MonoBehaviour
{
    [SerializeField]
    private Vector3[] Positions;

    [SerializeField]
    private float DockDuration = 2f;

    [SerializeField]
    private float MoveSpeed = 0.01f;

    [SerializeField]
    private NavMeshSurface Surface;

    private List<NavMeshAgent> AgentsOnPlatform = new List<NavMeshAgent>();
    private Vector3 startPosition;

    private void Start()
    {
        if (Surface == null)
        {
            Surface = GetComponent<NavMeshSurface>();
        }

        startPosition = transform.position;
        StartCoroutine(MovePlatform());
    }

    private void OnTriggerEnter(Collider other)
    {
        AddAgent(other.GetComponentInParent<NavMeshAgent>());
    }

    private void OnTriggerExit(Collider other)
    {
        RemoveAgent(other.GetComponentInParent<NavMeshAgent>());
    }

    private void OnCollisionStay(Collision collision)
    {
        AddAgent(collision.collider.GetComponentInParent<NavMeshAgent>());
    }

    private void OnCollisionExit(Collision collision)
    {
        RemoveAgent(collision.collider.GetComponentInParent<NavMeshAgent>());
    }

    private void AddAgent(NavMeshAgent agent)
    {
        if (agent != null && !AgentsOnPlatform.Contains(agent))
        {
            AgentsOnPlatform.Add(agent);
        }
    }

    private void RemoveAgent(NavMeshAgent agent)
    {
        if (agent != null)
        {
            AgentsOnPlatform.Remove(agent);
        }
    }

    private void MoveAgents(Vector3 platformMovement)
    {
        for (int i = AgentsOnPlatform.Count - 1; i >= 0; i--)
        {
            NavMeshAgent agent = AgentsOnPlatform[i];

            if (agent == null)
            {
                AgentsOnPlatform.RemoveAt(i);
                continue;
            }

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(platformMovement);
            }
        }
    }

    private IEnumerator MovePlatform()
    {
        if (Positions == null || Positions.Length == 0)
        {
            yield break;
        }

        if (MoveSpeed <= 0f)
        {
            Debug.LogError("MovingPlatform requires MoveSpeed to be greater than 0.", this);
            yield break;
        }

        // Index 0 is the platform's starting position. The serialized Positions
        // are offsets from that position, so the first offset starts at index 1.
        int positionIndex = 0;
        WaitForSeconds wait = new WaitForSeconds(DockDuration);

        // Keep the platform at its starting point before the first movement.
        yield return wait;

        while (true)
        {
            positionIndex++;

            if (positionIndex > Positions.Length)
            {
                positionIndex = 0;
            }

            Vector3 targetPosition = positionIndex == 0
                ? startPosition
                : startPosition + Positions[positionIndex - 1];

            const float arrivalDistance = 0.001f;

            while ((transform.position - targetPosition).sqrMagnitude
                   > arrivalDistance * arrivalDistance)
            {
                Vector3 previousPosition = transform.position;
                transform.position = Vector3.MoveTowards(
                    previousPosition,
                    targetPosition,
                    MoveSpeed * Time.deltaTime);
                Vector3 platformMovement = transform.position - previousPosition;

                MoveAgents(platformMovement);

                yield return null;
            }

            // A transformed FBX hierarchy can introduce a tiny world-position
            // rounding error. Snap to the target so the next waypoint can start.
            Vector3 finalMovement = targetPosition - transform.position;
            transform.position = targetPosition;
            MoveAgents(finalMovement);

            yield return wait;
        }
    }
}
