using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Builds battle UI in code on the scene's Canvas (intent pill, Victory / Defeat screen),
// plus small helpers the HUD uses, so nothing has to be set up by hand in the scene.
public static class BattleUI
{
    public static Canvas FindCanvas()
    {
        Canvas fallback = null;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas)
            {
                continue;
            }
            if (canvas.name == "Canvas")
            {
                return canvas;
            }
            fallback = canvas;
        }
        return fallback;
    }

    public static RectTransform Rect(Transform parent, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public static Image Box(Transform parent, string name, Sprite sprite, Color color, bool sliced = true)
    {
        RectTransform rect = Rect(parent, name);
        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sliced && sprite != null ? Image.Type.Sliced : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color, FontStyles style)
    {
        RectTransform rect = Rect(parent, name);
        TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    public static void Stretch(RectTransform rect, float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    public static void Shadow(Graphic graphic, float distance = 6f, float alpha = 0.45f)
    {
        Shadow shadow = graphic.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, alpha);
        shadow.effectDistance = new Vector2(0f, -distance);
    }

    // Soft drop shadow under text (TMP underlay). Gives big titles depth.
    public static void TextShadow(TextMeshProUGUI tmp, float softness = 0.25f, float offsetY = -0.6f)
    {
        Material material = tmp.fontMaterial; // per-text copy, so other texts aren't affected
        material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
        material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
        material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, offsetY);
        material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, softness);
        tmp.fontMaterial = material;
    }

    public static Button StyledButton(Transform parent, string name, string label, Color color, Color labelColor, Vector2 size, float fontSize)
    {
        Image image = Box(parent, name, UIShapes.RoundedRect(24), color);
        image.raycastTarget = true;
        image.rectTransform.sizeDelta = size;
        Shadow(image);

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f);
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        colors.selectedColor = Color.white;
        button.colors = colors;

        TextMeshProUGUI text = Text(image.transform, "Label", label, fontSize, labelColor, FontStyles.Bold);
        text.characterSpacing = 4f;
        Stretch(text.rectTransform);
        image.gameObject.AddComponent<UIHover>();
        return button;
    }

    // The pill above the enemy that shows its next move. TurnManager moves the pill (the text's parent).
    public static TMP_Text CreateIntentLabel()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("BattleUI: no Canvas found, enemy intent won't be shown.");
            return null;
        }

        Image pill = Box(canvas.transform, "EnemyIntent", UIShapes.FramedPanel(20, 3), Color.white);
        pill.rectTransform.pivot = new Vector2(0.5f, 0f);
        Shadow(pill);
        HorizontalLayoutGroup layout = pill.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(26, 26, 10, 12);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = pill.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        TextMeshProUGUI label = Text(pill.transform, "Text", "", 34f, UIShapes.TextLight, FontStyles.Bold);
        label.characterSpacing = 2f;
        return label;
    }

    // Victory / Defeat: dims the fight, pops in a card with a big title, a line of text and a button.
    public static void ShowEndScreen(string title, string subtitle, Color accent, string buttonLabel, UnityAction onClick)
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning($"BattleUI: no Canvas found. {title}: {subtitle}");
            return;
        }

        // Dim layer (also blocks clicks to the fight underneath).
        Image dim = Box(canvas.transform, "EndScreen", null, new Color(0.03f, 0.04f, 0.07f, 0.82f), false);
        dim.raycastTarget = true;
        Stretch(dim.rectTransform);
        dim.transform.SetAsLastSibling();
        CanvasGroup group = dim.gameObject.AddComponent<CanvasGroup>();
        UITween.FadeIn(group, 0.35f);

        // Coloured glow behind the card.
        Image glow = Box(dim.transform, "Glow", UIShapes.Glow(256), new Color(accent.r, accent.g, accent.b, 0.35f), false);
        glow.rectTransform.sizeDelta = new Vector2(1500f, 1100f);

        // Card: accent border with the dark panel inside.
        Image border = Box(dim.transform, "Card", UIShapes.RoundedRect(32), accent);
        border.rectTransform.sizeDelta = new Vector2(840f, 500f);
        Shadow(border, 14f, 0.6f);
        Image panel = Box(border.transform, "Panel", UIShapes.RoundedRect(28), UIShapes.Hex("1C1F2B"));
        Stretch(panel.rectTransform, 6f);

        TextMeshProUGUI titleText = Text(panel.transform, "Title", title.ToUpperInvariant(), 140f, Color.white, FontStyles.Bold);
        titleText.characterSpacing = 6f;
        titleText.enableVertexGradient = true;
        titleText.colorGradient = new VertexGradient(Color.white, Color.white, accent, accent);
        titleText.rectTransform.sizeDelta = new Vector2(800f, 180f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 110f);
        TextShadow(titleText);

        Image divider = Box(panel.transform, "Divider", UIShapes.RoundedRect(4), accent);
        divider.rectTransform.sizeDelta = new Vector2(180f, 8f);
        divider.rectTransform.anchoredPosition = new Vector2(0f, 10f);

        TextMeshProUGUI subtitleText = Text(panel.transform, "Subtitle", subtitle, 40f, UIShapes.TextMuted, FontStyles.Normal);
        subtitleText.rectTransform.sizeDelta = new Vector2(780f, 70f);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, -50f);

        Button button = StyledButton(panel.transform, "Button", buttonLabel.ToUpperInvariant(), accent, UIShapes.Hex("14161F"), new Vector2(380f, 100f), 38f);
        ((RectTransform)button.transform).anchoredPosition = new Vector2(0f, -160f);
        button.onClick.AddListener(onClick);

        UITween.PopIn(border.transform, 0.45f, 0.6f, 0.1f);
    }
}
