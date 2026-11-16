using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameFlowSceneLoader : MonoBehaviour
{
    [Serializable]
    public class GameModeSceneBinding
    {
        public GameMode gameMode;
        public string sceneName;
    }

    public static GameFlowSceneLoader Instance { get; private set; }

    [Header("Shared Flow Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenuScene";
    [SerializeField] private string characterSelectSceneName = "CharacterUpgradeScene";

    [Header("Gameplay Scenes")]
    [SerializeField] private List<GameModeSceneBinding> gameplayScenes = new List<GameModeSceneBinding>
    {
        new GameModeSceneBinding { gameMode = GameMode.Story, sceneName = "" },
        new GameModeSceneBinding { gameMode = GameMode.Brawl, sceneName = "" },
        new GameModeSceneBinding { gameMode = GameMode.GridWarsOnline, sceneName = "" },
        new GameModeSceneBinding { gameMode = GameMode.Lab, sceneName = "" },
        new GameModeSceneBinding { gameMode = GameMode.Simulation, sceneName = "" }
    };

    public static GameFlowSceneLoader EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameFlowSceneLoader existingLoader = FindFirstObjectByType<GameFlowSceneLoader>();
        if (existingLoader != null)
        {
            return existingLoader;
        }

        GameObject loaderObject = new GameObject("GameFlowSceneLoader");
        return loaderObject.AddComponent<GameFlowSceneLoader>();
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

    public void LoadMainMenuScene()
    {
        LoadSceneIfAvailable(mainMenuSceneName, "Main Menu");
    }

    public void LoadCharacterSelectScene()
    {
        LoadSceneIfAvailable(characterSelectSceneName, "Character Select");
    }

    public void LoadGameplayScene(GameMode gameMode)
    {
        if (gameMode == GameMode.None)
        {
            Debug.LogError("Cannot load gameplay scene because no GameMode has been selected.");
            return;
        }

        string sceneName = GetGameplaySceneName(gameMode);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("No gameplay scene is configured for GameMode " + gameMode + ". Assign it on GameFlowSceneLoader.");
            return;
        }

        LoadSceneIfAvailable(sceneName, "Gameplay mode " + gameMode);
    }

    public void ActivateGameplayScene(GameMode gameMode)
    {
        LoadGameplayScene(gameMode);
    }

    public void PreloadGameplayScene(GameMode gameMode)
    {
        if (gameMode == GameMode.None)
        {
            Debug.LogError("Cannot prepare gameplay scene because no GameMode has been selected.");
            return;
        }

        string sceneName = GetGameplaySceneName(gameMode);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("No gameplay scene is configured for GameMode " + gameMode + ". Assign it on GameFlowSceneLoader.");
        }
    }

    public string GetGameplaySceneName(GameMode gameMode)
    {
        for (int i = 0; i < gameplayScenes.Count; i++)
        {
            GameModeSceneBinding binding = gameplayScenes[i];
            if (binding != null && binding.gameMode == gameMode)
            {
                return binding.sceneName;
            }
        }

        return string.Empty;
    }

    private void LoadSceneIfAvailable(string sceneName, string sceneLabel)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError(sceneLabel + " scene name is empty on GameFlowSceneLoader.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(sceneLabel + " scene '" + sceneName + "' cannot be loaded. Check the exact scene name and make sure it is enabled in Build Settings or Build Profiles.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
