using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the battle: player turn -> enemy turn, enemy moves and intent, win/lose,
// and the run of encounters (enemy dies -> next enemy -> ... -> Victory).
public class TurnManager : MonoBehaviour
{
    [SerializeField] private BattleEntity player;
    [SerializeField] private BattleEntity enemy;
    [Tooltip("Only used if the Encounters list is empty.")]
    [SerializeField] private int enemyAttackDamage = 5;
    [Tooltip("Found automatically in the scene if left empty.")]
    [SerializeField] private DeckManager deckManager;

    [Header("Run")]
    [Tooltip("The fights in order. Filled by HueShift > Build Enemies.")]
    [SerializeField] private List<EnemyData> encounters = new List<EnemyData>();
    [Tooltip("Seconds between an enemy dying and the next one appearing.")]
    [SerializeField] private float pauseBetweenFights = 1f;

    [Header("Intent label")]
    [Tooltip("Screen-pixel offset of the intent label from the top of the enemy sprite.")]
    [SerializeField] private Vector2 intentOffset = new Vector2(0f, 10f);

    private EntityVisual enemyVisual;
    private TMP_Text intentText;
    private int encounterIndex;
    private int moveIndex;
    private bool battleOver;
    private bool switchingFight;

    private EnemyData CurrentEnemy =>
        encounters.Count > 0 && encounterIndex < encounters.Count ? encounters[encounterIndex] : null;

    void Awake()
    {
        if (deckManager == null)
        {
            deckManager = FindFirstObjectByType<DeckManager>();
        }
    }

    void Start()
    {
        enemyVisual = FindVisualFor(enemy);
        intentText = BattleUI.CreateIntentLabel();

        if (enemy != null)
        {
            enemy.OnDied += HandleEnemyDied;
        }
        if (player != null)
        {
            player.OnDied += HandlePlayerDied;
        }

        if (encounters.Count > 0)
        {
            LoadEncounter(0, false);
        }
        UpdateIntent();
    }

    void OnDestroy()
    {
        if (enemy != null)
        {
            enemy.OnDied -= HandleEnemyDied;
        }
        if (player != null)
        {
            player.OnDied -= HandlePlayerDied;
        }
    }

    // Keeps the intent label above the enemy sprite.
    void LateUpdate()
    {
        if (intentText == null || enemyVisual == null || enemyVisual.Body == null || Camera.main == null)
        {
            return;
        }
        Bounds bounds = enemyVisual.Body.bounds;
        Vector3 top = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        Vector3 screen = Camera.main.WorldToScreenPoint(top);
        intentText.rectTransform.position = screen + (Vector3)intentOffset;
    }

    // Hooked to the End Turn button.
    public void EndTurn()
    {
        if (battleOver || switchingFight)
        {
            return;
        }

        // 1. Player's turn ends: the leftover hand goes to the discard pile.
        if (deckManager != null)
        {
            deckManager.DiscardHand();
        }

        // 2. Enemy's turn.
        Debug.Log("Enemy's turn.");
        if (enemy != null && !enemy.IsDead)
        {
            enemy.ResetBlock();   // its block from last turn wears off
            enemy.StartOwnTurn(); // Green heals, Black wears off

            EnemyMove move = CurrentMove();
            // Yellow: the enemy does its move twice.
            int times = enemy.CurrentColor == CardColor.Yellow ? 2 : 1;
            for (int i = 0; i < times && !battleOver; i++)
            {
                DoMove(move);
            }
            moveIndex++;
        }

        if (battleOver)
        {
            return; // the player died
        }

        // 3. Back to the player: block wears off, colour effects tick, a fresh hand is drawn.
        if (player != null)
        {
            player.ResetBlock();
            player.StartOwnTurn();
        }

        Debug.Log("Back to player's turn.");

        if (deckManager != null)
        {
            deckManager.StartPlayerTurn(); // Yellow gives bonus energy here
        }
        UpdateIntent();
    }

    // ---------- Enemy moves ----------

    private EnemyMove CurrentMove()
    {
        EnemyData data = CurrentEnemy;
        if (data == null || data.moves.Count == 0)
        {
            return new EnemyMove(EnemyMoveType.Attack, enemyAttackDamage);
        }
        return data.moves[moveIndex % data.moves.Count];
    }

