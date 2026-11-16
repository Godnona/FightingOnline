using UnityEngine;

public class SceneModeManager : MonoBehaviour
{
    public static SceneModeManager Instance;

    public enum SceneMode { Brawl, Lab }
    [HideInInspector] public SceneMode nextSceneMode = SceneMode.Brawl;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            nextSceneMode = SceneMode.Brawl; // default mode
        }
        else
        {
            Destroy(gameObject);
        }
    }
}