using UnityEngine;

[DisallowMultipleComponent]
public class TimeoutMatchResultUI : MonoBehaviour
{
    [SerializeField] private MatchManager matchManager;

    private void Awake()
    {
        ResolveReferences();
    }

    public void ResolveResultFromMatchManager()
    {
        ResolveReferences();

        if (matchManager != null)
            matchManager.HandleMatchDurationExpired();
    }

    private void ResolveReferences()
    {
        if (matchManager == null)
            matchManager = MatchManager.Instance != null ? MatchManager.Instance : FindFirstObjectByType<MatchManager>();
    }
}
