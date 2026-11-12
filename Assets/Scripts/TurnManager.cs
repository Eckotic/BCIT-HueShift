using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Runs the battle: player turn -> enemy turn, enemy moves and intent, win/lose,
// and the run of encounters (enemy dies -> next enemy -> ... -> Victory).
// The enemy turn plays out as a short sequence (banner, wind-up, hit) so it reads clearly.
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

    [Header("Pacing")]
    [Tooltip("Seconds between the enemy's actions. Lower = snappier enemy turns.")]
    [SerializeField] private float enemyActionGap = 0.45f;

    [Header("Intent label")]
    [Tooltip("Screen-pixel offset of the intent label from the top of the enemy sprite.")]
    [SerializeField] private Vector2 intentOffset = new Vector2(0f, 10f);

    private static readonly Color EnemyAccent = new Color(0.9f, 0.28f, 0.3f);
    private static readonly Color PlayerAccent = new Color(0.45f, 0.5f, 1f);

    private BattleHUD hud;
    private EntityVisual enemyVisual;
    private EntityVisual playerVisual;
    private TMP_Text intentText;
    private int encounterIndex;
    private int moveIndex;
    private bool battleOver;
    private bool switchingFight;
    private bool enemyTurnRunning;

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
        playerVisual = FindVisualFor(player);
        intentText = BattleUI.CreateIntentLabel();
        hud = BattleHUD.Create(player, enemy, deckManager, playerVisual, enemyVisual, EnemyTitle);

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
        StartCoroutine(OpeningBanner());
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

    // Keeps the intent pill above the enemy sprite, bobbing gently.
    void LateUpdate()
    {
        if (intentText == null || enemyVisual == null || enemyVisual.Body == null || Camera.main == null)
        {
            return;
        }
        RectTransform label = intentText.transform.parent as RectTransform;
        if (label == null || label.GetComponent<Canvas>() != null)
        {
            label = intentText.rectTransform;
        }
        float bob = Mathf.Sin(Time.time * 2.5f) * 4f;

        // Docked just under the enemy's status panel, centred on it, so it reads as part of the
        // enemy's info and can never overlap the panel. (Intent Offset nudges it.)
        if (hud != null)
        {
            Vector3 anchor = hud.EnemyPanelBottomCentre();
            float scale = label.lossyScale.y;
            float height = label.rect.height * scale;
            label.position = anchor + new Vector3(intentOffset.x * scale, -14f * scale - height + intentOffset.y * scale - 10f * scale + bob * scale, 0f);
            return;
        }

        // Fallback without the HUD: above the art you can actually see.
        Bounds bounds = enemyVisual.VisibleBounds();
        Vector3 screen = Camera.main.WorldToScreenPoint(new Vector3(bounds.center.x, bounds.max.y, bounds.center.z));
        label.position = screen + (Vector3)intentOffset + Vector3.up * bob;
    }

    private IEnumerator OpeningBanner()
    {
        yield return new WaitForSeconds(0.5f);
        if (hud != null && !battleOver)
        {
            hud.ShowBanner("YOUR TURN", PlayerAccent);
        }
    }

    // Hooked to the End Turn button.
    public void EndTurn()
    {
        if (battleOver || switchingFight || enemyTurnRunning)
        {
            return;
        }
        StartCoroutine(EnemyTurn());
    }

    private IEnumerator EnemyTurn()
    {
        enemyTurnRunning = true;
        if (hud != null)
        {
            hud.SetEndTurnInteractable(false);
        }

        // 1. Player's turn ends: the leftover hand flies to the discard pile.
        if (deckManager != null)
        {
            deckManager.SetInputLocked(true);
            deckManager.DiscardHand();
        }
        yield return new WaitForSeconds(0.35f);

        // 2. Enemy's turn.
        Debug.Log("Enemy's turn.");
        float bannerTime = hud != null ? hud.ShowBanner("ENEMY TURN", EnemyAccent) : 0f;
        yield return new WaitForSeconds(bannerTime + 0.1f);

        if (enemy != null && !enemy.IsDead)
        {
            enemy.ResetBlock();   // its block from last turn wears off
            enemy.StartOwnTurn(); // Green heals, Black wears off

            EnemyMove move = CurrentMove();
            // Yellow: the enemy does its move twice.
            int times = enemy.CurrentColor == CardColor.Yellow ? 2 : 1;
            for (int i = 0; i < times && !battleOver; i++)
            {
                if (move.type == EnemyMoveType.Attack && enemyVisual != null && playerVisual != null)
                {
                    enemyVisual.Lunge(playerVisual.transform.position);
                    Sfx.Play(t => t.enemyAttack, 0.8f, 0.1f);
                    yield return new WaitForSeconds(0.2f); // land the hit on the strike frame
                }
                DoMove(move);
                yield return new WaitForSeconds(enemyActionGap);
            }
            moveIndex++;
        }

        if (battleOver)
        {
            enemyTurnRunning = false;
            yield break; // the player died
        }

        // 3. Back to the player: block wears off, colour effects tick, a fresh hand is drawn.
        if (player != null)
        {
            player.ResetBlock();
            player.StartOwnTurn();
        }

        Debug.Log("Back to player's turn.");
        if (hud != null)
        {
            hud.ShowBanner("YOUR TURN", PlayerAccent);
        }
        yield return new WaitForSeconds(0.3f);

        if (deckManager != null)
        {
            deckManager.StartPlayerTurn(); // Yellow gives bonus energy here
            deckManager.SetInputLocked(false);
        }
        UpdateIntent();
        enemyTurnRunning = false;
        if (hud != null)
        {
            hud.SetEndTurnInteractable(true);
        }
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
        bool show = !(battleOver || enemy == null || enemy.IsDead);
        Transform pill = intentText.transform.parent;
        if (pill != null && pill.GetComponent<Canvas>() == null)
        {
            pill.gameObject.SetActive(show);
        }
        if (!show)
        {
            intentText.text = "";
            return;
        }

        // The enemy's name and fight number are on its status panel; the pill shows only the move.
        EnemyMove move = CurrentMove();
        string times = enemy.CurrentColor == CardColor.Yellow ? "  x2" : "";
        if (move.type == EnemyMoveType.Attack)
        {
            intentText.text = $"<color=#FF6B6B>ATTACK {enemy.OutgoingDamage(move.amount)}{times}</color>";
        }
        else
        {
            intentText.text = $"<color=#6BB8FF>BLOCK {enemy.BlockGain(move.amount, enemy.CurrentColor)}{times}</color>";
        }
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
            enemyVisual.PlayEntrance();
        }
        Sfx.Play(t => t.enemySpawn, 0.8f);

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
        if (enemyVisual != null)
        {
            enemyVisual.PlayDeath();
        }
        Sfx.Play(t => t.enemyDeath);
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
            if (hud != null)
            {
                hud.ShowBanner($"{encounters[next].enemyName.ToUpperInvariant()}  {next + 1}/{encounters.Count}", EnemyAccent);
            }
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
            Sfx.Play(t => t.victory);
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
        if (playerVisual != null)
        {
            playerVisual.PlayDeath();
        }
        StartCoroutine(DefeatAfterPause());
    }

    private IEnumerator DefeatAfterPause()
    {
        yield return new WaitForSeconds(0.8f);
        Sfx.Play(t => t.defeat);
        EnemyData data = CurrentEnemy;
        string killer = data != null ? data.enemyName : "the enemy";
        BattleUI.ShowEndScreen("Defeat", $"You fell to {killer} in fight {encounterIndex + 1}.", new Color(1f, 0.35f, 0.35f), "Restart", Restart);
    }

    private void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Title for the enemy's status panel, e.g. "SQUARE  1/3".
    private string EnemyTitle()
    {
        EnemyData data = CurrentEnemy;
        string enemyName = data != null ? data.enemyName : (enemy != null ? enemy.gameObject.name : "Enemy");
        string fight = encounters.Count > 0 ? $"  {encounterIndex + 1}/{encounters.Count}" : "";
        return enemyName.ToUpperInvariant() + fight;
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
        Debug.LogWarning($"TurnManager: no EntityVisual found for {entity.name}, its animations won't play.");
        return null;
    }
}
