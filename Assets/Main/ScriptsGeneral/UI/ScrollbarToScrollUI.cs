using UnityEngine;
using UnityEngine.UI;

public class ScrollbarToScrollUI : MonoBehaviour
{
    [Header("References")]
    public Scrollbar scrollbar;
    public RectTransform content;

    [Header("Scroll Settings")]
    public float scrollRange = 500f; // Maximum scroll offset

    private Vector2 initialPos;

    private void Start()
    {
        if (content != null)
        {
            // Record initial position of content
            initialPos = content.anchoredPosition;
        }

        if (scrollbar != null)
        {
            scrollbar.onValueChanged.AddListener(OnScrollChanged);

            // Initialize content to current scrollbar value without snapping
            OnScrollChanged(scrollbar.value);
        }
    }

    private void OnDestroy()
    {
        if (scrollbar != null)
        {
            scrollbar.onValueChanged.RemoveListener(OnScrollChanged);
        }
    }

    private void OnScrollChanged(float value)
    {
        if (content == null) return;

        Vector2 pos = content.anchoredPosition;

        // Scroll moves opposite direction
        pos.y = initialPos.y + value * scrollRange;

        content.anchoredPosition = pos;
    }
}
