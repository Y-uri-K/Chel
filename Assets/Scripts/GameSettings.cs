using System;
using UnityEngine;

public static class GameSettings
{
    const string MasterVolumeKey = "settings_master_volume";
    const string MusicVolumeKey = "settings_music_volume";
    const string SfxVolumeKey = "settings_sfx_volume";
    const string ResolutionIndexKey = "settings_resolution_index";

    public static readonly (int width, int height)[] Resolutions =
    {
        (1280, 720),
        (1366, 768),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
    };

    public static float MasterVolume { get; private set; } = 1f;
    public static float MusicVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static int ResolutionIndex { get; private set; }

    public static event Action SettingsChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        Load();
        Apply();
    }

    public static void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        ResolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, GetDefaultResolutionIndex());
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
        PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
        PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
        PlayerPrefs.SetInt(ResolutionIndexKey, ResolutionIndex);
        PlayerPrefs.Save();
    }

    public static void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        Save();
        Apply();
    }

    public static void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        Save();
        Apply();
    }

    public static void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        Save();
        Apply();
    }

    public static void SetResolutionIndex(int index)
    {
        ResolutionIndex = Mathf.Clamp(index, 0, Resolutions.Length - 1);
        Save();
        Apply();
    }

    public static void Apply()
    {
        AudioListener.volume = 1f;
        ApplyMusicSources();
        ApplyResolution();
        SettingsChanged?.Invoke();
    }

    public static float GetEffectiveMusicVolume()
    {
        return MasterVolume * MusicVolume;
    }

    public static float GetEffectiveSfxVolume()
    {
        return MasterVolume * SfxVolume;
    }

    public static string GetResolutionLabel(int index)
    {
        index = Mathf.Clamp(index, 0, Resolutions.Length - 1);
        var resolution = Resolutions[index];
        return $"{resolution.width} x {resolution.height}";
    }

    static int GetDefaultResolutionIndex()
    {
        int closestIndex = Resolutions.Length - 1;
        int closestScore = int.MaxValue;

        for (int i = 0; i < Resolutions.Length; i++)
        {
            int score = Mathf.Abs(Screen.currentResolution.width - Resolutions[i].width)
                + Mathf.Abs(Screen.currentResolution.height - Resolutions[i].height);

            if (score < closestScore)
            {
                closestScore = score;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    internal static void ApplyMusicSources()
    {
        float musicVolume = GetEffectiveMusicVolume();
        var musicSources = UnityEngine.Object.FindObjectsByType<MusicAudioSource>(FindObjectsSortMode.None);

        foreach (var musicSource in musicSources)
        {
            if (musicSource != null)
                musicSource.ApplyVolume(musicVolume);
        }
    }

    static void ApplyResolution()
    {
        var resolution = Resolutions[ResolutionIndex];
        Screen.SetResolution(
            resolution.width,
            resolution.height,
            Screen.fullScreenMode);
    }
}
