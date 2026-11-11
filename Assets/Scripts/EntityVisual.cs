using DG.Tweening;
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
    private Vector3 restScale;
    private Tween idle;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        restPosition = transform.localPosition;
        restScale = transform.localScale;
    }

    void Start()
    {
        // If an entrance is already playing, it starts the idle itself when it ends.
        if (!DOTween.IsTweening(transform))
        {
            StartIdle();
        }
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

    void OnDestroy()
    {
        transform.DOKill();
        if (spriteRenderer != null)
        {
            spriteRenderer.DOKill();
        }
    }

    // ---------- Animations (DOTween) ----------

    // Gentle breathing so characters never look frozen.
    private void StartIdle()
    {
        idle?.Kill();
        transform.localScale = restScale;
        idle = transform.DOScale(new Vector3(restScale.x * 1.015f, restScale.y * 1.03f, restScale.z), 1.6f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetDelay(Random.Range(0f, 0.8f));
    }

    // Shake and flash red when hit. Bigger hits shake harder.
    public void PlayHit(int damage)
    {
        float strength = Mathf.Clamp(damage / 12f, 0.2f, 0.7f);
        transform.DOKill(false);
        transform.localPosition = restPosition;
        transform.localScale = restScale;
        Sequence hit = DOTween.Sequence();
        hit.Append(transform.DOShakePosition(0.35f, new Vector3(strength, strength * 0.5f, 0f), 30, 90f, false, true));
        hit.Join(transform.DOPunchScale(new Vector3(-0.08f, 0.1f, 0f) * restScale.x, 0.3f, 8, 0.6f));
        hit.OnComplete(StartIdle);

        spriteRenderer.DOKill();
        spriteRenderer.color = new Color(1f, 0.4f, 0.4f);
        DOVirtual.DelayedCall(0.12f, () => ApplyColor(CurrentColor())).SetLink(gameObject);
    }

    // Quick jab towards a world position (attacking).
    public void Lunge(Vector3 towards)
    {
        Vector3 direction = towards - transform.position;
        direction.z = 0f;
        transform.DOKill(false);
        transform.localPosition = restPosition;
        transform.localScale = restScale;
        Sequence lunge = DOTween.Sequence();
        lunge.Append(transform.DOLocalMove(restPosition - direction.normalized * 0.4f, 0.1f).SetEase(Ease.OutQuad)); // wind up
        lunge.Append(transform.DOLocalMove(restPosition + direction.normalized * 1.6f, 0.12f).SetEase(Ease.InQuad)); // strike
        lunge.Append(transform.DOLocalMove(restPosition, 0.25f).SetEase(Ease.OutBack));
        lunge.OnComplete(StartIdle);
    }

    // Small upward hop with a squash (blocking or buffing).
    public void Hop()
    {
        transform.DOKill(false);
        transform.localPosition = restPosition;
        transform.localScale = restScale;
        Sequence hop = DOTween.Sequence();
        hop.Append(transform.DOLocalJump(restPosition, 0.5f, 1, 0.3f));
        hop.Join(transform.DOPunchScale(new Vector3(0.08f, -0.08f, 0f) * restScale.x, 0.3f, 6, 0.5f));
        hop.OnComplete(StartIdle);
    }

    // Shrinks, spins and fades out.
    public Tween PlayDeath()
    {
        transform.DOKill(false);
        idle?.Kill();
        Sequence death = DOTween.Sequence();
        death.Append(transform.DOShakePosition(0.25f, 0.4f, 40));
        death.Append(transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InBack));
        death.Join(transform.DORotate(new Vector3(0f, 0f, 25f), 0.4f));
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.DOKill();
            death.Join(sr.DOFade(0f, 0.4f));
        }
        return death;
    }

    // Pops in from nothing (a new enemy arriving).
    public void PlayEntrance()
    {
        transform.DOKill(false);
        idle?.Kill();
        transform.localPosition = restPosition + Vector3.up * 2f;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.zero;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            sr.DOKill();
            Color c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, 1f);
        }
        ApplyColor(CurrentColor());

        Sequence entrance = DOTween.Sequence();
        entrance.Append(transform.DOScale(restScale, 0.5f).SetEase(Ease.OutBack));
        entrance.Join(transform.DOLocalMove(restPosition, 0.5f).SetEase(Ease.OutBounce));
        entrance.OnComplete(StartIdle);
    }

    // ---------- Visible bounds ----------

    private static readonly System.Collections.Generic.Dictionary<Sprite, Bounds> tightBounds =
        new System.Collections.Generic.Dictionary<Sprite, Bounds>();

    // Bounds of the pixels you can actually see (ignores the empty space around the art),
    // so labels, numbers and effects land on the character, not in its transparent padding.
    public Bounds VisibleBounds()
    {
        bool any = false;
        Bounds result = new Bounds(transform.position, Vector3.zero);
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
        {
            if (!sr.enabled || sr.sprite == null)
            {
                continue;
            }
            Bounds local = TightLocalBounds(sr.sprite);
            Vector3 min = sr.transform.TransformPoint(local.min);
            Vector3 max = sr.transform.TransformPoint(local.max);
            if (!any)
            {
                result = new Bounds(min, Vector3.zero);
                any = true;
            }
            else
            {
                result.Encapsulate(min);
            }
            result.Encapsulate(max);
        }
        if (!any && spriteRenderer != null)
        {
            return spriteRenderer.bounds;
        }
        return result;
    }

    private static Bounds TightLocalBounds(Sprite sprite)
    {
        if (tightBounds.TryGetValue(sprite, out Bounds cached))
        {
            return cached;
        }
        Vector2[] vertices = sprite.vertices; // the tight outline Unity cut around the visible pixels
        Bounds bounds = new Bounds(vertices.Length > 0 ? (Vector3)vertices[0] : Vector3.zero, Vector3.zero);
        foreach (Vector2 v in vertices)
        {
            bounds.Encapsulate(v);
        }
        tightBounds[sprite] = bounds;
        return bounds;
    }

    // ---------- Look ----------

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
        ApplyColor(CurrentColor());
    }

    private CardColor CurrentColor()
    {
        return battleEntity != null ? battleEntity.CurrentColor : CardColor.White;
    }

    private void ApplyColor(CardColor cardColor)
    {
        if (spriteRenderer == null)
        {
            return;
        }
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
