using UnityEngine;

public class GameSession : MonoBehaviour
{
    public static GameSession Instance { get; private set; }

    public GameMode SelectedGameMode { get; private set; } = GameMode.None;
    public int SelectedCharacterId { get; private set; } = -1;

    public bool HasSelectedGameMode
    {
        get { return SelectedGameMode != GameMode.None; }
    }

    public bool HasSelectedCharacter
    {
        get { return SelectedCharacterId >= 0; }
    }

    public static GameSession EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameSession existingSession = FindFirstObjectByType<GameSession>();
        if (existingSession != null)
        {
            return existingSession;
        }

        GameObject sessionObject = new GameObject("GameSession");
        return sessionObject.AddComponent<GameSession>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SelectGameMode(GameMode gameMode)
    {
        if (gameMode == GameMode.None)
        {
            Debug.LogError("GameSession.SelectGameMode was called with GameMode.None.");
            return;
        }

        SelectedGameMode = gameMode;
        ClearSelectedCharacter();
    }

    public void SelectCharacter(int characterId)
    {
        if (characterId < 0)
        {
            Debug.LogError("GameSession.SelectCharacter was called with an invalid character id: " + characterId);
            return;
        }

        SelectedCharacterId = characterId;
    }

    public void ClearSelectedCharacter()
    {
        SelectedCharacterId = -1;
    }

    public void ClearSelectedGameMode()
    {
        SelectedGameMode = GameMode.None;
    }

    public void ResetSession()
    {
        SelectedGameMode = GameMode.None;
        SelectedCharacterId = -1;
    }
}
