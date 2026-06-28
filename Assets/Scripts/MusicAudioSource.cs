using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicAudioSource : MonoBehaviour
{
    AudioSource audioSource;
    [SerializeField] float baseVolume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureWorldMusicSources()
    {
        var audioSources = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in audioSources)
        {
            if (source == null || source.gameObject.name != "World")
                continue;

            if (source.GetComponent<MusicAudioSource>() != null)
                continue;

            source.gameObject.AddComponent<MusicAudioSource>();
        }

        GameSettings.ApplyMusicSources();
    }

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ApplyVolume(GameSettings.GetEffectiveMusicVolume());
        GameSettings.SettingsChanged += HandleSettingsChanged;
    }

    void OnDestroy()
    {
        GameSettings.SettingsChanged -= HandleSettingsChanged;
    }

    void HandleSettingsChanged()
    {
        ApplyVolume(GameSettings.GetEffectiveMusicVolume());
    }

    public void ApplyVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = baseVolume * volume;
    }
}
