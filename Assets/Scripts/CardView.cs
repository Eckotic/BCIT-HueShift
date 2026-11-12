using UnityEngine;
using TMPro;

// Fills a card's cost, name and description labels from its Card component,
// so the text on the card always matches the numbers in the code.
[RequireComponent(typeof(Card))]
public class CardView : MonoBehaviour
{
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;

    void Awake()
    {
        Refresh();
    }

    public void Refresh()
    {
        Card card = GetComponent<Card>();
        if (card == null)
        {
            return;
        }

        if (costText != null)
        {
            costText.text = card.Cost.ToString();
        }
        if (nameText != null)
        {
            nameText.text = card.CardName;
        }
        if (descriptionText != null)
        {
            descriptionText.text = card.Description;
        }
    }
}
