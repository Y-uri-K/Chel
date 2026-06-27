using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicAudioSource : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] float baseVolume = 1f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyVolume(GameSettings.MasterVolume * GameSettings.MusicVolume);
        GameSettings.SettingsChanged += HandleSettingsChanged;
    }

    void OnDestroy()
    {
        GameSettings.SettingsChanged -= HandleSettingsChanged;
    }

    void HandleSettingsChanged()
    {
        ApplyVolume(GameSettings.MasterVolume * GameSettings.MusicVolume);
    }

    public void ApplyVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = baseVolume * volume;
    }
}
