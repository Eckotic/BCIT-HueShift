using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> cardPrefabs;
    [SerializeField] private Transform handArea;
    [Tooltip("How many cards the player draws at the start of each turn.")]
    [SerializeField] private int startingHandSize = 5;
    [Tooltip("The hand can never hold more than this many cards.")]
    [SerializeField] private int maxHandSize = 10;
    [Tooltip("Debug only: lets clicking the draw pile draw a card at any time.")]
    [SerializeField] private bool allowManualDraw = false;
    [SerializeField] private TMP_Text deckCountText;
    [SerializeField] private TMP_Text discardCountText;
    [SerializeField] private BattleEntity enemyTarget;
    [SerializeField] private BattleEntity playerTarget;

    [Header("Energy")]
    [Tooltip("Energy the player gets back at the start of every turn.")]
    [SerializeField] private int maxEnergy = 3;
    [Tooltip("Optional: a TMP text that shows energy as current/max.")]
    [SerializeField] private TMP_Text energyText;
    [Tooltip("Extra energy at the start of the turn while the player is Yellow (3 becomes 4).")]
    [SerializeField] private int yellowBonusEnergy = 1;

    private int currentEnergy;
    public int CurrentEnergy => currentEnergy;

    private List<GameObject> deck = new List<GameObject>();
    private List<GameObject> hand = new List<GameObject>();
    private List<GameObject> discard = new List<GameObject>();
    private Dictionary<GameObject, GameObject> instancePrefabs = new Dictionary<GameObject, GameObject>();

    void Start()
    {
        BuildDeck();
        ShuffleDeck();
        StartPlayerTurn();
    }

    // Called at the start of every player turn (and once when the battle starts).
    public void StartPlayerTurn()
    {
        currentEnergy = maxEnergy;
        if (playerTarget != null && playerTarget.CurrentColor == CardColor.Yellow)
        {
            currentEnergy += yellowBonusEnergy;
            Debug.Log($"Yellow: +{yellowBonusEnergy} energy this turn.");
        }
        for (int i = 0; i < startingHandSize; i++)
        {
            if (!DrawOne())
            {
                break;
            }
        }
        RefreshPileCounts();
        RefreshEnergy();
    }

    // Used by cards like Battery. Energy can go above max for the rest of the turn.
    public void GainEnergy(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        currentEnergy += amount;
        Debug.Log($"Gained {amount} energy. Energy: {currentEnergy}/{maxEnergy}");
        RefreshEnergy();
    }

    // Updates the energy counter, greys out any card in hand the player can't afford,
    // and refreshes each card's text (its numbers change with the player's colour).
    private void RefreshEnergy()
    {
        if (energyText != null)
        {
            energyText.text = $"{currentEnergy}/{maxEnergy}";
        }

        foreach (GameObject cardInstance in hand)
        {
            Button button = cardInstance.GetComponent<Button>();
            Card card = cardInstance.GetComponent<Card>();
            if (button != null && card != null)
            {
                button.interactable = card.Cost <= currentEnergy;
            }

            CardView view = cardInstance.GetComponent<CardView>();
            if (view != null)
            {
                view.Refresh();
            }
        }
    }

    // Called at the end of the player's turn: every card still in hand goes to the discard pile.
    public void DiscardHand()
    {
        foreach (GameObject cardInstance in hand)
        {
            if (instancePrefabs.TryGetValue(cardInstance, out GameObject sourcePrefab))
            {
                discard.Add(sourcePrefab);
                instancePrefabs.Remove(cardInstance);
            }
            Destroy(cardInstance);
        }
        Debug.Log($"Discarded {hand.Count} card(s) from hand. Deck: {deck.Count}, Discard: {discard.Count}");
        hand.Clear();
        RefreshPileCounts();
    }

    private void RefreshPileCounts()
    {
        if (deckCountText != null)
        {
            deckCountText.text = deck.Count.ToString();
        }
        if (discardCountText != null)
        {
            discardCountText.text = discard.Count.ToString();
        }
    }

    private void BuildDeck()
    {
        deck.Clear();
        foreach (GameObject prefab in cardPrefabs)
        {
            deck.Add(prefab);
        }
    }

    private void ShuffleDeck()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (deck[i], deck[j]) = (deck[j], deck[i]);
        }
    }

    // Hooked to clicking the draw pile in the scene. Off unless allowManualDraw is ticked (for testing).
    public void DrawCard()
    {
        if (!allowManualDraw)
        {
            Debug.Log("Manual draw is off. Cards are drawn at the start of each turn.");
            return;
        }
        DrawOne();
        RefreshPileCounts();
        RefreshEnergy();
    }

    // Draws one card into the hand. Returns false if nothing could be drawn.
    private bool DrawOne()
    {
        if (hand.Count >= maxHandSize)
        {
            Debug.Log($"Hand is full ({maxHandSize}), can't draw.");
            return false;
        }

        if (deck.Count == 0)
        {
            Debug.Log("Deck is empty, reshuffling discard into deck.");
            deck.AddRange(discard);
            discard.Clear();
            ShuffleDeck();
            if (deck.Count == 0)
            {
                Debug.Log("Discard is also empty, nothing left to draw.");
                return false;
            }
        }

        GameObject prefab = deck[0];
        deck.RemoveAt(0);

        GameObject cardInstance = Instantiate(prefab, handArea);
        hand.Add(cardInstance);
        instancePrefabs[cardInstance] = prefab;

        Card drawnCard = cardInstance.GetComponent<Card>();
        if (drawnCard != null)
        {
            drawnCard.owner = playerTarget;
        }

        AttackExample attack = cardInstance.GetComponent<AttackExample>();
        if (attack != null)
        {
            attack.target = enemyTarget;
        }

        DefendExample defend = cardInstance.GetComponent<DefendExample>();
        if (defend != null)
        {
            defend.target = playerTarget;
        }

        BatteryCard battery = cardInstance.GetComponent<BatteryCard>();
        if (battery != null)
        {
            battery.deck = this;
        }

        Button button = cardInstance.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(() => PlayCard(cardInstance));
        }

        Debug.Log($"Drew {prefab.name}. Hand: {hand.Count}, Deck: {deck.Count}, Discard: {discard.Count}");
        return true;
    }

    public void PlayCard(GameObject cardInstance)
    {
        if (!hand.Contains(cardInstance))
        {
            Debug.Log($"{cardInstance.name} is not in hand, can't play it.");
            return;
        }

        Card card = cardInstance.GetComponent<Card>();
        if (card != null)
        {
            if (card.Cost > currentEnergy)
            {
                Debug.Log($"Not enough energy to play {card.CardName} (costs {card.Cost}, have {currentEnergy}).");
                return;
            }
            // Pay first, so a 0-cost Battery nets +1.
            currentEnergy -= card.Cost;
            // Shift colour before the card resolves, so the card gets the new colour's buff.
            if (playerTarget != null)
            {
                playerTarget.ShiftColor(card.Color);
            }
            card.playFromHand();
        }

        hand.Remove(cardInstance);
        if (instancePrefabs.TryGetValue(cardInstance, out GameObject sourcePrefab))
        {
            discard.Add(sourcePrefab);
            instancePrefabs.Remove(cardInstance);
        }

        Destroy(cardInstance);
        RefreshPileCounts();
        RefreshEnergy();
    }
}
