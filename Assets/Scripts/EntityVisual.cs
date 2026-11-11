using UnityEngine;

public class EntityVisual : MonoBehaviour
{
    [SerializeField] private BattleEntity battleEntity;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
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
