using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private BattleEntity player;
    [SerializeField] private BattleEntity enemy;
    [SerializeField] private int enemyAttackDamage = 5;
    [Tooltip("Found automatically in the scene if left empty.")]
    [SerializeField] private DeckManager deckManager;

    void Awake()
    {
        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }
    }

    public void EndTurn()
    {
        // 1. Player's turn ends: the leftover hand goes to the discard pile.
        if (deckManager != null)
        {
            deckManager.DiscardHand();
        }

        // 2. Enemy's turn. Its colour is fixed (Starting Color on its BattleEntity).
        Debug.Log("Enemy's turn.");

        if (enemy != null && !enemy.IsDead)
        {
            enemy.StartOwnTurn(); // Green heals, Black wears off

            // Yellow: the enemy acts twice.
            int actions = enemy.CurrentColor == CardColor.Yellow ? 2 : 1;
            for (int i = 0; i < actions; i++)
            {
                if (player != null)
                {
                    player.TakeDamage(enemy.OutgoingDamage(enemyAttackDamage));
                }
            }
        }

        // 3. Back to the player: block wears off, colour effects tick, a fresh hand is drawn.
        if (player != null)
        {
            player.ResetBlock();
            player.StartOwnTurn(); // Green heals, Black wears off back to White
        }

        Debug.Log("Back to player's turn.");

        if (deckManager != null)
        {
            deckManager.StartPlayerTurn(); // Yellow gives bonus energy here
        }
    }
}
