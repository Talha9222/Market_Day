using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Music and SFX. Clips are looked up by name under Resources/Audio, so dropping a file
/// called e.g. "buy.wav" into Assets/Game/Resources/Audio wires it up with no Inspector
/// work and no prefab - which matters because every screen here is built from code.
///
/// A missing clip is a silent no-op, so the game runs fine with no audio at all.
/// </summary>
public static class MDAudio
{
    // Drop a clip with one of these names into Assets/Game/Resources/Audio to hear it.
    public const string Music = "theme";
    public const string Click = "click";
    public const string Investigate = "investigate";
    public const string Buy = "buy";
    public const string Sell = "sell";
    public const string MarketEvent = "event";
    public const string Win = "win";
    public const string Lose = "lose";

    const string MusicVolKey = "MD_MusicVolume";
    const string SfxVolKey = "MD_SfxVolume";
    const string ResourceFolder = "Audio/";

    static AudioSource _music, _sfx;
    static readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();

    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicVolKey, 0.6f);
        set
        {
            PlayerPrefs.SetFloat(MusicVolKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            if (_music != null) _music.volume = Mathf.Clamp01(value);
        }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxVolKey, 1f);
        set
        {
            PlayerPrefs.SetFloat(SfxVolKey, Mathf.Clamp01(value));
            PlayerPrefs.Save();
        }
    }

    public static bool MusicOn => MusicVolume > 0.001f;
    public static bool SfxOn => SfxVolume > 0.001f;

    public static void ToggleMusic() => MusicVolume = MusicOn ? 0f : 0.6f;
    public static void ToggleSfx() => SfxVolume = SfxOn ? 0f : 1f;

    /// <summary>
    /// Two AudioSources on one persistent object. No custom MonoBehaviour is needed, so
    /// there is nothing extra to keep in a scene.
    /// </summary>
    static void Ensure()
    {
        if (_sfx != null) return;

        var go = new GameObject("MDAudio");
        Object.DontDestroyOnLoad(go);

        _music = go.AddComponent<AudioSource>();
        _music.loop = true;
        _music.playOnAwake = false;
        _music.volume = MusicVolume;

        _sfx = go.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
    }

    static AudioClip Load(string name)
    {
        if (_clips.TryGetValue(name, out var cached)) return cached;   // caches misses too
        var clip = Resources.Load<AudioClip>(ResourceFolder + name);
        _clips[name] = clip;
        return clip;
    }

    /// <summary>One-shot sound effect. Safe to call for a clip that does not exist yet.</summary>
    public static void Play(string name, float volumeScale = 1f)
    {
        if (!SfxOn) return;
        var clip = Load(name);
        if (clip == null) return;
        Ensure();
        _sfx.PlayOneShot(clip, Mathf.Clamp01(SfxVolume * volumeScale));
    }

    public static void PlayClick() => Play(Click, 0.7f);

    /// <summary>Starts the loop if it is not already the track that is playing.</summary>
    public static void PlayMusic(string name = Music)
    {
        var clip = Load(name);
        if (clip == null) return;
        Ensure();
        if (_music.clip == clip && _music.isPlaying)
        {
            _music.volume = MusicVolume;
            return;
        }
        _music.clip = clip;
        _music.volume = MusicVolume;
        _music.Play();
    }

    public static void StopMusic()
    {
        if (_music != null) _music.Stop();
    }
}
