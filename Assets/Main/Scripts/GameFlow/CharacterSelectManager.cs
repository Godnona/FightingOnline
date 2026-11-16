using UnityEngine;

public class CharacterSelectManager : MonoBehaviour
{
    [SerializeField] private GameFlowSceneLoader sceneLoader;
    [SerializeField] private int defaultCharacterId = -1;

    private int selectedCharacterId = -1;

    public int SelectedCharacterId
    {
        get { return selectedCharacterId; }
    }

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        GameSession.EnsureExists();
    }

    private void Start()
    {
        if (defaultCharacterId >= 0)
        {
            SelectCharacter(defaultCharacterId);
        }

        GameSession session = GameSession.EnsureExists();
        if (!session.HasSelectedGameMode)
        {
            Debug.LogError("Character Select was opened without a selected GameMode. Return to Main Menu and choose a mode first.");
        }
    }

    public void SelectCharacter(int characterId)
    {
        if (characterId < 0)
        {
            Debug.LogError("Cannot select character. Character id must be zero or greater. Received: " + characterId);
            return;
        }

        selectedCharacterId = characterId;
        Debug.Log("Selected character id: " + selectedCharacterId);
    }

    public void ConfirmSelection()
    {
        GameSession session = GameSession.EnsureExists();

        if (!session.HasSelectedGameMode)
        {
            Debug.LogError("Cannot confirm character selection because no GameMode is selected.");
            return;
        }

        if (selectedCharacterId < 0)
        {
            Debug.LogError("Cannot confirm character selection because no character has been selected.");
            return;
        }

        session.SelectCharacter(selectedCharacterId);

        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        sceneLoader.LoadGameplayScene(session.SelectedGameMode);
    }

    public void BackToMainMenu()
    {
        GameSession session = GameSession.EnsureExists();
        session.ResetSession();

        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        sceneLoader.LoadMainMenuScene();
    }
}
