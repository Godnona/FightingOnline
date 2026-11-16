using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    [Serializable]
    private class ManagedComponentEntry
    {
        public GameObject targetObject;
        public string componentName;
    }

    private sealed class ManagedComponentState
    {
        public Component component;
        public PropertyInfo toggleProperty;
        public bool originalValue;
    }

    private sealed class ManagedObjectState
    {
        public GameObject targetObject;
        public bool originalActiveState;
    }

    public static MatchManager Instance;
    public bool matchStarted;

    [Header("Match Settings")]
    [Tooltip("Thoi gian cua tran dau (dong bo cho ca Timer va EndGame)")]
    public float matchDuration = 90f;
    [SerializeField] private bool isMatchCountdown = true;

    [Header("Countdown Flow")]
    [SerializeField] private bool disableConfiguredComponentsUntilMatchStart = true;
    [SerializeField] private List<ManagedComponentEntry> managedComponents = new List<ManagedComponentEntry>();

    [Header("Endgame Flow")]
    [SerializeField] private bool disableConfiguredObjectsOnEndGame = true;
    [SerializeField] private List<GameObject> endGameObjectsToDisable = new List<GameObject>();

    [Header("Timeout Result")]
    [SerializeField] private ScoreController playerScore;
    [SerializeField] private ScoreController enemyScore;
    [SerializeField] private PlayerCameraController playerCameraController;
    [SerializeField] private GameObject endGameRoot;
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject loseUI;
    [SerializeField] private bool playerWinsOnTie = true;

    private readonly List<ManagedComponentState> runtimeManagedComponents = new List<ManagedComponentState>();
    private readonly List<ManagedObjectState> runtimeManagedObjects = new List<ManagedObjectState>();
    private readonly HashSet<int> cachedComponentIds = new HashSet<int>();
    private readonly HashSet<int> cachedObjectIds = new HashSet<int>();
    private bool hasResolvedTimeoutResult;
    private float remainingMatchTime;

    public float RemainingMatchTime => remainingMatchTime;
    public float MatchTimeNormalized => matchDuration <= 0f ? 0f : Mathf.Clamp01(remainingMatchTime / matchDuration);

    private void Awake()
    {
        Instance = this;
        matchStarted = false;
        remainingMatchTime = matchDuration;

        CacheManagedComponents();
        CacheManagedObjects();

        RestoreManagedObjectsToOriginalState();
        ResolveTimeoutResultReferences();
        HideTimeoutResultUi();

        if (disableConfiguredComponentsUntilMatchStart)
            ApplyConfiguredComponents(false);
    }

    private void Update()
    {
        if (!matchStarted || hasResolvedTimeoutResult || !isMatchCountdown )
            return;

        remainingMatchTime -= Time.deltaTime;
        if (remainingMatchTime > 0f)
            return;

        remainingMatchTime = 0f;
        HandleMatchDurationExpired();
    }

    public void StartMatch()
    {
        if (matchStarted)
            return;

        matchStarted = true;
        hasResolvedTimeoutResult = false;
        remainingMatchTime = matchDuration;

        ResolveTimeoutResultReferences();
        HideTimeoutResultUi();

        if (disableConfiguredComponentsUntilMatchStart)
            ApplyConfiguredComponents(true);

        RestoreManagedObjectsToOriginalState();
    }

    public void StopMatch()
    {
        if (!matchStarted && !disableConfiguredComponentsUntilMatchStart && !disableConfiguredObjectsOnEndGame)
            return;

        matchStarted = false;
        remainingMatchTime = Mathf.Max(remainingMatchTime, 0f);

        if (disableConfiguredComponentsUntilMatchStart)
            ApplyConfiguredComponents(false);

        if (disableConfiguredObjectsOnEndGame)
            ApplyEndGameObjects(false);
    }

    public void HandleMatchDurationExpired()
    {
        if (hasResolvedTimeoutResult)
            return;

        hasResolvedTimeoutResult = true;
        ResolveTimeoutResultReferences();
        StopMatch();

        if (playerCameraController != null)
            playerCameraController.MoveToEndGameTarget();

        bool playerWon = DidPlayerWinOnTimeout();

        if (endGameRoot != null && !endGameRoot.activeSelf)
            endGameRoot.SetActive(true);

        SetUiActive(winUI, playerWon);
        SetUiActive(loseUI, !playerWon);
    }

    private void CacheManagedComponents()
    {
        runtimeManagedComponents.Clear();
        cachedComponentIds.Clear();

        for (int i = 0; i < managedComponents.Count; i++)
        {
            ManagedComponentEntry entry = managedComponents[i];
            if (entry == null || entry.targetObject == null || string.IsNullOrWhiteSpace(entry.componentName))
                continue;

            if (!TryFindComponent(entry.targetObject, entry.componentName, out Component component))
            {
                Debug.LogWarning($"MatchManager could not find component '{entry.componentName}' on '{entry.targetObject.name}'.", this);
                continue;
            }

            if (!TryResolveToggleProperty(component, out PropertyInfo toggleProperty))
            {
                Debug.LogWarning($"MatchManager could not toggle component '{component.GetType().Name}' on '{entry.targetObject.name}'.", this);
                continue;
            }

            int componentId = component.GetInstanceID();
            if (!cachedComponentIds.Add(componentId))
                continue;

            runtimeManagedComponents.Add(new ManagedComponentState
            {
                component = component,
                toggleProperty = toggleProperty,
                originalValue = ReadToggleValue(component, toggleProperty)
            });
        }
    }

    private void CacheManagedObjects()
    {
        runtimeManagedObjects.Clear();
        cachedObjectIds.Clear();

        for (int i = 0; i < endGameObjectsToDisable.Count; i++)
        {
            GameObject targetObject = endGameObjectsToDisable[i];
            if (targetObject == null)
                continue;

            int objectId = targetObject.GetInstanceID();
            if (!cachedObjectIds.Add(objectId))
                continue;

            runtimeManagedObjects.Add(new ManagedObjectState
            {
                targetObject = targetObject,
                originalActiveState = targetObject.activeSelf
            });
        }
    }

    private void ApplyConfiguredComponents(bool restoreOriginalState)
    {
        for (int i = 0; i < runtimeManagedComponents.Count; i++)
        {
            ManagedComponentState state = runtimeManagedComponents[i];
            if (state.component == null || state.toggleProperty == null)
                continue;

            bool nextValue = restoreOriginalState ? state.originalValue : false;
            WriteToggleValue(state.component, state.toggleProperty, nextValue);
        }
    }

    private void ApplyEndGameObjects(bool restoreOriginalState)
    {
        for (int i = 0; i < runtimeManagedObjects.Count; i++)
        {
            ManagedObjectState state = runtimeManagedObjects[i];
            if (state.targetObject == null)
                continue;

            bool nextValue = restoreOriginalState ? state.originalActiveState : false;
            state.targetObject.SetActive(nextValue);
        }
    }

    private void RestoreManagedObjectsToOriginalState()
    {
        ApplyEndGameObjects(true);
    }

    private void ResolveTimeoutResultReferences()
    {
        if (playerScore == null || enemyScore == null)
            ResolveScoreControllers();

        if (playerCameraController == null)
            playerCameraController = FindFirstObjectByType<PlayerCameraController>();

        if (endGameRoot == null)
        {
            TimeoutMatchResultUI timeoutResultUi = FindFirstObjectByType<TimeoutMatchResultUI>(FindObjectsInactive.Include);
            if (timeoutResultUi != null)
                endGameRoot = timeoutResultUi.gameObject;
        }

        if (endGameRoot == null)
            endGameRoot = GameObject.Find("Canvas_EndGameUi");

        if (winUI == null)
            winUI = FindChildByName(endGameRoot != null ? endGameRoot.transform : null, "Win");

        if (loseUI == null)
            loseUI = FindChildByName(endGameRoot != null ? endGameRoot.transform : null, "Lose");
    }

    private void ResolveScoreControllers()
    {
        ScoreController[] scores = FindObjectsByType<ScoreController>(FindObjectsSortMode.None);
        for (int i = 0; i < scores.Length; i++)
        {
            ScoreController score = scores[i];
            if (score == null)
                continue;

            if (enemyScore == null && score.GetComponent<EnemyMovement>() != null)
            {
                enemyScore = score;
                continue;
            }

            if (playerScore == null &&
                (score.GetComponent<PlayerController1>() != null || score.GetComponent<PlayerMovement>() != null))
            {
                playerScore = score;
            }
        }

        if (playerScore != null && enemyScore == null)
        {
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] != null && scores[i] != playerScore)
                {
                    enemyScore = scores[i];
                    break;
                }
            }
        }

        if (enemyScore != null && playerScore == null)
        {
            for (int i = 0; i < scores.Length; i++)
            {
                if (scores[i] != null && scores[i] != enemyScore)
                {
                    playerScore = scores[i];
                    break;
                }
            }
        }
    }

    private bool DidPlayerWinOnTimeout()
    {
        int playerPoints = playerScore != null ? playerScore.CurrentScore : 0;
        int enemyPoints = enemyScore != null ? enemyScore.CurrentScore : 0;
        return playerPoints > enemyPoints || (playerWinsOnTie && playerPoints == enemyPoints);
    }

    private void HideTimeoutResultUi()
    {
        SetUiActive(winUI, false);
        SetUiActive(loseUI, false);
    }

    private static GameObject FindChildByName(Transform root, string childName)
    {
        if (root == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private static void SetUiActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }

    private static bool TryFindComponent(GameObject targetObject, string componentName, out Component foundComponent)
    {
        Component[] components = targetObject.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            Type componentType = component.GetType();
            if (string.Equals(componentType.Name, componentName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(componentType.FullName, componentName, StringComparison.OrdinalIgnoreCase))
            {
                foundComponent = component;
                return true;
            }
        }

        foundComponent = null;
        return false;
    }

    private static bool TryResolveToggleProperty(Component component, out PropertyInfo toggleProperty)
    {
        Type componentType = component.GetType();
        toggleProperty = componentType.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
        if (IsValidToggleProperty(toggleProperty))
            return true;

        toggleProperty = componentType.GetProperty("simulated", BindingFlags.Instance | BindingFlags.Public);
        if (IsValidToggleProperty(toggleProperty))
            return true;

        toggleProperty = null;
        return false;
    }

    private static bool IsValidToggleProperty(PropertyInfo property)
    {
        return property != null &&
               property.PropertyType == typeof(bool) &&
               property.CanRead &&
               property.CanWrite;
    }

    private static bool ReadToggleValue(Component component, PropertyInfo property)
    {
        object value = property.GetValue(component);
        return value is bool boolValue && boolValue;
    }

    private static void WriteToggleValue(Component component, PropertyInfo property, bool value)
    {
        property.SetValue(component, value);
    }
}
