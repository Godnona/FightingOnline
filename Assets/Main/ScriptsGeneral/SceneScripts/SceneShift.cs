using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneShift : MonoBehaviour
{
    [Header("Type the exact Scene Name here")]
    public string sceneName; // Set this in the Inspector

    // Call this method to load the scene
    public void LoadScene()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty! Set a valid scene name in the inspector.");
        }
    }

    // Optional: Load scene by key press for testing
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadScene();
        }
    }
}