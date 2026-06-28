using UnityEngine;

public static class SfxPlayer
{
    static AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        EnsureSource();
    }

    static void EnsureSource()
    {
        if (source != null)
            return;

        var go = new GameObject("SfxPlayer");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
    }

    public static void Play(AudioClip clip)
    {
        if (clip == null)
            return;

        EnsureSource();
        source.PlayOneShot(clip, GameSettings.GetEffectiveSfxVolume());
    }
}
