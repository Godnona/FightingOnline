using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PersistentMusicManager : MonoBehaviour
{
    public static PersistentMusicManager Instance;

    [Header("Audio")]
    public AudioSource musicSource;

    [Header("Allowed Scenes (Music Plays Here)")]
    public List<string> allowedScenes = new List<string>();

    private void Awake()
    {
        // Singleton logic (prevents duplicates)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (allowedScenes.Contains(scene.name))
        {
            if (!musicSource.isPlaying)
                musicSource.Play();
        }
        else
        {
            musicSource.Stop();
        }
    }
}