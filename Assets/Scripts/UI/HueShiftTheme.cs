using UnityEngine;

// Sounds (and other look settings) for the battle, in one asset the code loads by itself:
// Assets/Resources/HueShiftTheme.asset. Made by HueShift > Build Theme; swap any clip in the Inspector.
[CreateAssetMenu(menuName = "HueShift/Theme", fileName = "HueShiftTheme")]
public class HueShiftTheme : ScriptableObject
{
    [Header("UI")]
    public AudioClip buttonHover;
    public AudioClip buttonPress;
    public AudioClip cardHover;

    [Header("Cards")]
    public AudioClip cardDraw;
    public AudioClip cardPlay;
    public AudioClip cardDiscard;

    [Header("Battle")]
    public AudioClip hit;
    public AudioClip blockGain;
    public AudioClip enemyAttack;
    public AudioClip enemyDeath;
    public AudioClip enemySpawn;
    public AudioClip turnStart;
    public AudioClip victory;
    public AudioClip defeat;

    [Range(0f, 1f)] public float volume = 0.7f;

    private static HueShiftTheme cached;
    private static bool looked;

    // The theme asset, or null if it hasn't been built yet (sounds are then silently skipped).
    public static HueShiftTheme Get()
    {
        if (!looked || cached == null)
        {
            cached = Resources.Load<HueShiftTheme>("HueShiftTheme");
            looked = true;
        }
        return cached;
    }
}
