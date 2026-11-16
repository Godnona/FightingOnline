using UnityEngine;

[System.Serializable]
public class SoundEntry
{
    public string soundName;
    public AudioClip clip;
    [Range(0f, 1f)] public float volume = 1f;
}

public class SoundController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundEntry[] sounds;

    // Auto-fill the local audio source when the component is added.
    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    // Cache the audio source once for animation event playback.
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    // Play one sound by name, similar to AnimationSFXPlayer in NewScripts.
    public void PlaySound(string soundName)
    {
        if (audioSource == null || string.IsNullOrWhiteSpace(soundName))
            return;

        for (int i = 0; i < sounds.Length; i++)
        {
            SoundEntry sound = sounds[i];
            if (sound != null && sound.soundName == soundName && sound.clip != null)
            {
                audioSource.PlayOneShot(sound.clip, sound.volume);
                return;
            }
        }
    }

    // Stop every sound that is currently playing on this source.
    public void StopAllSounds()
    {
        if (audioSource != null)
            audioSource.Stop();
    }
}
