using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class UiFunctionEndGame : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string mainCharacterSelectSceneName = "CharacterUpgradeScene";

    [Header("Cursor")]
    [Tooltip("Optional custom cursor object. If left empty, the script will try to find one.")]
    [SerializeField] private GameObject customCursorObject;
    [SerializeField] private KeyCode showMouseKey = KeyCode.Escape;
    [SerializeField] private int hideMouseButton = 0;
    [SerializeField] private bool hideMouseOnClick = true;

    private void Awake()
    {
        ResolveCustomCursor();
    }

    private void Update()
    {
        if (Input.GetKeyDown(showMouseKey))
            ShowMouse();

        if (hideMouseOnClick && Input.GetMouseButtonDown(hideMouseButton) && !IsPointerOverUi())
            HideMouse();
    }

    public void MainMenu()
    {
        ShowMouse();
        SceneManager.LoadScene(mainCharacterSelectSceneName);
    }

    public void Restart()
    {
        ShowMouse();
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }

    public void ShowMouse()
    {
        ResolveCustomCursor();
        Cursor.lockState = CursorLockMode.None;

        if (customCursorObject != null)
        {
            customCursorObject.SetActive(true);
            Cursor.visible = false;
            return;
        }

        Cursor.visible = true;
    }

    public void HideMouse()
    {
        ResolveCustomCursor();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        if (customCursorObject != null)
            customCursorObject.SetActive(false);
    }

    private void ResolveCustomCursor()
    {
        if (customCursorObject != null)
            return;

        CustomCursorFollow customCursor = FindFirstObjectByType<CustomCursorFollow>(FindObjectsInactive.Include);
        if (customCursor != null)
        {
            customCursorObject = customCursor.gameObject;
            return;
        }

        GameObject pointer = GameObject.Find("CURSOR_POINTER");
        if (pointer != null)
            customCursorObject = pointer;
    }

    private bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
