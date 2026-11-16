using UnityEngine;
using Unity.Cinemachine;
using Unity.VisualScripting;

[DisallowMultipleComponent]
public class ZoomOnPlatfrom : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Player transform. If left empty, the script will try to find the object tagged Player.")]
    public Transform player;

    [Tooltip("Optional player collider. If left empty, the script will try to get it from the player.")]
    public Collider2D playerCollider;

    [Tooltip("Optional Cinemachine Camera. If left empty, the script will try to find one in the scene.")]
    public CinemachineCamera cinemachineCamera;

    [Tooltip("Optional Cinemachine Follow component. If left empty, the script will try to get it from the Cinemachine Camera.")]
    public CinemachineFollow cinemachineFollow;

    [Header("Layer Masks")]
    [Tooltip("Layer(s) that count as Platform. When the player is standing on one of these layers, Z zoom is applied.")]
    public LayerMask platformLayer;

    [Tooltip("Layer(s) that count as Ground. When the player is standing on one of these layers, zoom returns to normal.")]
    public LayerMask groundLayer;

    [Header("Zoom On Platform")]
    [Tooltip("FollowOffset.z while the player is on Platform.")]
    public float platformFollowZ = -130f;

    [Tooltip("FollowOffset.z while the player is on Ground or not standing on Platform.")]
    public float groundFollowZ = -170.8f;

    [Tooltip("How fast FollowOffset.z moves toward the target value.")]
    public float zoomSmoothSpeed = 8f;

    [Tooltip("How far below the player's feet to check for a surface.")]
    public float groundCheckDistance = 0.15f;

    [Tooltip("Small vertical offset to avoid missing the surface because of collider shape.")]
    public float feetOffset = 0.05f;

    [Tooltip("If true, movement is based on unscaled time.")]
    public bool useUnscaledTime = false;

    [Header("Debug")]
    [SerializeField] private bool isOnPlatform;
    [SerializeField] private bool isOnGround;
    [SerializeField] private float currentTargetZ;

    private void Awake()
    {
        ResolveReferences();
        currentTargetZ = groundFollowZ;
    }

    private void OnEnable()
    {
        ResolveReferences();
        SnapToCurrentTarget();
    }

    private void LateUpdate()
    {
        ResolveReferences();

        if (cinemachineFollow == null)
            return;

        bool onPlatform = IsStandingOnMask(platformLayer);
        bool onGround = IsStandingOnMask(groundLayer);

        isOnPlatform = onPlatform;
        isOnGround = onGround;

        float targetZ = (player.transform.position.y >= 470.0f) ? platformFollowZ : groundFollowZ;
        currentTargetZ = targetZ;

        Vector3 followOffset = cinemachineFollow.FollowOffset;
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        followOffset.z = Mathf.MoveTowards(followOffset.z, targetZ, zoomSmoothSpeed * dt);
        cinemachineFollow.FollowOffset = followOffset;
    }

    [ContextMenu("Snap To Current Target")]
    public void SnapToCurrentTarget()
    {
        if (cinemachineFollow == null)
            return;

        bool onPlatform = IsStandingOnMask(platformLayer);
        float targetZ = onPlatform ? platformFollowZ : groundFollowZ;

        Vector3 followOffset = cinemachineFollow.FollowOffset;
        followOffset.z = targetZ;
        cinemachineFollow.FollowOffset = followOffset;

        isOnPlatform = onPlatform;
        isOnGround = !onPlatform && IsStandingOnMask(groundLayer);
        currentTargetZ = targetZ;
    }

    private void ResolveReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindWithTag("Player");
            if (playerObject != null)
                player = playerObject.transform;
        }

        if (playerCollider == null && player != null)
        {
            playerCollider = player.GetComponent<Collider2D>();
        }

        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        if (cinemachineFollow == null)
        {
            if (cinemachineCamera != null)
            {
                cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
            }

            if (cinemachineFollow == null)
            {
                cinemachineFollow = GetComponent<CinemachineFollow>();
            }
        }
    }

    private bool IsStandingOnMask(LayerMask mask)
    {
        if (player == null || playerCollider == null)
            return false;

        if (mask.value == 0)
            return false;

        Bounds bounds = playerCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + feetOffset);
        float distance = Mathf.Max(groundCheckDistance, 0.01f);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, distance, mask);
        return hit.collider != null;
    }
}
