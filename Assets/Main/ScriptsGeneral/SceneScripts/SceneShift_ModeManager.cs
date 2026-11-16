using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneShift_ModeManager : MonoBehaviour
{
    public string sceneName; // Set this in inspector

    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }
}