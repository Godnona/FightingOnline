using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIHoverGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public float glowIntensity = 2f;

    private Material mat;

    void Awake()
    {
        Image img = GetComponent<Image>();
        mat = new Material(img.material);
        img.material = mat;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mat.SetFloat("_GlowIntensity", glowIntensity);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mat.SetFloat("_GlowIntensity", 0f);
    }
}