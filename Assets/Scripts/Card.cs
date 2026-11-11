using UnityEngine;
using System.Collections;

public class Card : MonoBehaviour
{

    [SerializeField] protected int cost;
    [SerializeField] protected CardColor color;
    [Tooltip("Shown in the card's name banner. Uses the GameObject name if left empty.")]
    [SerializeField] protected string cardName;
    [Tooltip("Fallback rules text. Card types that compute their own text (Strike, Defend, Battery) ignore this.")]
    [SerializeField, TextArea] protected string description;

    // Who plays this card (the player). Set by DeckManager when the card is drawn.
    [HideInInspector] public BattleEntity owner;

    // The owner's colour once this card's colour is mixed in. Colour shifts before the card
    // resolves, so this is the colour whose buff the card gets (and what its text previews).
    protected CardColor ColorWhenPlayed => owner != null ? owner.PreviewShift(color) : color;

    public int Cost => cost;
    public CardColor Color => color;
    public string CardName => string.IsNullOrEmpty(cardName) ? gameObject.name.Replace("(Clone)", "").Trim() : cardName;
    public virtual string Description => description;

    public virtual void playFromHand()
    {
        Debug.Log("I'm not yet implemented!");
    }

}

public enum CardColor
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple,
    // Added at the end so existing prefabs keep their colours.
    White,  // neutral, no buff
    Black   // all three primaries: no buff, unshiftable, clears next turn
}