using UnityEngine;
using TMPro;
using System;
using System.Collections;
using System.Collections.Generic;

public class ComboManager : MonoBehaviour
{
    private const string DefaultAlertObjectName = "Alert";
    private const string DefaultAlertComboParameter = "Combo";

    public static ComboManager Instance { get; private set; }
    public event Action<int> ComboHitCountChanged;
    public event Action<int> ComboEnded;

    [Header("UI References")]
    [SerializeField] private GameObject visualContainer; // The object to enable/disable
    [SerializeField] private GameObject hitsObject;      // "Hits" text object
    [SerializeField] private GameObject numsObject;      // "Nums" text object
    [SerializeField] private Animator comboAnimator;
    [SerializeField] private Animator alertAnimator;
    [SerializeField] private string alertComboParameter = DefaultAlertComboParameter;
    [SerializeField] private TextMeshProUGUI comboText;
    [SerializeField] private AudioSource audioSource;

    [Header("Settings")]
    [Tooltip("How many seconds without a hit before the combo is lost")]
    public float comboDuration = 3f;
    [SerializeField] private int comboDisplayStartCount = 3;
    [SerializeField] private bool revealComboTextFromAnimationEvent = true;
    [SerializeField] private float numsShakeDuration = 0.15f;
    [SerializeField] private float numsShakeStrength = 10f;
    [SerializeField] private float numsShakeFrequency = 45f;

    [Header("Combo Sounds")]
    [SerializeField] private AudioClip combo3Sound;
    [SerializeField] private AudioClip combo5Sound;
    [SerializeField] private AudioClip combo7Sound;
    [SerializeField] private AudioClip combo9Sound;
    [SerializeField] [Range(0f, 1f)] private float comboSoundVolume = 1f;

    private int currentCombo = 0;
    private float comboTimer = 0f;
    private bool isComboActive = false;
    private Coroutine numsShakeRoutine;
    private Coroutine soundQueueRoutine;
    private RectTransform numsRectTransform;
    private Vector2 numsDefaultAnchoredPos;
    private CanvasGroup visualCanvasGroup;
    private bool keepsManagerObjectActive;
    private bool pendingComboTextReveal;
    private readonly Queue<AudioClip> pendingComboSounds = new Queue<AudioClip>();

