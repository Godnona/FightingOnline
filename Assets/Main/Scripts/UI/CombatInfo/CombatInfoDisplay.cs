using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class CombatInfoDisplay : MonoBehaviour
{
    public static CombatInfoDisplay Instance { get; private set; }

    [Header("Container")]
    [SerializeField] private GameObject visualContainer;
    [SerializeField] private bool hideWhenInactive;

    [Header("Value Text References")]
    [SerializeField] private TMP_Text comboHitsValueText;
    [SerializeField] private TMP_Text comboDamageValueText;
    [SerializeField] private TMP_Text lastHitDamageValueText;

    [Header("Settings")]
    [FormerlySerializedAs("comboDuration")]
    [SerializeField] private float comboResetDelay = 3f;
    [SerializeField] private float fallbackComboDuration = 3f;
    [SerializeField] private bool resetValuesWhenComboEnds = true;

    private int comboHits;
    private int comboDamage;
    private int lastHitDamage;
    private float fallbackComboTimer;
    private float resetDelayTimer;
    private bool isComboActive;
    private bool isResetPending;
    private ComboManager comboManager;

    public int ComboHits => comboHits;
    public int ComboDamage => comboDamage;
    public int LastHitDamage => lastHitDamage;

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

        if (visualContainer == null)
            visualContainer = gameObject;

        comboResetDelay = Mathf.Max(0f, comboResetDelay);
        fallbackComboDuration = Mathf.Max(0.01f, fallbackComboDuration);
        RefreshUI();
        SetVisible(!hideWhenInactive);
    }

    private void OnEnable()
    {
        TryBindComboManager();
    }

    private void Start()
    {
        TryBindComboManager();
    }

    private void Update()
    {
        TryBindComboManager();

        if (comboManager == null && isComboActive)
        {
            fallbackComboTimer -= Time.deltaTime;
            if (fallbackComboTimer <= 0f)
                BeginComboLostDelay();
        }

        if (!isResetPending)
            return;

        resetDelayTimer -= Time.deltaTime;
        if (resetDelayTimer <= 0f)
            ResetValuesAfterDelay();
    }

    private void OnDestroy()
    {
        UnbindComboManager();

        if (Instance == this)
            Instance = null;
    }

    public void RecordHit(int damageAmount)
    {
        TryBindComboManager();

        int safeDamage = Mathf.Max(0, damageAmount);
        if (isResetPending)
            ClearComboValues();

        comboHits = comboManager != null ? Mathf.Max(0, comboManager.CurrentCombo) : comboHits + 1;
        comboDamage += safeDamage;
        lastHitDamage = safeDamage;
        fallbackComboTimer = fallbackComboDuration;
        isComboActive = true;
        CancelPendingReset();

        SetVisible(true);
        RefreshUI();
    }

    public void ResetCombatInfo()
    {
        comboHits = 0;
        comboDamage = 0;
        lastHitDamage = 0;
        fallbackComboTimer = 0f;
        resetDelayTimer = 0f;
        isComboActive = false;
        isResetPending = false;

        RefreshUI();
        SetVisible(!hideWhenInactive);
    }

    private void HandleComboHitCountChanged(int currentCombo)
    {
        if (isResetPending)
            ClearComboValues();

        comboHits = Mathf.Max(0, currentCombo);
        isComboActive = true;
        CancelPendingReset();
        SetVisible(true);
        RefreshUI();
    }

    private void HandleComboEnded(int endedCombo)
    {
        if (endedCombo > 0)
            comboHits = endedCombo;

        BeginComboLostDelay();
    }

    private void BeginComboLostDelay()
    {
        isComboActive = false;
        fallbackComboTimer = 0f;

        if (!resetValuesWhenComboEnds)
        {
            isResetPending = false;
            SetVisible(!hideWhenInactive);
            return;
        }

        if (comboResetDelay <= 0f)
        {
            ResetValuesAfterDelay();
            return;
        }

        resetDelayTimer = comboResetDelay;
        isResetPending = true;
        SetVisible(true);
        RefreshUI();
    }

    private void ResetValuesAfterDelay()
    {
        ClearComboValues();
        fallbackComboTimer = 0f;
        resetDelayTimer = 0f;
        isComboActive = false;
        isResetPending = false;

        RefreshUI();
        SetVisible(!hideWhenInactive);
    }

    private void CancelPendingReset()
    {
        resetDelayTimer = 0f;
        isResetPending = false;
    }

    private void ClearComboValues()
    {
        comboHits = 0;
        comboDamage = 0;
        lastHitDamage = 0;
    }

    private void RefreshUI()
    {
        SetValueText(comboHitsValueText, comboHits);
        SetValueText(comboDamageValueText, comboDamage);
        SetValueText(lastHitDamageValueText, lastHitDamage);
    }

    private static void SetValueText(TMP_Text text, int value)
    {
        if (text == null)
            return;

        text.text = value.ToString();
    }

    private void SetVisible(bool visible)
    {
        if (visualContainer != null && visualContainer.activeSelf != visible)
            visualContainer.SetActive(visible);
    }

    private void TryBindComboManager()
    {
        if (comboManager == ComboManager.Instance)
            return;

        UnbindComboManager();
        comboManager = ComboManager.Instance;

        if (comboManager == null)
            return;

        comboManager.ComboHitCountChanged += HandleComboHitCountChanged;
        comboManager.ComboEnded += HandleComboEnded;
    }

    private void UnbindComboManager()
    {
        if (comboManager == null)
            return;

        comboManager.ComboHitCountChanged -= HandleComboHitCountChanged;
        comboManager.ComboEnded -= HandleComboEnded;
        comboManager = null;
    }
}
