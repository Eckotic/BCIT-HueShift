using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Builds the battle's runtime UI (enemy intent label, Victory / Defeat screen) on the scene's Canvas,
// so nothing has to be set up by hand in the scene.
public static class BattleUI
{
    private static Canvas FindCanvas()
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

    private static TextMeshProUGUI MakeText(Transform parent, string name, string text, float size, Color color, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
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
        return tmp;
    }

    // A label that TurnManager moves above the enemy every frame.
    public static TMP_Text CreateIntentLabel()
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning("BattleUI: no Canvas found, enemy intent won't be shown.");
            return null;
        }

        TextMeshProUGUI label = MakeText(canvas.transform, "EnemyIntent", "", 34f, Color.white, FontStyles.Bold);
        label.rectTransform.sizeDelta = new Vector2(420f, 110f);
        label.outlineWidth = 0.2f;
        label.outlineColor = new Color32(0, 0, 0, 255);
        return label;
    }

    // Full-screen Victory / Defeat panel with one button. Blocks clicks to everything under it.
    public static void ShowEndScreen(string title, string subtitle, Color titleColor, string buttonLabel, UnityAction onClick)
    {
        Canvas canvas = FindCanvas();
        if (canvas == null)
        {
            Debug.LogWarning($"BattleUI: no Canvas found. {title}: {subtitle}");
            return;
        }

        GameObject panel = new GameObject("EndScreen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetAsLastSibling();
        RectTransform panelRect = (RectTransform)panel.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

        TextMeshProUGUI titleText = MakeText(panel.transform, "Title", title, 110f, titleColor, FontStyles.Bold);
        titleText.rectTransform.sizeDelta = new Vector2(1200f, 160f);
        titleText.rectTransform.anchoredPosition = new Vector2(0f, 120f);

        TextMeshProUGUI subtitleText = MakeText(panel.transform, "Subtitle", subtitle, 40f, Color.white, FontStyles.Normal);
        subtitleText.rectTransform.sizeDelta = new Vector2(1200f, 80f);
        subtitleText.rectTransform.anchoredPosition = new Vector2(0f, 10f);

        GameObject buttonObject = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = (RectTransform)buttonObject.transform;
        buttonRect.sizeDelta = new Vector2(340f, 96f);
        buttonRect.anchoredPosition = new Vector2(0f, -130f);
        Image buttonImage = buttonObject.GetComponent<Image>();
        buttonImage.color = Color.white;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        TextMeshProUGUI buttonText = MakeText(buttonObject.transform, "Label", buttonLabel, 40f, new Color32(30, 30, 30, 255), FontStyles.Bold);
        buttonText.rectTransform.anchorMin = Vector2.zero;
        buttonText.rectTransform.anchorMax = Vector2.one;
        buttonText.rectTransform.offsetMin = Vector2.zero;
        buttonText.rectTransform.offsetMax = Vector2.zero;
    }
}