    public int CurrentCombo => currentCombo;
    public bool IsComboActive => isComboActive;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        comboDisplayStartCount = Mathf.Max(1, comboDisplayStartCount);
        ResolveVisualContainerState();
        SetVisualContainerVisible(false);
        ResolveAudioSource();
        ResolveAlertAnimator();
        CacheNumsRectTransform();
        SyncAlertComboParameter();
        SetComboTextObjectsVisible(false);
    }

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (isComboActive)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0)
            {
                EndCombo();
            }
        }
    }

    /// Call this whenever the player successfully hits an enemy.
    public void AddHit()
    {
        currentCombo++;
        comboTimer = comboDuration;
        isComboActive = true;
        SyncAlertComboParameter();
        ComboHitCountChanged?.Invoke(currentCombo);
        EnqueueComboMilestoneSound(currentCombo);

        if (currentCombo == comboDisplayStartCount)
        {
            // Enable the object first
            SetVisualContainerVisible(true);

            pendingComboTextReveal = revealComboTextFromAnimationEvent && comboAnimator != null;
            SetComboTextObjectsVisible(!pendingComboTextReveal);

            if (comboAnimator != null)
            {
                comboAnimator.SetTrigger("ComboCounterStart");
            }

            if (pendingComboTextReveal)
            {
                UpdateUI();
            }
            else
            {
                ShowComboTexts();
            }
        }
        else if (currentCombo > comboDisplayStartCount)
        {
            if (pendingComboTextReveal)
            {
                ShowComboTexts();
            }

            // Update the UI and play increment animation
            if (comboAnimator != null)
            {
                comboAnimator.SetTrigger("CountCombo");
            }
            
            UpdateUI();
            TriggerNumsShake();
        }
    }

    private void EndCombo()
    {
        int endedCombo = currentCombo;

        if (currentCombo >= comboDisplayStartCount)
        {
            // Play the disappear animation (only the background/frame)
            if (comboAnimator != null)
            {
                comboAnimator.SetTrigger("ComboCounterEnd");
            }
            
            // Wait for animation to finish before disabling container
            StartCoroutine(DisableAfterDelay(0.6f)); 
        }
        else
        {
            // If combo didn't reach 2, just hide immediately if it was visible
            SetVisualContainerVisible(false);
        }

        currentCombo = 0;
        isComboActive = false;
        SyncAlertComboParameter();

        if (endedCombo > 0)
            ComboEnded?.Invoke(endedCombo);
    }

    /// Call this from an Animation Event in the ComboCounterEnd animation
    public void HideComboTexts()
    {
        pendingComboTextReveal = false;
        SetComboTextObjectsVisible(false);
        ResetNumsPosition();
    }

    /// Call this from an Animation Event when the intro reaches the frame where combo text should appear.
    public void ShowComboTexts()
    {
        if (!isComboActive || currentCombo < comboDisplayStartCount)
            return;

        pendingComboTextReveal = false;
        SetComboTextObjectsVisible(true);
        UpdateUI();
        TriggerNumsShake();
    }

    private IEnumerator DisableAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only disable if a NEW combo hasn't started in the meantime
        if (!isComboActive)
        {
            SetVisualContainerVisible(false);
        }
    }

    private void UpdateUI()
    {
        if (comboText != null)
        {
            comboText.text = currentCombo.ToString();
        }
    }

    private void SetComboTextObjectsVisible(bool visible)
    {
        if (hitsObject != null)
            hitsObject.SetActive(visible);

        if (numsObject != null)
            numsObject.SetActive(visible);
    }

    private void CacheNumsRectTransform()
    {
        if (comboText != null)
        {
            numsRectTransform = comboText.rectTransform;
        }
        else if (numsObject != null)
        {
            numsRectTransform = numsObject.GetComponent<RectTransform>();
        }

        if (numsRectTransform != null)
        {
            numsDefaultAnchoredPos = numsRectTransform.anchoredPosition;
        }
    }

    private void TriggerNumsShake()
    {
        if (numsRectTransform == null)
        {
            CacheNumsRectTransform();
        }

        if (numsRectTransform == null)
        {
            return;
        }

        if (numsShakeRoutine != null)
        {
            StopCoroutine(numsShakeRoutine);
        }

        numsRectTransform.anchoredPosition = numsDefaultAnchoredPos;
        numsShakeRoutine = StartCoroutine(ShakeNumsRoutine());
    }

    private IEnumerator ShakeNumsRoutine()
    {
        float elapsed = 0f;

        while (elapsed < numsShakeDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / numsShakeDuration);
            float damping = 1f - progress;
            float angle = elapsed * numsShakeFrequency;
            Vector2 offset = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle * 1.17f)) * numsShakeStrength * damping;
            numsRectTransform.anchoredPosition = numsDefaultAnchoredPos + offset;
            yield return null;
        }

        numsRectTransform.anchoredPosition = numsDefaultAnchoredPos;
        numsShakeRoutine = null;
    }

    private void ResetNumsPosition()
    {
        if (numsShakeRoutine != null)
        {
            StopCoroutine(numsShakeRoutine);
            numsShakeRoutine = null;
        }

        if (numsRectTransform != null)
        {
            numsRectTransform.anchoredPosition = numsDefaultAnchoredPos;
        }
    }

    private void ResolveAlertAnimator()
    {
        if (alertAnimator != null)
            return;

        if (visualContainer == null)
            return;

        Transform alertTransform = visualContainer.transform.Find(DefaultAlertObjectName);
        if (alertTransform != null)
            alertAnimator = alertTransform.GetComponent<Animator>();
    }

    private void ResolveVisualContainerState()
    {
        keepsManagerObjectActive = visualContainer == gameObject;

        if (!keepsManagerObjectActive || visualContainer == null)
        {
            visualCanvasGroup = null;
            return;
        }

        visualCanvasGroup = visualContainer.GetComponent<CanvasGroup>();
        if (visualCanvasGroup == null)
        {
            visualCanvasGroup = visualContainer.AddComponent<CanvasGroup>();
        }
    }

    private void SetVisualContainerVisible(bool visible)
    {
        if (visualContainer == null)
            return;

        if (keepsManagerObjectActive)
        {
            if (visualCanvasGroup == null)
            {
                ResolveVisualContainerState();
            }

            if (visualCanvasGroup != null)
            {
                visualCanvasGroup.alpha = visible ? 1f : 0f;
                visualCanvasGroup.interactable = visible;
                visualCanvasGroup.blocksRaycasts = visible;
            }

            return;
        }

        visualContainer.SetActive(visible);
    }

    private void SyncAlertComboParameter()
    {
        ResolveAlertAnimator();
        if (alertAnimator == null)
            return;

        if (!HasAnimatorIntParameter(alertAnimator, alertComboParameter))
            return;

        alertAnimator.SetInteger(alertComboParameter, GetVisibleComboCount());
    }

    private int GetVisibleComboCount()
    {
        return currentCombo >= comboDisplayStartCount ? currentCombo : 0;
    }

    private bool HasAnimatorIntParameter(Animator animatorToCheck, string parameterName)
    {
        if (animatorToCheck == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animatorToCheck.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Int &&
                parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void EnqueueComboMilestoneSound(int comboCount)
    {
        AudioClip clipToQueue = GetComboMilestoneSound(comboCount);
        if (clipToQueue == null)
            return;

        pendingComboSounds.Enqueue(clipToQueue);

        if (soundQueueRoutine == null)
            soundQueueRoutine = StartCoroutine(PlayQueuedComboSounds());
    }

    private AudioClip GetComboMilestoneSound(int comboCount)
    {
        switch (comboCount)
        {
            case 3:
                return combo3Sound;
            case 5:
                return combo5Sound;
            case 7:
                return combo7Sound;
            case 9:
                return combo9Sound;
            default:
                return null;
        }
    }

    private IEnumerator PlayQueuedComboSounds()
    {
        ResolveAudioSource();

        while (pendingComboSounds.Count > 0)
        {
            if (audioSource == null)
            {
                pendingComboSounds.Clear();
                break;
            }

            AudioClip nextClip = pendingComboSounds.Dequeue();
            if (nextClip == null)
                continue;

            audioSource.PlayOneShot(nextClip, comboSoundVolume);
            yield return new WaitForSeconds(nextClip.length);
        }

        soundQueueRoutine = null;
    }

    private void ResolveAudioSource()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void OnDisable()
    {
        if (soundQueueRoutine != null)
        {
            StopCoroutine(soundQueueRoutine);
            soundQueueRoutine = null;
        }

        pendingComboSounds.Clear();
    }
}
