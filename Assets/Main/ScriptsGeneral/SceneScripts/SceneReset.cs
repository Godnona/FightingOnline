using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneReset : MonoBehaviour
{
    [Header("Key to Reset Scene")]
    public KeyCode resetKey = KeyCode.Tab;

    void Update()
    {
        // Check if the reset key (TAB) is pressed
        if (Input.GetKeyDown(resetKey))
        {
            RestartScene();
        }
    }

    void RestartScene()
    {
        // Get the currently active scene
        Scene currentScene = SceneManager.GetActiveScene();
        // Reload the scene
        SceneManager.LoadScene(currentScene.buildIndex);
        Debug.Log("Scene restarted: " + currentScene.name);
    }
}
