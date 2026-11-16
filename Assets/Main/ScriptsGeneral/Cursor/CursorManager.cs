using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour
{
    [Tooltip("Drag the Custom Cursor GameObject here. If null, will attempt to find automatically.")]
    [SerializeField] private GameObject customCursorObject;

    void Start()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (customCursorObject == null)
        {
            CustomCursorFollow customCursor = FindObjectOfType<CustomCursorFollow>(true);
            if (customCursor != null)
            {
                customCursorObject = customCursor.gameObject;
            }
        }

        if (sceneName == "Map1")
        {
            // Gameplay scene: Hide system cursor and deactivate custom cursor on start
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None; 

            if (customCursorObject != null)
            {
                customCursorObject.SetActive(false);
            }
        }
        else
        {
            // Menu / Selection scene: Hide system cursor but activate custom cursor
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.None; 

            if (customCursorObject != null)
            {
                customCursorObject.SetActive(true);
            }
        }
    }
}