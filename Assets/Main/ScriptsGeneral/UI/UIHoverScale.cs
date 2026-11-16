using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Scale Settings")]
    public float hoverScaleMultiplier = 1.1f;
    public float scaleSpeed = 10f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Awake()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * scaleSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScaleMultiplier;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }
}