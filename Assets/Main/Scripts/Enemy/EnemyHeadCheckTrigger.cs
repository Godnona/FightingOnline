using System.Collections.Generic;
using UnityEngine;

public class EnemyHeadCheckTrigger : MonoBehaviour
{
    [Header("Slide")]
    [SerializeField] private Collider headTrigger;
    [SerializeField] private float slideDistance = 3f;
    [SerializeField] private float initialSlideSpeed = 6f;
    [SerializeField] private float slideAcceleration = 45f;
    [SerializeField] private float maxSlideSpeed = 30f;
    [SerializeField] private float centerDeadZone = 0.05f;

    private readonly Dictionary<PlayerMovement, SlideState> activeSlides = new Dictionary<PlayerMovement, SlideState>();

    private sealed class SlideState
    {
        public Rigidbody Rigidbody;
        public float Direction;
        public float Speed;
        public float RemainingDistance;
        public bool Completed;
    }

    private void Reset()
    {
        headTrigger = GetComponent<Collider>();
    }

    private void Awake()
    {
        if (headTrigger == null)
            headTrigger = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryBeginSlide(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryBeginSlide(other);
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerMovement playerMovement = GetPlayerMovement(other);
        if (playerMovement == null)
            return;

        activeSlides.Remove(playerMovement);
    }

    private void OnDisable()
    {
        activeSlides.Clear();
    }

    private void FixedUpdate()
    {
        if (activeSlides.Count == 0)
            return;

        List<PlayerMovement> completedPlayers = null;

        foreach (KeyValuePair<PlayerMovement, SlideState> pair in activeSlides)
        {
            SlideState state = pair.Value;
            if (state == null || state.Completed || state.Rigidbody == null)
            {
                completedPlayers ??= new List<PlayerMovement>();
                completedPlayers.Add(pair.Key);
                continue;
            }

            state.Speed = Mathf.Min(maxSlideSpeed, state.Speed + slideAcceleration * Time.fixedDeltaTime);
            float delta = Mathf.Min(state.RemainingDistance, state.Speed * Time.fixedDeltaTime);
            if (delta <= 0f)
            {
                state.Completed = true;
                completedPlayers ??= new List<PlayerMovement>();
                completedPlayers.Add(pair.Key);
                continue;
            }

            Vector3 nextPosition = state.Rigidbody.position + Vector3.right * (state.Direction * delta);
            nextPosition.z = 0f;
            state.Rigidbody.MovePosition(nextPosition);

            state.RemainingDistance -= delta;
            if (state.RemainingDistance <= 0.0001f)
            {
                state.Completed = true;
                completedPlayers ??= new List<PlayerMovement>();
                completedPlayers.Add(pair.Key);
            }
        }

        if (completedPlayers == null)
            return;

        for (int i = 0; i < completedPlayers.Count; i++)
            activeSlides.Remove(completedPlayers[i]);
    }

    private void TryBeginSlide(Collider other)
    {
        PlayerMovement playerMovement = GetPlayerMovement(other);
        if (playerMovement == null || activeSlides.ContainsKey(playerMovement))
            return;

        Rigidbody playerRigidbody = playerMovement.GetComponent<Rigidbody>();
        if (playerRigidbody == null)
            return;

        float direction = ResolveSlideDirection(playerMovement, playerRigidbody, other);
        if (Mathf.Approximately(direction, 0f))
            return;

        activeSlides[playerMovement] = new SlideState
        {
            Rigidbody = playerRigidbody,
            Direction = direction,
            Speed = Mathf.Max(0f, initialSlideSpeed),
            RemainingDistance = Mathf.Max(0f, slideDistance)
        };
    }

    private float ResolveSlideDirection(PlayerMovement playerMovement, Rigidbody playerRigidbody, Collider playerCollider)
    {
        PlayerInputController input = playerMovement.GetComponent<PlayerInputController>();
        if (input != null && Mathf.Abs(input.MoveX) > 0.01f)
            return Mathf.Sign(input.MoveX);

        float velocityX = playerRigidbody.linearVelocity.x;
        if (Mathf.Abs(velocityX) > 0.01f)
            return Mathf.Sign(velocityX);

        Bounds triggerBounds = headTrigger != null ? headTrigger.bounds : new Bounds(transform.position, Vector3.zero);
        float offsetFromCenter = playerCollider.bounds.center.x - triggerBounds.center.x;
        if (Mathf.Abs(offsetFromCenter) > centerDeadZone)
            return Mathf.Sign(offsetFromCenter);

        return playerMovement.FacingRight ? 1f : -1f;
    }

    private static PlayerMovement GetPlayerMovement(Collider other)
    {
        return other == null ? null : other.GetComponentInParent<PlayerMovement>();
    }
}
