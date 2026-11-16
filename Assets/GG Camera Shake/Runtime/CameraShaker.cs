using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CameraShake
{
    /// <summary>
    /// Camera shaker component registeres new shakes, holds a list of active shakes, and applies them to the camera additively.
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public static CameraShaker Instance;
        public static CameraShakePresets Presets;

        readonly List<ICameraShake> activeShakes = new List<ICameraShake>();

        [Tooltip("Transform which will be affected by the shakes.\n\nCameraShaker will set this transform's local position and rotation.")]
        [SerializeField]
        Transform cameraTransform;

        [Tooltip("Scales the strength of all shakes.")]
        [Range(0, 1)]
        [SerializeField]
        public float StrengthMultiplier = 1;

        public CameraShakePresets ShakePresets;

        [Tooltip("If true, CameraShaker will directly set the transform's localPosition. If false, another script can read the offsets and apply them manually.")]
        public bool applyToTransform = true;

        [HideInInspector] public Vector3 currentShakePositionOffset;
        [HideInInspector] public Vector3 currentShakeRotationOffset;

        Camera attachedCamera;
        Vector3 lastAppliedPositionOffset;
        Quaternion lastAppliedRotationOffset = Quaternion.identity;
        bool hasAppliedOffsets;
        Vector3 preRenderBaseLocalPosition;
        Quaternion preRenderBaseLocalRotation = Quaternion.identity;
        bool restoreAfterRender;

        /// <summary>
        /// Adds a shake to the list of active shakes.
        /// </summary>
        public static void Shake(ICameraShake shake)
        {
            if (IsInstanceNull()) return;
            Instance.RegisterShake(shake);
        }

        /// <summary>
        /// Adds a shake to the list of active shakes.
        /// </summary>
        public void RegisterShake(ICameraShake shake)
        {
            shake.Initialize(cameraTransform.position, cameraTransform.rotation);
            activeShakes.Add(shake);
        }

        /// <summary>
        /// Sets the transform which will be affected by the shakes.
        /// </summary>
        public void SetCameraTransform(Transform cameraTransform)
        {
            this.cameraTransform = cameraTransform;
            ResetAppliedOffsets();
        }

        private void Awake()
        {
            Instance = this;
            ShakePresets = new CameraShakePresets(this);
            Presets = ShakePresets;

            if (cameraTransform == null)
            {
                cameraTransform = transform;
            }

            TryGetComponent(out attachedCamera);
        }

        private void Update()
        {
            if (cameraTransform == null)
            {
                return;
            }

            Displacement cameraDisplacement = Displacement.Zero;
            for (int i = activeShakes.Count - 1; i >= 0; i--)
            {
                if (activeShakes[i].IsFinished)
                {
                    activeShakes.RemoveAt(i);
                }
                else
                {
                    activeShakes[i].Update(Time.deltaTime, cameraTransform.position, cameraTransform.rotation);
                    cameraDisplacement += activeShakes[i].CurrentDisplacement;
                }
            }

            currentShakePositionOffset = StrengthMultiplier * cameraDisplacement.position;
            currentShakeRotationOffset = StrengthMultiplier * cameraDisplacement.eulerAngles;

            if (!applyToTransform)
            {
                return;
            }

            if (attachedCamera == null)
            {
                ApplyShakeToTrackedTransform(useCurrentTransformAsBase: false);
            }
        }

        private void OnEnable()
        {
            Camera.onPreCull += HandleBuiltInPreCull;
            Camera.onPostRender += HandleBuiltInPostRender;
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= HandleBuiltInPreCull;
            Camera.onPostRender -= HandleBuiltInPostRender;
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
            restoreAfterRender = false;
            ResetAppliedOffsets();
        }

        private void HandleBuiltInPreCull(Camera cam)
        {
            BeginCameraRender(cam);
        }

        private void HandleBuiltInPostRender(Camera cam)
        {
            EndCameraRender(cam);
        }

        private void HandleBeginCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            BeginCameraRender(cam);
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera cam)
        {
            EndCameraRender(cam);
        }

        private void BeginCameraRender(Camera cam)
        {
            if (!applyToTransform || attachedCamera == null || cam != attachedCamera || cameraTransform == null || restoreAfterRender)
            {
                return;
            }

            ApplyShakeToTrackedTransform(useCurrentTransformAsBase: true);
        }

        private void EndCameraRender(Camera cam)
        {
            if (cam != attachedCamera || !restoreAfterRender || cameraTransform == null)
            {
                return;
            }

            cameraTransform.localPosition = preRenderBaseLocalPosition;
            cameraTransform.localRotation = preRenderBaseLocalRotation;
            restoreAfterRender = false;
            ResetAppliedOffsets();
        }

        private void ApplyShakeToTrackedTransform(bool useCurrentTransformAsBase)
        {
            Vector3 baseLocalPosition = cameraTransform.localPosition;
            Quaternion baseLocalRotation = cameraTransform.localRotation;

            if (!useCurrentTransformAsBase && hasAppliedOffsets)
            {
                baseLocalPosition -= lastAppliedPositionOffset;
                baseLocalRotation *= Quaternion.Inverse(lastAppliedRotationOffset);
            }

            if (useCurrentTransformAsBase)
            {
                preRenderBaseLocalPosition = baseLocalPosition;
                preRenderBaseLocalRotation = baseLocalRotation;
                restoreAfterRender = true;
            }

            lastAppliedPositionOffset = currentShakePositionOffset;
            lastAppliedRotationOffset = Quaternion.Euler(currentShakeRotationOffset);
            hasAppliedOffsets = true;

            cameraTransform.localPosition = baseLocalPosition + lastAppliedPositionOffset;
            cameraTransform.localRotation = baseLocalRotation * lastAppliedRotationOffset;
        }

        private void ResetAppliedOffsets()
        {
            hasAppliedOffsets = false;
            lastAppliedPositionOffset = Vector3.zero;
            lastAppliedRotationOffset = Quaternion.identity;
        }

        private static bool IsInstanceNull()
        {
            if (Instance == null)
            {
                Debug.LogError("CameraShaker Instance is missing!");
                return true;
            }
            return false;
        }
    }
}
