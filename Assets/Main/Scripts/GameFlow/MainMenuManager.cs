using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    [SerializeField] private GameFlowSceneLoader sceneLoader;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        GameSession.EnsureExists();
    }

    public void StartStoryMode()
    {
        StartGameMode(GameMode.Story);
    }

    public void StartBrawl()
    {
        StartGameMode(GameMode.Brawl);
    }

    public void StartGridWarsOnline()
    {
        StartGameMode(GameMode.GridWarsOnline);
    }

    public void StartLab()
    {
        StartGameMode(GameMode.Lab);
    }

    public void StartSimulation()
    {
        StartGameMode(GameMode.Simulation);
    }

    public void StartGameMode(GameMode gameMode)
    {
        GameSession session = GameSession.EnsureExists();
        session.SelectGameMode(gameMode);

        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        sceneLoader.LoadCharacterSelectScene();
    }
}
