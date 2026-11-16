using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class GameEvent : MonoBehaviour
{
    [Header("Train Event")]
    [SerializeField] private PlayableDirector trainDirector;
    [SerializeField] private Animator trainAnimator;
    [SerializeField] private PlayerCameraController playerCameraController;
    [SerializeField] private Transform pointTrain;

    [Header("Train Timing")]
    [Tooltip("Time in seconds between each train event.")]
    [SerializeField, Min(0.1f)] private float trainInterval = 10f;
    [SerializeField] private bool repeatTrainEvent = true;
    [SerializeField] private bool restoreCameraAfterTimeline = true;

    private Coroutine trainRoutine;
    private bool isStoppingTimeline;
    private bool playerHitByTrainDuringTimeline;

    private void Reset()
    {
        trainDirector = GetComponent<PlayableDirector>();
        if (trainDirector == null)
            trainDirector = GetComponentInChildren<PlayableDirector>(true);

        trainAnimator = GetComponent<Animator>();
        if (trainAnimator == null)
            trainAnimator = GetComponentInChildren<Animator>(true);

        playerCameraController = FindFirstObjectByType<PlayerCameraController>();
    }

    private void Awake()
    {
        CacheReferences();
        PrepareTrainTimeline();
    }

    private void OnEnable()
    {
        CacheReferences();

        if (trainDirector == null)
        {
            Debug.LogWarning("GameEvent needs a reference to the train PlayableDirector.", this);
            return;
        }

        trainDirector.stopped -= HandleTrainTimelineStopped;
        trainDirector.stopped += HandleTrainTimelineStopped;

        PrepareTrainTimeline();
        trainRoutine = StartCoroutine(TrainEventLoop());
    }

    private void OnDisable()
    {
        if (trainRoutine != null)
        {
            StopCoroutine(trainRoutine);
            trainRoutine = null;
        }

        if (trainDirector != null)
        {
            trainDirector.stopped -= HandleTrainTimelineStopped;
            StopTrainTimeline(false);
        }
    }

    public void PlayTrainTimeline()
    {
        CacheReferences();
        if (trainDirector == null)
            return;

        if (playerCameraController != null && pointTrain != null)
            playerCameraController.MoveToTrainTarget(pointTrain);

        // Restart cleanly if this method is called while the event is active.
        if (trainDirector.state == PlayState.Playing)
            StopTrainTimeline(false);

        playerHitByTrainDuringTimeline = false;

        if (trainAnimator != null)
            trainAnimator.enabled = true;

        SetTimelineActive(true);
        trainDirector.playOnAwake = false;
        trainDirector.time = 0d;
        trainDirector.Play();
    }

    // Kept for existing UnityEvents or code that still uses the old method name.
    public void PlayTrainAnimation()
    {
        PlayTrainTimeline();
    }

    public void StopTrainTimeline()
    {
        StopTrainTimeline(true);
    }

    public void NotifyPlayerHitByTrain()
    {
        if (trainDirector == null || trainDirector.state != PlayState.Playing)
            return;

        playerHitByTrainDuringTimeline = true;

        if (playerCameraController != null)
            playerCameraController.BeginTrainHitCameraSequence();
    }

    public void RestartTrainCountdown()
    {
        if (!isActiveAndEnabled || trainDirector == null)
            return;

        if (trainRoutine != null)
            StopCoroutine(trainRoutine);

        StopTrainTimeline(false);
        trainRoutine = StartCoroutine(TrainEventLoop());
    }

    private IEnumerator TrainEventLoop()
    {
        do
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, trainInterval));
            PlayTrainTimeline();
        }
        while (repeatTrainEvent);

        trainRoutine = null;
    }

    private void HandleTrainTimelineStopped(PlayableDirector director)
    {
        if (isStoppingTimeline || director != trainDirector)
            return;

        bool moveCameraToRespawnView = playerHitByTrainDuringTimeline;
        playerHitByTrainDuringTimeline = false;
        ResetAndDisableTimeline();

        if (playerCameraController == null)
            return;

        if (moveCameraToRespawnView)
        {
            playerCameraController.CompleteTrainHitTimeline();
            return;
        }

        if (restoreCameraAfterTimeline)
            playerCameraController.RestoreTrackingCamera();
    }

    private void StopTrainTimeline(bool restoreCamera)
    {
        if (trainDirector == null)
            return;

        isStoppingTimeline = true;
        if (trainDirector.state == PlayState.Playing)
            trainDirector.Stop();

        ResetAndDisableTimeline();
        isStoppingTimeline = false;
        playerHitByTrainDuringTimeline = false;

        if (playerCameraController == null)
            return;

        playerCameraController.CancelTrainHitCameraSequence();
        if (restoreCamera && restoreCameraAfterTimeline)
            playerCameraController.RestoreTrackingCamera();
    }

    private void PrepareTrainTimeline()
    {
        if (trainDirector == null)
            return;

        trainDirector.playOnAwake = false;
        StopTrainTimeline(false);
    }

    private void ResetAndDisableTimeline()
    {
        if (trainDirector == null)
            return;

        trainDirector.time = 0d;
        if (trainDirector.enabled && trainDirector.gameObject.activeInHierarchy)
            trainDirector.Evaluate();

        SetTimelineActive(false);

        // The Animator is still required as a Timeline binding, but keeping it
        // disabled here prevents its old controller from moving the train.
        if (trainAnimator != null)
            trainAnimator.enabled = false;
    }

    private void SetTimelineActive(bool active)
    {
        // Normally the director has its own GameObject. This fallback prevents
        // GameEvent from disabling itself if both components share one object.
        if (trainDirector.gameObject == gameObject)
        {
            trainDirector.enabled = active;
            return;
        }

        trainDirector.gameObject.SetActive(active);
    }

    private void CacheReferences()
    {
        if (trainDirector == null)
        {
            trainDirector = GetComponent<PlayableDirector>();
            if (trainDirector == null)
                trainDirector = GetComponentInChildren<PlayableDirector>(true);
        }

        if (playerCameraController == null)
            playerCameraController = FindFirstObjectByType<PlayerCameraController>();
    }
}