    private void DoMove(EnemyMove move)
    {
        switch (move.type)
        {
            case EnemyMoveType.Attack:
                if (player != null)
                {
                    player.TakeDamage(enemy.OutgoingDamage(move.amount));
                }
                break;
            case EnemyMoveType.Block:
                enemy.GainBlock(enemy.BlockGain(move.amount, enemy.CurrentColor));
                break;
        }
    }

    // Shows what the enemy will do on its next turn, with its colour buffs already counted.
    private void UpdateIntent()
    {
        if (intentText == null)
        {
            return;
        }
        if (battleOver || enemy == null || enemy.IsDead)
        {
            intentText.text = "";
            return;
        }

        EnemyMove move = CurrentMove();
        string times = enemy.CurrentColor == CardColor.Yellow ? " x2" : "";
        string action;
        if (move.type == EnemyMoveType.Attack)
        {
            action = $"<color=#FF6B6B>Attack {enemy.OutgoingDamage(move.amount)}{times}</color>";
        }
        else
        {
            action = $"<color=#6BB8FF>Block {enemy.BlockGain(move.amount, enemy.CurrentColor)}{times}</color>";
        }

        EnemyData data = CurrentEnemy;
        string enemyLabel = data != null ? data.enemyName : enemy.gameObject.name;
        string fight = encounters.Count > 0 ? $" ({encounterIndex + 1}/{encounters.Count})" : "";
        intentText.text = $"{enemyLabel}{fight}\n{action}";
    }

    // ---------- Encounters, win and lose ----------

    private void LoadEncounter(int index, bool resetPlayerSide)
    {
        encounterIndex = index;
        moveIndex = 0;
        EnemyData data = encounters[index];

        enemy.Setup(data.maxHealth, data.color);
        if (enemyVisual != null)
        {
            enemyVisual.SetLook(data.bodySprite, data.outlineSprite, data.tintBody);
        }

        if (resetPlayerSide)
        {
            // HP carries over between fights; block and colour start fresh.
            if (player != null)
            {
                player.ResetBlock();
                player.SetCardColor(CardColor.White);
            }
            if (deckManager != null)
            {
                deckManager.ResetForNewFight();
            }
        }

        Debug.Log($"Fight {index + 1}/{encounters.Count}: {data.enemyName} ({data.color}, {data.maxHealth} HP).");
        UpdateIntent();
    }

    private void HandleEnemyDied()
    {
        if (battleOver || switchingFight)
        {
            return;
        }

        switchingFight = true;
        if (deckManager != null)
        {
            deckManager.SetInputLocked(true);
        }
        UpdateIntent();
        StartCoroutine(NextFightAfterPause());
    }

    private IEnumerator NextFightAfterPause()
    {
        yield return new WaitForSeconds(pauseBetweenFights);

        int next = encounterIndex + 1;
        if (next < encounters.Count)
        {
            LoadEncounter(next, true);
            switchingFight = false;
            if (deckManager != null)
            {
                deckManager.SetInputLocked(false);
            }
        }
        else
        {
            switchingFight = false;
            battleOver = true;
            UpdateIntent();
            string subtitle = encounters.Count > 0 ? $"You beat all {encounters.Count} enemies." : "The enemy is defeated.";
            BattleUI.ShowEndScreen("Victory!", subtitle, new Color(1f, 0.85f, 0.2f), "Play again", Restart);
        }
    }

    private void HandlePlayerDied()
    {
        if (battleOver)
        {
            return;
        }

        battleOver = true;
        if (deckManager != null)
        {
            deckManager.SetInputLocked(true);
        }
        UpdateIntent();

        EnemyData data = CurrentEnemy;
        string killer = data != null ? data.enemyName : "the enemy";
        BattleUI.ShowEndScreen("Defeat", $"You fell to {killer} in fight {encounterIndex + 1}.", new Color(1f, 0.35f, 0.35f), "Restart", Restart);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private static EntityVisual FindVisualFor(BattleEntity entity)
    {
        if (entity == null)
        {
            return null;
        }
        foreach (EntityVisual visual in FindObjectsByType<EntityVisual>(FindObjectsSortMode.None))
        {
            if (visual.Entity == entity)
            {
                return visual;
            }
        }
        Debug.LogWarning($"TurnManager: no EntityVisual found for {entity.name}, enemy art won't change between fights.");
        return null;
    }
}
