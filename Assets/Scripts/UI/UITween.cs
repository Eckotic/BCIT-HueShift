using System.Collections;
using TMPro;
using UnityEngine;

// Small one-shot UI animations (pop-in, fade-in, floating numbers), no tween library needed.
public class UITween : MonoBehaviour
{
    // Scales from `from` to 1 with a little overshoot.
    public static void PopIn(Transform target, float duration = 0.35f, float from = 0.6f, float delay = 0f)
    {
        UITween tween = target.gameObject.AddComponent<UITween>();
        tween.StartCoroutine(tween.PopRoutine(target, duration, from, delay));
    }

    // Fades a CanvasGroup from 0 to `to`.
    public static void FadeIn(CanvasGroup group, float duration = 0.3f, float to = 1f)
    {
        UITween tween = group.gameObject.AddComponent<UITween>();
        tween.StartCoroutine(tween.FadeRoutine(group, duration, to));
    }

    // Quick scale punch (used when a number changes).
    public static void Punch(Transform target, float amount = 0.18f, float duration = 0.25f)
    {
        UITween tween = target.gameObject.GetComponent<UITween>();
        if (tween == null)
        {
            tween = target.gameObject.AddComponent<UITween>();
        }
        tween.StopAllCoroutines();
        tween.StartCoroutine(tween.PunchRoutine(target, amount, duration));
    }

    // A number that floats up and fades out, then deletes itself.
    public static void FloatingText(Transform canvas, Vector2 screenPosition, string text, Color color, float size = 56f)
    {
        GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas, false);
        go.transform.SetAsLastSibling();
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null)
        {
            tmp.font = TMP_Settings.defaultFontAsset;
        }
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(15, 17, 24, 255);
        tmp.rectTransform.sizeDelta = new Vector2(400f, 100f);
        tmp.rectTransform.position = screenPosition + new Vector2(Random.Range(-30f, 30f), 0f);

        UITween tween = go.AddComponent<UITween>();
        tween.StartCoroutine(tween.FloatRoutine(tmp));
    }

    private IEnumerator PopRoutine(Transform target, float duration, float from, float delay)
    {
        target.localScale = Vector3.one * from;
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float eased = EaseOutBack(Mathf.Clamp01(t));
            target.localScale = Vector3.one * Mathf.LerpUnclamped(from, 1f, eased);
            yield return null;
        }
        target.localScale = Vector3.one;
        Destroy(this);
    }

    private IEnumerator FadeRoutine(CanvasGroup group, float duration, float to)
    {
        group.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            group.alpha = Mathf.Lerp(0f, to, Mathf.Clamp01(t));
            yield return null;
        }
        Destroy(this);
    }

    private IEnumerator PunchRoutine(Transform target, float amount, float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            target.localScale = Vector3.one * (1f + amount * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private IEnumerator FloatRoutine(TextMeshProUGUI tmp)
    {
        RectTransform rect = tmp.rectTransform;
        Vector3 start = rect.position;
        Color baseColor = tmp.color;
        const float duration = 1f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float k = Mathf.Clamp01(t);
            rect.position = start + Vector3.up * (140f * EaseOutCubic(k));
            float scale = k < 0.15f ? Mathf.Lerp(0.5f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) / 0.85f);
            rect.localScale = Vector3.one * scale;
            tmp.color = new Color(baseColor.r, baseColor.g, baseColor.b, k < 0.6f ? 1f : Mathf.Lerp(1f, 0f, (k - 0.6f) / 0.4f));
            yield return null;
        }
        Destroy(gameObject);
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
