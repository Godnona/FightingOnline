using UnityEngine;
using UnityEngine.InputSystem;

public class SimulationMenuManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference pauseAction;

    [Header("UI")]
    [SerializeField] private GameObject pauseMenuCanvas;
    [SerializeField] private GameObject moveListCanvas;

    [Header("Scene Loader")]
    [SerializeField] private GameFlowSceneLoader sceneLoader;

    private bool isPaused;

    private void Awake()
    {
        if (sceneLoader == null)
        {
            sceneLoader = GameFlowSceneLoader.EnsureExists();
        }

        pauseMenuCanvas.SetActive(false);
        moveListCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPausePressed;
            pauseAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (pauseAction != null)
        {
            pauseAction.action.performed -= OnPausePressed;
            pauseAction.action.Disable();
        }
    }

    private void OnPausePressed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;

        pauseMenuCanvas.SetActive(true);
        moveListCanvas.SetActive(false);

        Time.timeScale = 0f;
    }

    public void Resume()
    {
        isPaused = false;

        pauseMenuCanvas.SetActive(false);
        moveListCanvas.SetActive(false);

        Time.timeScale = 1f;
    }

    public void OpenMoveList()
    {
        pauseMenuCanvas.SetActive(false);
        moveListCanvas.SetActive(true);
    }

    public void CloseMoveList()
    {
        moveListCanvas.SetActive(false);
        pauseMenuCanvas.SetActive(true);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;

        isPaused = false;

        GameSession.EnsureExists().ResetSession();

        sceneLoader.LoadMainMenuScene();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}