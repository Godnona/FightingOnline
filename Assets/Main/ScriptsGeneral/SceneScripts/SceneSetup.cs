using UnityEngine;

public class SceneSetup : MonoBehaviour
{
    public GameObject[] labObjectsToDeactivate;

    private void Awake()
    {
        var manager = SceneModeManager.Instance;
        if (manager == null) manager = Object.FindAnyObjectByType<SceneModeManager>();

        if (manager == null)
        {
            Debug.LogError("SceneModeManager is NULL");
            return;
        }

        var mode = manager.nextSceneMode;

        Debug.Log("MODE DETECTED: " + mode);

        foreach (GameObject obj in labObjectsToDeactivate)
        {
            if (obj == null) continue;

            // Disable only for Lab mode, force enable otherwise
            obj.SetActive(mode == SceneModeManager.SceneMode.Lab ? false : true);
        }
    }
}