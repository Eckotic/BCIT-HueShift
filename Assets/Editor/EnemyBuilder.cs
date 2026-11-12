using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menu: HueShift > Build Enemies
// Creates (or updates) the three EnemyData assets for the run and puts them, in order,
// into the open scene's TurnManager Encounters list. Safe to run again.
public static class EnemyBuilder
{
    private const string ArtRoot = "Assets/HueShift_Entity_Visuals 1";
    private const string DataFolder = "Assets/Data/Enemies";

    [MenuItem("HueShift/Build Enemies")]
    public static void Build()
    {
        MakeSprites($"{ArtRoot}/Enemy_Triangle");
        MakeSprites($"{ArtRoot}/Enemy_Circle");

        if (!AssetDatabase.IsValidFolder("Assets/Data"))
        {
            AssetDatabase.CreateFolder("Assets", "Data");
        }
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets/Data", "Enemies");
        }

        // Square: Red, attacks get +3. White fill tinted red, plus the outline.
        EnemyData square = MakeEnemy("Enemy_Square", "Square", 40, CardColor.Red,
            Load($"{ArtRoot}/Enemy_Square/Enemy_Square_Base_Fill.png"),
            Load($"{ArtRoot}/Enemy_Square/Enemy_Square_Base_Outline.png"), true,
            new EnemyMove(EnemyMoveType.Attack, 6),
            new EnemyMove(EnemyMoveType.Attack, 6),
            new EnemyMove(EnemyMoveType.Block, 8));

        // Triangle: Yellow, does every move twice. Art is already coloured.
        EnemyData triangle = MakeEnemy("Enemy_Triangle", "Triangle", 35, CardColor.Yellow,
            Load($"{ArtRoot}/Enemy_Triangle/Enemy_Triangle_Yellow.png"), null, false,
            new EnemyMove(EnemyMoveType.Attack, 4),
            new EnemyMove(EnemyMoveType.Block, 5));

        // Circle (boss): Blue, blocks get +3. Art is already coloured.
        EnemyData circle = MakeEnemy("Enemy_Circle", "Circle", 60, CardColor.Blue,
            Load($"{ArtRoot}/Enemy_Circle/Enemy_Circle_Blue.png"), null, false,
            new EnemyMove(EnemyMoveType.Attack, 10),
            new EnemyMove(EnemyMoveType.Block, 10),
            new EnemyMove(EnemyMoveType.Attack, 6));

        AssetDatabase.SaveAssets();

        FillEncounters(new List<EnemyData> { square, triangle, circle });
    }

    private static void MakeSprites(string folder)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || importer.textureType == TextureImporterType.Sprite)
            {
                continue;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            Debug.Log($"EnemyBuilder: set {path} to Sprite.");
        }
    }

    private static Sprite Load(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            Debug.LogError($"EnemyBuilder: no sprite at {path}.");
        }
        return sprite;
    }

    private static EnemyData MakeEnemy(string fileName, string displayName, int maxHealth, CardColor color,
        Sprite body, Sprite outline, bool tint, params EnemyMove[] moves)
    {
        string path = $"{DataFolder}/{fileName}.asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.enemyName = displayName;
        data.maxHealth = maxHealth;
        data.color = color;
        data.bodySprite = body;
        data.outlineSprite = outline;
        data.tintBody = tint;
        data.moves = new List<EnemyMove>(moves);
        EditorUtility.SetDirty(data);

        Debug.Log($"EnemyBuilder: {path} ({displayName}, {color}, {maxHealth} HP, {moves.Length} moves).");
        return data;
    }

    private static void FillEncounters(List<EnemyData> run)
    {
        TurnManager turnManager = Object.FindFirstObjectByType<TurnManager>();
        if (turnManager == null)
        {
            Debug.LogWarning("EnemyBuilder: enemies built, but no TurnManager in the open scene. Open GamePlayScene and run it again.");
            return;
        }

        SerializedObject turnData = new SerializedObject(turnManager);
        SerializedProperty list = turnData.FindProperty("encounters");
        list.ClearArray();
        for (int i = 0; i < run.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = run[i];
        }
        turnData.ApplyModifiedProperties(); // undoable with Ctrl+Z
        EditorSceneManager.MarkSceneDirty(turnManager.gameObject.scene);

        Debug.Log("EnemyBuilder: run set to Square, Triangle, Circle. Save the scene to keep it.");
    }
}
