using System;
using UnityEngine;

// One-line sound effects: Sfx.Play(t => t.hit). Clips come from the HueShiftTheme asset.
public static class Sfx
{
    private static AudioSource source;

    public static void Play(Func<HueShiftTheme, AudioClip> pick, float volume = 1f, float pitchJitter = 0.06f)
    {
        HueShiftTheme theme = HueShiftTheme.Get();
        if (theme == null)
        {
            return;
        }
        AudioClip clip = pick(theme);
        if (clip == null)
        {
            return;
        }

        if (source == null)
        {
            GameObject go = new GameObject("Sfx");
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }
        source.pitch = 1f + UnityEngine.Random.Range(-pitchJitter, pitchJitter);
        source.PlayOneShot(clip, volume * theme.volume);
    }
}
