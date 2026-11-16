using UnityEngine;

namespace Ripp.UI
{
    /// <summary>
    /// Responsive World Space Canvas Scaler.
    /// Adjusts sizeDelta (Width/Height) based on camera frustum and a fixed scale.
    /// This allows child elements with proper Anchors to stay on screen.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class WorldSpaceCanvasScaler : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Camera targetCamera;
        
        [Tooltip("Ensure this matches the Scale you designed your UI with (e.g., 0.022).")]
        public float fixedScale = 0.022f;

        [Tooltip("Distance from camera (Perspective only).")]
        public float distance = 90f;

        private RectTransform rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (targetCamera == null) targetCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (targetCamera == null || rectTransform == null) return;

            // Ensure Canvas is always centered in front of the Camera
            transform.position = targetCamera.transform.position + targetCamera.transform.forward * distance;
            transform.rotation = targetCamera.transform.rotation;

            float frustumHeight;
            float frustumWidth;

            // 1. Precise frustum calculation
            if (targetCamera.orthographic)
            {
                frustumHeight = targetCamera.orthographicSize * 2.0f;
                frustumWidth = frustumHeight * targetCamera.aspect;
            }
            else
            {
                frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                frustumWidth = frustumHeight * targetCamera.aspect;
            }

            // 2. Convert world dimensions to pixel dimensions
            float targetWidth = frustumWidth / fixedScale;
            float targetHeight = frustumHeight / fixedScale;

            // 3. Update sizeDelta only if changed to avoid unnecessary layout dirtying
            if (Mathf.Abs(rectTransform.sizeDelta.x - targetWidth) > 0.01f || 
                Mathf.Abs(rectTransform.sizeDelta.y - targetHeight) > 0.01f)
            {
                rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
            }
            
            // 4. Enforce the fixed scale
            transform.localScale = new Vector3(fixedScale, fixedScale, 1f);
        }
    }
}
