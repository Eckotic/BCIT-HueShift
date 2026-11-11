using UnityEngine;

public class EntityVisual : MonoBehaviour
{
    [SerializeField] private BattleEntity battleEntity;
    [Tooltip("Off for art that is already coloured: the sprite is shown as-is.")]
    [SerializeField] private bool tintEnabled = true;
    private SpriteRenderer spriteRenderer;

    public BattleEntity Entity => battleEntity;
    public SpriteRenderer Body => spriteRenderer;

    private Vector3 restPosition;
    private Coroutine hitRoutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        restPosition = transform.localPosition;
    }

    // Shake and flash red when hit. Bigger hits shake harder.
    public void PlayHit(int damage)
    {
        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            transform.localPosition = restPosition;
        }
        hitRoutine = StartCoroutine(HitRoutine(Mathf.Clamp(damage / 10f, 0.15f, 0.6f)));
    }

    private System.Collections.IEnumerator HitRoutine(float strength)
    {
        spriteRenderer.color = new Color(1f, 0.45f, 0.45f);
        const float duration = 0.3f;
        float t = 0f;
        bool restored = false;
        while (t < duration)
        {
            t += Time.deltaTime;
            float falloff = 1f - t / duration;
            transform.localPosition = restPosition + (Vector3)(Random.insideUnitCircle * strength * falloff);
            if (!restored && t > 0.1f)
            {
                ApplyColor(battleEntity != null ? battleEntity.CurrentColor : CardColor.White);
                restored = true;
            }
            yield return null;
        }
        transform.localPosition = restPosition;
        ApplyColor(battleEntity != null ? battleEntity.CurrentColor : CardColor.White);
        hitRoutine = null;
    }

    // Swaps the art (used when a new enemy comes in). The "Outline" child is hidden if outline is null.
    public void SetLook(Sprite body, Sprite outline, bool tint)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (body != null)
        {
            spriteRenderer.sprite = body;
        }

        Transform outlineChild = transform.Find("Outline");
        if (outlineChild != null)
        {
            SpriteRenderer outlineRenderer = outlineChild.GetComponent<SpriteRenderer>();
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = outline != null;
                if (outline != null)
                {
                    outlineRenderer.sprite = outline;
                }
            }
        }

        tintEnabled = tint;
        ApplyColor(battleEntity != null ? battleEntity.CurrentColor : CardColor.White);
    }

    void OnEnable()
    {
        if (battleEntity != null)
        {
            battleEntity.OnCardColorChanged += ApplyColor;
        }
    }

    void OnDisable()
    {
        if (battleEntity != null)
        {
            battleEntity.OnCardColorChanged -= ApplyColor;
        }
    }

    private void ApplyColor(CardColor cardColor)
    {
        if (!tintEnabled)
        {
            spriteRenderer.color = Color.white;
            return;
        }

        switch (cardColor)
        {
            case CardColor.Red:
                spriteRenderer.color = Color.red;
                break;
            case CardColor.Orange:
                spriteRenderer.color = new Color(1f, 0.5f, 0f);
                break;
            case CardColor.Yellow:
                spriteRenderer.color = Color.yellow;
                break;
            case CardColor.Green:
                spriteRenderer.color = Color.green;
                break;
            case CardColor.Blue:
                spriteRenderer.color = Color.blue;
                break;
            case CardColor.Purple:
                spriteRenderer.color = new Color(0.5f, 0f, 0.5f);
                break;
            case CardColor.White:
                spriteRenderer.color = Color.white; // no tint
                break;
            case CardColor.Black:
                spriteRenderer.color = new Color(0.25f, 0.25f, 0.25f); // dark, but the sprite stays visible
                break;
        }
    }
}
