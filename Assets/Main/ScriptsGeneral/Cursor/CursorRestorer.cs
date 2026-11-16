using UnityEngine;
using UnityEngine.SceneManagement;

public static class CursorRestorer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        // Run immediately for the initial scene loaded when starting the game/editor
        RestoreCursorIfNeeded(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreCursorIfNeeded(scene);
    }

    private static void RestoreCursorIfNeeded(Scene scene)
    {
        // When transitioning away from the gameplay scene (Map1) to menus or selection scenes, show the cursor.
        if (scene.name != "Map1")
        {
            Cursor.lockState = CursorLockMode.None;

            // Also make sure any custom cursor in the loaded scene is activated
            CustomCursorFollow customCursor = Object.FindObjectOfType<CustomCursorFollow>(true);
            GameObject pointer = null;

            if (customCursor != null)
            {
                pointer = customCursor.gameObject;
            }
            else
            {
                // Fallback search for a GameObject named CURSOR_POINTER in case it doesn't have the script directly
                pointer = FindInactiveObjectByName("CURSOR_POINTER");
            }

            if (pointer != null)
            {
                // Recursively activate hierarchy to ensure it's active even if parents are inactive
                ActivateHierarchy(pointer);
                // Hide system cursor because we are using the custom cursor
                Cursor.visible = false;
            }
            else
            {
                // Show system cursor because there is no custom cursor in the scene
                Cursor.visible = true;
            }
        }
    }

    // Helper to recursively activate a GameObject and all its parents
    private static void ActivateHierarchy(GameObject obj)
    {
        if (obj == null) return;
        obj.SetActive(true);
        if (obj.transform.parent != null)
        {
            ActivateHierarchy(obj.transform.parent.gameObject);
        }
    }

    // Helper to find inactive root or child object by name in the active scene
    private static GameObject FindInactiveObjectByName(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.name == name && !obj.activeInHierarchy)
            {
                // Ensure it is in the current active scene, not an asset database prefab
                #if UNITY_EDITOR
                if (UnityEditor.AssetDatabase.Contains(obj)) continue;
                #endif
                return obj;
            }
        }
        return null;
    }
}
