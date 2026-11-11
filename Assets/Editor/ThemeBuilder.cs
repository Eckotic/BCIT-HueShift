using UnityEditor;
using UnityEngine;

// Menu: HueShift > Build Theme
// Creates Assets/Resources/HueShiftTheme.asset and fills it with sounds from the Brackeys 2D Mega Pack.
// Safe to run again. Swap any clip in the Inspector afterwards.
public static class ThemeBuilder
{
    private const string SoundFolder = "Assets/Brackeys/2D Mega Pack/Sounds";
    private const string ThemePath = "Assets/Resources/HueShiftTheme.asset";

    [MenuItem("HueShift/Build Theme")]
    public static void Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        HueShiftTheme theme = AssetDatabase.LoadAssetAtPath<HueShiftTheme>(ThemePath);
        if (theme == null)
        {
            theme = ScriptableObject.CreateInstance<HueShiftTheme>();
            AssetDatabase.CreateAsset(theme, ThemePath);
        }

        theme.buttonHover = Clip("ButtonHover");
        theme.buttonPress = Clip("ButtonPress");
        theme.cardHover = Clip("ButtonHover");
        theme.cardDraw = Clip("Click");
        theme.cardPlay = Clip("Whoosh");
        theme.cardDiscard = Clip("Whoosh");
        theme.hit = Clip("Hit");
        theme.blockGain = Clip("ButtonPress");
        theme.enemyAttack = Clip("GruntVoice01");
        theme.enemyDeath = Clip("Explosion");
        theme.enemySpawn = Clip("Spawn");
        theme.turnStart = Clip("Click");
        theme.victory = Clip("Bonus");
        theme.defeat = Clip("GameOver");

        EditorUtility.SetDirty(theme);
        AssetDatabase.SaveAssets();
        Selection.activeObject = theme;
        Debug.Log($"ThemeBuilder: {ThemePath} built with Brackeys sounds. Select it to swap clips or change the volume.");
    }

    private static AudioClip Clip(string name)
    {
        string path = $"{SoundFolder}/{name}.wav";
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"ThemeBuilder: no sound at {path}.");
        }
        return clip;
    }
}
