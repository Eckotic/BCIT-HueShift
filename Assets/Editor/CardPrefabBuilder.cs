using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu: HueShift > Build Card Prefabs
// Builds the polished Strike / Defend / Battery card prefabs from the card art,
// then fills the open scene's DeckManager with the starter deck. Safe to run again:
// it overwrites the same three prefabs (same GUIDs), so references stay intact.
public static class CardPrefabBuilder
{
    private const string ArtFolder = "Assets/HueShift_Entity_Visuals 1/HueShift_Card_Visuals";
    private const string OutParent = "Assets/Prefabs";
    private const string OutFolder = "Assets/Prefabs/Cards";

    // The art is 1000 x 1200, so the card keeps that 5:6 shape.
    private const float CardWidth = 250f;
    private const float CardHeight = 300f;

    private const int StrikeCount = 5;
    private const int DefendCount = 4;
    private const int BatteryCount = 1;

    [MenuItem("HueShift/Build Card Prefabs")]
    public static void Build()
    {
        MakeArtSprites();

        if (!AssetDatabase.IsValidFolder(OutFolder))
        {
            AssetDatabase.CreateFolder(OutParent, "Cards");
        }

        GameObject strike = BuildCard<AttackExample>("Card_Strike", "Strike", 1, CardColor.Red, "Card_Red");
        GameObject defend = BuildCard<DefendExample>("Card_Defend", "Defend", 1, CardColor.Blue, "Card_Blue");
        GameObject battery = BuildCard<BatteryCard>("Card_Battery", "Battery", 0, CardColor.Yellow, "Card_Yellow");

        AssetDatabase.SaveAssets();

        if (strike == null || defend == null || battery == null)
        {
            Debug.LogError("CardPrefabBuilder: a card prefab failed to build, see the errors above. Deck not changed.");
            return;
        }

        FillStarterDeck(strike, defend, battery);
    }

    // The PNGs import as plain textures by default; a UI Image needs them as Sprites.
    private static void MakeArtSprites()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
        foreach (string guid in guids)
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
            Debug.Log($"CardPrefabBuilder: set {path} to Sprite.");
        }
    }

    private static GameObject BuildCard<T>(string prefabName, string displayName, int cost, CardColor color, string artName) where T : Card
    {
        string artPath = $"{ArtFolder}/{artName}.png";
        Sprite art = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
        if (art == null)
        {
            Debug.LogError($"CardPrefabBuilder: no sprite at {artPath}.");
            return null;
        }

        GameObject root = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        try
        {
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(CardWidth, CardHeight);

            Image image = root.GetComponent<Image>();
            image.sprite = art;
            image.preserveAspect = true;

            Button button = root.GetComponent<Button>();
            button.targetGraphic = image;

            T card = root.AddComponent<T>();
            SerializedObject cardData = new SerializedObject(card);
            cardData.FindProperty("cost").intValue = cost;
            cardData.FindProperty("color").enumValueIndex = (int)color;
            cardData.FindProperty("cardName").stringValue = displayName;
            cardData.ApplyModifiedPropertiesWithoutUndo();

            // Label boxes, measured on the 1000 x 1200 art (0,0 = bottom-left, 1,1 = top-right).
            TMP_Text costText = MakeLabel(root.transform, "CostText",
                new Vector2(0.152f, 0.819f), new Vector2(0.292f, 0.936f),
                new Color32(30, 30, 30, 255), FontStyles.Bold, 16f, 46f);
            TMP_Text nameText = MakeLabel(root.transform, "NameText",
                new Vector2(0.29f, 0.452f), new Vector2(0.71f, 0.513f),
                new Color32(30, 30, 30, 255), FontStyles.Bold, 10f, 26f);
            TMP_Text descriptionText = MakeLabel(root.transform, "DescriptionText",
                new Vector2(0.26f, 0.16f), new Vector2(0.74f, 0.42f),
                Color.white, FontStyles.Normal, 10f, 24f);

            CardView view = root.AddComponent<CardView>();
            SerializedObject viewData = new SerializedObject(view);
            viewData.FindProperty("costText").objectReferenceValue = costText;
            viewData.FindProperty("nameText").objectReferenceValue = nameText;
            viewData.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            viewData.ApplyModifiedPropertiesWithoutUndo();
            view.Refresh(); // so the prefab shows its text in the editor too

            string prefabPath = $"{OutFolder}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool saved);
            if (!saved)
            {
                Debug.LogError($"CardPrefabBuilder: couldn't save {prefabPath}.");
                return null;
            }
            Debug.Log($"CardPrefabBuilder: built {prefabPath} ({displayName}, cost {cost}, {color}).");
            return prefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static TMP_Text MakeLabel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Color color, FontStyles style, float minSize, float maxSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = minSize;
        text.fontSizeMax = maxSize;
        text.raycastTarget = false; // clicks go through to the card's Button
        return text;
    }

    private static void FillStarterDeck(GameObject strike, GameObject defend, GameObject battery)
    {
        DeckManager deckManager = Object.FindFirstObjectByType<DeckManager>();
        if (deckManager == null)
        {
            Debug.LogWarning("CardPrefabBuilder: prefabs built, but no DeckManager in the open scene. Open GamePlayScene and run the menu item again to fill the deck.");
            return;
        }

        List<GameObject> starter = new List<GameObject>();
        for (int i = 0; i < StrikeCount; i++) starter.Add(strike);
        for (int i = 0; i < DefendCount; i++) starter.Add(defend);
        for (int i = 0; i < BatteryCount; i++) starter.Add(battery);

        SerializedObject deckData = new SerializedObject(deckManager);
        SerializedProperty list = deckData.FindProperty("cardPrefabs");
        list.ClearArray();
        for (int i = 0; i < starter.Count; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = starter[i];
        }
        SerializedProperty back = deckData.FindProperty("cardBackSprite");
        if (back != null)
        {
            back.objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Card_Base_Back.png");
        }
        deckData.ApplyModifiedProperties(); // undoable with Ctrl+Z
        EditorSceneManager.MarkSceneDirty(deckManager.gameObject.scene);

        Debug.Log($"CardPrefabBuilder: deck set to {StrikeCount} Strike, {DefendCount} Defend, {BatteryCount} Battery. Save the scene to keep it.");
    }
}
