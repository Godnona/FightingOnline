using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerShadowDecal : MonoBehaviour
{
    [Header("Shadow Decal")]
    public Transform decal;
    public LayerMask platformLayer;
    public float maxDistance = 20f;
    [SerializeField] private Transform followTarget;
    [SerializeField] private bool autoResolveDecalFromCharacterName = true;
    [SerializeField] private bool hideWhenNoGround = false;
    [SerializeField] private bool disableCompetingShadows = true;
    [SerializeField, Min(0.01f)] private float projectionDepth = 3f;

    // Cached editor transform data
    private float horizontalOffset;
    private float verticalOffset;
    private Quaternion editorRotation;
    private float editorWorldZ;
    private bool hasCachedPlacement;
    private DecalProjector decalProjector;

    void Awake()
    {
        ResolveDecalReferenceIfNeeded();
        ConfigureProjectionVolume();
        CachePlacement();
    }

    void OnEnable()
    {
        DisableCompetingShadowSources();
    }

    void LateUpdate()
    {
        if (decal == null)
        {
            ResolveDecalReferenceIfNeeded();
            ConfigureProjectionVolume();
            CachePlacement();

            if (decal == null)
                return;
        }

        if (followTarget == null)
            followTarget = transform;

        Vector3 origin = followTarget.position + Vector3.up * 0.1f;

        RaycastHit hit;
        bool hasSurface = Physics.Raycast(
            origin,
            Vector3.down,
            out hit,
            maxDistance,
            platformLayer,
            QueryTriggerInteraction.Ignore
        );

        if (hasSurface)
        {
            if (!hasCachedPlacement)
                CachePlacement();

            // Always follow character on X. Only update Y when we find valid ground.
            Vector3 targetPos = decal.position;
            targetPos.x = followTarget.position.x + horizontalOffset;
            targetPos.y = hit.point.y + verticalOffset;
            targetPos.z = editorWorldZ;

            decal.position = targetPos;
            decal.rotation = editorRotation;

            if (!decal.gameObject.activeSelf)
                decal.gameObject.SetActive(true);
        }
        else
        {
            Vector3 targetPos = decal.position;
            targetPos.x = followTarget.position.x + horizontalOffset;
            targetPos.z = editorWorldZ;
            decal.position = targetPos;
            decal.rotation = editorRotation;

            if (hideWhenNoGround && decal.gameObject.activeSelf)
                decal.gameObject.SetActive(false);
        }
    }

    private void ResolveDecalReferenceIfNeeded()
    {
        if (!autoResolveDecalFromCharacterName)
            return;

        if (decal != null && !decal.name.Contains("Spawn_Position"))
            return;

        string expectedName = $"Decal Projector_SHADOW_{transform.root.name}";
        Transform[] sceneTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform candidate = sceneTransforms[i];
            if (candidate.name != expectedName)
                continue;

            decal = candidate;
            return;
        }
    }

    private void DisableCompetingShadowSources()
    {
        if (!disableCompetingShadows)
            return;

        SpriteRenderer[] spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].shadowCastingMode = ShadowCastingMode.Off;

        ShadowCaster2D[] shadowCasters = GetComponentsInChildren<ShadowCaster2D>(true);
        for (int i = 0; i < shadowCasters.Length; i++)
            shadowCasters[i].enabled = false;
    }

    private void ConfigureProjectionVolume()
    {
        if (decal == null)
            return;

        if (decalProjector == null || decalProjector.transform != decal)
            decalProjector = decal.GetComponent<DecalProjector>();

        if (decalProjector == null)
            return;

        float depth = Mathf.Max(0.01f, projectionDepth);

        Vector3 size = decalProjector.size;
        size.z = depth;
        decalProjector.size = size;

        // Start the projection at the decal transform and extend only downward.
        // A shallow volume prevents the decal from passing through a platform
        // and appearing again on the ground below it.
        Vector3 pivot = decalProjector.pivot;
        pivot.z = depth * 0.5f;
        decalProjector.pivot = pivot;
    }

    private void CachePlacement()
    {
        if (decal == null)
            return;

        Transform target = followTarget != null ? followTarget : transform;
        horizontalOffset = decal.position.x - target.position.x;
        verticalOffset = decal.position.y;
        editorRotation = decal.rotation;
        editorWorldZ = decal.position.z;
        hasCachedPlacement = true;
    }

    //private void OnDrawGizmosSelected()
    //{
    //    Vector3 origin = transform.position + Vector3.up * 0.1f;

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawLine(origin, origin + Vector3.down * maxDistance);
    //}
}
