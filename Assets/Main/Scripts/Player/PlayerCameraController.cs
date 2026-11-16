using Unity.Cinemachine;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class PlayerCameraController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private Transform trackingCamera;
    [SerializeField] private Transform pointZoom;
    [SerializeField] private Transform endGameFollowTarget;
    [SerializeField] private Transform playerPositionReference;
    [SerializeField] private float offsetLeft;
    [SerializeField] private float offsetRight;
    [SerializeField] private bool isCameraFollow;

    [Header("Zoom")]
    [SerializeField] private float pointZoomThresholdY = 435f;

    private bool isUsingPointZoom;
    private bool lockToCustomTarget;
    private Transform lockedTarget;
    private Vector3 trackingCameraOffset;
    private float referenceX;
    private bool trainHitCameraSequenceActive;
    private bool trainTimelineFinished;
    private bool trackingRestoreRequested;

    public bool IsFollowingCustomTarget => lockToCustomTarget;

    // Resolve camera references early and start from the default tracking target.
    private void Awake()
    {
        ResolveReferences();
        if (trackingCamera != null)
            trackingCameraOffset = trackingCamera.position - transform.position;
        referenceX = playerPositionReference != null
            ? playerPositionReference.position.x
            : transform.position.x;
        SetFollowTarget(trackingCamera);
    }

    // Keep the camera follow target synced with the player's vertical position.
    private void Update()
    {
        ResolveReferences();
        if (lockToCustomTarget)
        {
            SetFollowTarget(lockedTarget);
            return;
        }

        UpdateFollowTarget();
    }

    private void LateUpdate()
    {
        UpdateCameraFollowPosition();
    }

    private void UpdateCameraFollowPosition()
    {
        if (trackingCamera == null ||
            !isCameraFollow ||
            MatchManager.Instance == null ||
            !MatchManager.Instance.matchStarted)
            return;

        Vector3 targetPosition = transform.position + trackingCameraOffset;
        float left = referenceX - offsetLeft;
        float right = referenceX + offsetRight;
        targetPosition.x = Mathf.Clamp(transform.position.x, left, right) + trackingCameraOffset.x;

        trackingCamera.position = targetPosition;
    }

    // Swap between the normal tracking target and the zoom point when the player crosses the threshold.
    private void UpdateFollowTarget()
    {
        if (cinemachineCamera == null)
            return;

        if (transform.position.y < pointZoomThresholdY)
        {
            if (!isUsingPointZoom)
                SetFollowTarget(pointZoom);
            return;
        }

        if (isUsingPointZoom)
            SetFollowTarget(trackingCamera);
    }

    // Assign the Cinemachine tracking target and cache which camera mode is active.
    private void SetFollowTarget(Transform target)
    {
        if (cinemachineCamera == null || target == null)
            return;

        cinemachineCamera.Target.TrackingTarget = target;
        isUsingPointZoom = target == pointZoom;
    }

    public void MoveToPointZoomCamera()
    {
        ResolveReferences();
        LockFollowTarget(pointZoom);
    }

    public void MoveToTrainTarget(Transform pointTrain)
    {
        ResolveReferences();
        LockFollowTarget(pointTrain);
    }

    public void BeginTrainHitCameraSequence()
    {
        trainHitCameraSequenceActive = true;
        trainTimelineFinished = false;
        trackingRestoreRequested = false;
    }

    public void CompleteTrainHitTimeline()
    {
        if (!trainHitCameraSequenceActive)
            return;

        trainTimelineFinished = true;
        if (trackingRestoreRequested)
        {
            ClearTrainHitCameraSequence();
            RestoreTrackingCamera();
            return;
        }

        MoveToPointZoomCamera();
    }

    public void CancelTrainHitCameraSequence()
    {
        ClearTrainHitCameraSequence();
    }

    public void MoveToEndGameTarget()
    {
        ResolveReferences();
        LockFollowTarget(endGameFollowTarget != null ? endGameFollowTarget : pointZoom);
    }

    public void RestoreTrackingCamera()
    {
        ResolveReferences();

        // A train hit can finish its death/respawn flow before the Timeline.
        // Keep following the train until the Timeline has actually completed.
        if (trainHitCameraSequenceActive && !trainTimelineFinished)
        {
            trackingRestoreRequested = true;
            return;
        }

        ClearTrainHitCameraSequence();
        lockToCustomTarget = false;
        lockedTarget = null;
        SetFollowTarget(trackingCamera);
    }

    private void ClearTrainHitCameraSequence()
    {
        trainHitCameraSequenceActive = false;
        trainTimelineFinished = false;
        trackingRestoreRequested = false;
    }

    private void LockFollowTarget(Transform target)
    {
        if (target == null)
            return;

        lockToCustomTarget = true;
        lockedTarget = target;
        SetFollowTarget(target);
    }

    // Auto-fill the most likely references from the current scene setup.
    private void ResolveReferences()
    {
        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (trackingCamera == null)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i] != null && children[i].name == "TrackingCamera")
                {
                    trackingCamera = children[i];
                    break;
                }
            }
        }

        if (pointZoom == null)
        {
            GameObject pointZoomObject = GameObject.Find("pointZoom");
            if (pointZoomObject == null)
                pointZoomObject = GameObject.Find("PointZoomCamera");

            if (pointZoomObject != null)
                pointZoom = pointZoomObject.transform;
        }

        if (endGameFollowTarget == null)
        {
            GameObject endGameTargetObject = GameObject.Find("EndGameCameraTarget");
            if (endGameTargetObject != null)
                endGameFollowTarget = endGameTargetObject.transform;
        }
    }
}
