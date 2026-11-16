using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneButtonHandler : MonoBehaviour
{
    public string sceneName; // Set this in Inspector

    public void LoadLabScene()
    {
        if (SceneModeManager.Instance != null)
            SceneModeManager.Instance.nextSceneMode = SceneModeManager.SceneMode.Lab;

        SceneManager.LoadScene(sceneName);
    }

    public void LoadBrawlScene()
    {
        if (SceneModeManager.Instance != null)
            SceneModeManager.Instance.nextSceneMode = SceneModeManager.SceneMode.Brawl;

        SceneManager.LoadScene(sceneName);
    }
}