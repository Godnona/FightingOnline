using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ToggleButton : MonoBehaviour
{
    [Header("Assign the button that triggers the toggle")]
    public Button toggleButton;

    [Header("GameObjects to Activate when button is clicked")]
    public List<GameObject> objectsToActivate;

    [Header("GameObjects to Deactivate when button is clicked")]
    public List<GameObject> objectsToDeactivate;

    // Keeps track of toggle state
    private bool isActiveState = false;

    void Start()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleObjects);
        }
    }

    void ToggleObjects()
    {
        if (isActiveState)
        {
            // Switch back
            SetObjects(objectsToActivate, false);
            SetObjects(objectsToDeactivate, true);
        }
        else
        {
            SetObjects(objectsToActivate, true);
            SetObjects(objectsToDeactivate, false);
        }

        isActiveState = !isActiveState;
    }

    void SetObjects(List<GameObject> objects, bool state)
    {
        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}