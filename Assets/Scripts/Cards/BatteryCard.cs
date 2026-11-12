using UnityEngine;

// The basic yellow card: costs 0 and gives energy back, like a battery,
// so the player can afford one more (or a more expensive) card this turn.
public class BatteryCard : Card
{
    [SerializeField] private int energyGain = 1;

    // Set by DeckManager when the card is drawn.
    [HideInInspector] public DeckManager deck;

    public override string Description => $"Gain {energyGain} energy.";

    public override void playFromHand()
    {
        CardManager.playerGainsEnergy(energyGain, deck);
    }
}
