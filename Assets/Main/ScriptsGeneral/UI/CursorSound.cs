using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CursorSound : MonoBehaviour
{
    public AudioSource audioSource;
    public float minMoveThreshold = 0.1f;

    [Header("Disable Cursor Sound In These Scenes")]
    public List<string> disableInScenes = new List<string>();

    private Vector3 lastMousePos;
    private bool sceneAllowsSound = true;

    void Start()
    {
        lastMousePos = Input.mousePosition;
        audioSource.loop = true;

        CheckScene();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (!sceneAllowsSound)
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
            return;
        }

        Vector3 delta = Input.mousePosition - lastMousePos;

        if (delta.magnitude > minMoveThreshold)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Pause();
        }

        lastMousePos = Input.mousePosition;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckScene();
    }

    void CheckScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;

        sceneAllowsSound = !disableInScenes.Contains(currentScene);

        if (!sceneAllowsSound && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}