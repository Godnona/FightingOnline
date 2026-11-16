using UnityEngine;

public class SmashStyleShadow : MonoBehaviour
{
    public Transform character;
    public LayerMask groundMask;

    public float maxDistance = 8f;
    public float maxScale = 1.2f;
    public float minScale = 0.4f;
    public float edgeFadeSpeed = 10f;

    private Renderer shadowRenderer;
    private float currentAlpha = 1f;

    void Awake()
    {
        shadowRenderer = GetComponent<Renderer>();
    }

    void LateUpdate()
    {
        // Center ray
        if (!Physics.Raycast(character.position, Vector3.down, out RaycastHit centerHit, maxDistance, groundMask))
        {
            FadeOut();
            return;
        }

        // Scale by height
        float t = centerHit.distance / maxDistance;
        float scale = Mathf.Lerp(maxScale, minScale, t);

        transform.position = centerHit.point + Vector3.up * 0.02f;
        transform.localScale = new Vector3(scale, scale, 1f);

        // Edge rays
        Vector3 right = character.right * scale * 0.4f;
        Vector3 left  = -right;

        bool rightHit = Physics.Raycast(character.position + right, Vector3.down, maxDistance, groundMask);
        bool leftHit  = Physics.Raycast(character.position + left,  Vector3.down, maxDistance, groundMask);

        // If any edge ray misses → fade shadow
        if (!rightHit || !leftHit)
            FadeOut();
        else
            FadeIn();
    }

    void FadeOut()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, Time.deltaTime * edgeFadeSpeed);
        SetAlpha(currentAlpha);
    }

    void FadeIn()
    {
        currentAlpha = Mathf.MoveTowards(currentAlpha, 1f, Time.deltaTime * edgeFadeSpeed);
        SetAlpha(currentAlpha);
    }

    void SetAlpha(float a)
    {
        Color c = shadowRenderer.material.color;
        c.a = a * 0.4f; // base shadow opacity
        shadowRenderer.material.color = c;
    }
}
