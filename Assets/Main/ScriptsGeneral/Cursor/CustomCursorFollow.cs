using UnityEngine;

public class CustomCursorFollow : MonoBehaviour
{
    [SerializeField] Vector2 offset;
    RectTransform rectTransform;
    Canvas canvas;

    [SerializeField] float smoothSpeed = 15f;
    [SerializeField] float moveThreshold = 0.1f;
    Vector2 lastPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        lastPos = rectTransform.localPosition;

        // Tắt raycastTarget của ảnh chuột để không chặn bấm nút UI phía sau
        var graphics = GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
        foreach (var g in graphics)
        {
            g.raycastTarget = false;
        }

        // Tắt GraphicRaycaster trên Canvas chuột nếu có để không bắt tương tác click
        if (canvas != null)
        {
            var raycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }
        }
    }

    void LateUpdate()
    {
        if (canvas == null) return;

        Vector2 screenPos = Input.mousePosition;
        Vector2 targetPos;
        
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : canvas.worldCamera;

        // Tự động tìm parent để tính tọa độ chính xác nhất
        RectTransform parentRect = rectTransform.parent as RectTransform;
        if (parentRect == null) parentRect = canvas.transform as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            screenPos,
            cam,
            out targetPos
        );

        Vector3 desiredPos = targetPos + offset;

        // Nếu game bị Pause (timeScale = 0), Snap tức thời để tránh giật lag/frozen
        if (Time.timeScale == 0f)
        {
            rectTransform.localPosition = desiredPos;
        }
        // Lúc chơi bình thường, chạy Lerp mượt mà
        else if (Vector2.Distance(rectTransform.localPosition, desiredPos) > moveThreshold)
        {
            rectTransform.localPosition = Vector2.Lerp(rectTransform.localPosition, desiredPos, Time.unscaledDeltaTime * smoothSpeed);
        }
        else
        {
            // Snap to avoid tiny creeping
            rectTransform.localPosition = desiredPos;
        }
    }
}