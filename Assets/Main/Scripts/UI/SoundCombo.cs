using UnityEngine;

public class SoundCombo : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (audioSource == null || clip == null)
            return;

        if (!audioSource.isActiveAndEnabled)
            return;

        audioSource.PlayOneShot(clip, volume);
    }
}
