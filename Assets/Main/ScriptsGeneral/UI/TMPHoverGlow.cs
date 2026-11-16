using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TMPHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float hoverIntensity = 2f;
    public float speed = 8f;

    private TMP_Text text;
    private Material mat;
    private float targetIntensity;
    private float currentIntensity;

    void Awake()
    {
        text = GetComponent<TMP_Text>();

        mat = new Material(text.fontMaterial);
        text.fontMaterial = mat;

        currentIntensity = 0;
        targetIntensity = 0;
    }

    void Update()
    {
        currentIntensity = Mathf.Lerp(
            currentIntensity,
            targetIntensity,
            Time.unscaledDeltaTime * speed
        );

        mat.SetFloat("_GlowIntensity", currentIntensity);

        // FORCE TMP TO REFRESH
        text.SetMaterialDirty();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetIntensity = hoverIntensity;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetIntensity = 0f;
    }
}