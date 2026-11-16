using UnityEngine;

public class SceneObjectDeactivator : MonoBehaviour
{
    [Header("Assign the objects you want to deactivate on scene load")]
    public GameObject[] objectsToDeactivate;

    private void Awake()
    {
        // Loop through all objects and deactivate them
        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}