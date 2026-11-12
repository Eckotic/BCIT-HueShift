using UnityEngine;

public static class CardManager : object
{

    public static void playerAttacksEnemy(int x, BattleEntity target)
    {
        target.TakeDamage(x);
    }
    public static void playerGainsBlock(int x, BattleEntity target)
    {
        if (target != null)
        {
            target.GainBlock(x);
        }
    }
    public static void playerGainsEnergy(int x, DeckManager deck)
    {
        if (deck == null)
        {
            Debug.LogWarning("playerGainsEnergy: no DeckManager to give energy to.");
            return;
        }
        deck.GainEnergy(x);
    }
}
