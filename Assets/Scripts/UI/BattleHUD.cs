using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Restyles the battle screen at runtime: status panels (name, colour, HP, block) for the player and
// enemy, an energy orb, card-back draw/discard piles, the End Turn button, the fanned hand, the
// backdrop, and hit effects with floating numbers. Created by TurnManager; no scene setup needed.
public class BattleHUD : MonoBehaviour
{
    private class StatusPanel
    {
        public BattleEntity entity;
        public bool isPlayer;
        public TMP_Text nameText;
        public Image colorPill;
        public TMP_Text colorText;
        public RectTransform hpFill;
        public RectTransform hpChip;
        public TMP_Text hpText;
        public Image blockBadge;
        public TMP_Text blockText;
        public float shownPct = 1f;
        public float chipPct = 1f;
        public float chipDelay;
        public int lastHp = int.MinValue;
        public int lastMax = int.MinValue;
        public int lastBlock = int.MinValue;
        public CardColor lastColor = (CardColor)(-1);
        public string lastEffect;
    }

    private Canvas canvas;
    private DeckManager deck;
    private EntityVisual playerVisual;
    private EntityVisual enemyVisual;
    private Func<string> enemyTitle;
    private StatusPanel playerPanel;
    private StatusPanel enemyPanel;
    private RectTransform energyOrb;
    private Image energyFill;
    private TMP_Text energyText;
    private int lastEnergy = int.MinValue;
    private string lastEnemyTitle;

    public static BattleHUD Create(BattleEntity player, BattleEntity enemy, DeckManager deck,
        EntityVisual playerVisual, EntityVisual enemyVisual, Func<string> enemyTitle)
    {
        GameObject go = new GameObject("BattleHUD");
        BattleHUD hud = go.AddComponent<BattleHUD>();
        hud.Init(player, enemy, deck, playerVisual, enemyVisual, enemyTitle);
        return hud;
    }

    private void Init(BattleEntity player, BattleEntity enemy, DeckManager deckManager,
        EntityVisual pv, EntityVisual ev, Func<string> title)
    {
        canvas = BattleUI.FindCanvas();
        deck = deckManager;
        playerVisual = pv;
        enemyVisual = ev;
        enemyTitle = title;
        if (canvas == null)
        {
            Debug.LogWarning("BattleHUD: no Canvas found.");
            return;
        }

        ApplyDefaultFont();
        HideOldBar(player);
        HideOldBar(enemy);

        if (player != null)
        {
            playerPanel = BuildPanel(player, true);
            player.OnDamaged += (hp, blocked) => OnDamaged(playerVisual, hp, blocked);
            player.OnBlockGained += amount => Float(playerVisual, $"+{amount} BLOCK", UIShapes.BlockBlue, 44f, 0f);
            player.OnHealed += amount => Float(playerVisual, $"+{amount}", UIShapes.HealGreen, 56f, 0f);
        }
        if (enemy != null)
        {
            enemyPanel = BuildPanel(enemy, false);
            enemy.OnDamaged += (hp, blocked) => OnDamaged(enemyVisual, hp, blocked);
            enemy.OnBlockGained += amount => Float(enemyVisual, $"+{amount} BLOCK", UIShapes.BlockBlue, 44f, 0f);
            enemy.OnHealed += amount => Float(enemyVisual, $"+{amount}", UIShapes.HealGreen, 56f, 0f);
        }

        StylePile("DrawPile", "DRAW", false);
        StylePile("DiscardPile", "DISCARD", true);
        StyleEndTurn();
        BuildEnergyOrb();

        if (deck != null && deck.HandArea != null)
        {
            HandLayout layout = deck.HandArea.gameObject.AddComponent<HandLayout>();
            layout.spawnFrom = canvas.transform.Find("DrawPile") as RectTransform;
            layout.maxHandWidth = 1000f;
        }

        Material spriteMaterial = null;
        if (enemyVisual != null && enemyVisual.Body != null)
        {
            spriteMaterial = enemyVisual.Body.sharedMaterial;
        }
        else if (playerVisual != null && playerVisual.Body != null)
        {
            spriteMaterial = playerVisual.Body.sharedMaterial;
        }
        BattleBackdrop.Create(Camera.main, spriteMaterial);
    }

    void Update()
    {
        Refresh(playerPanel);
        Refresh(enemyPanel);
        RefreshEnergy();

        if (enemyPanel != null && enemyTitle != null)
        {
            string title = enemyTitle();
            if (title != lastEnemyTitle)
            {
                lastEnemyTitle = title;
                enemyPanel.nameText.text = title;
            }
        }
    }

    // ---------- Status panels ----------

    private StatusPanel BuildPanel(BattleEntity entity, bool isPlayer)
    {
        StatusPanel panel = new StatusPanel { entity = entity, isPlayer = isPlayer };

        Image root = BattleUI.Box(canvas.transform, isPlayer ? "PlayerStatus" : "EnemyStatus", UIShapes.RoundedRect(22), UIShapes.Panel);
        RectTransform rect = root.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = isPlayer ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.anchoredPosition = isPlayer ? new Vector2(32f, -28f) : new Vector2(-32f, -28f);
        rect.sizeDelta = new Vector2(560f, 150f);
        BattleUI.Shadow(root);

        // Name (top-left).
        panel.nameText = BattleUI.Text(rect, "Name", isPlayer ? "YOU" : "ENEMY", 32f, UIShapes.TextLight, FontStyles.Bold);
        panel.nameText.alignment = TextAlignmentOptions.Left;
        panel.nameText.characterSpacing = 3f;
        RectTransform nameRect = panel.nameText.rectTransform;
        nameRect.anchorMin = nameRect.anchorMax = nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(26f, -14f);
        nameRect.sizeDelta = new Vector2(260f, 48f);

        // Colour pill (top-right): swatch colour + what it does.
        panel.colorPill = BattleUI.Box(rect, "ColourPill", UIShapes.RoundedRect(18), Color.white);
        RectTransform pillRect = panel.colorPill.rectTransform;
        pillRect.anchorMin = pillRect.anchorMax = pillRect.pivot = new Vector2(1f, 1f);
        pillRect.anchoredPosition = new Vector2(-18f, -18f);
        pillRect.sizeDelta = new Vector2(260f, 42f);
        panel.colorText = BattleUI.Text(pillRect, "Text", "", 20f, Color.black, FontStyles.Bold);
        BattleUI.Stretch(panel.colorText.rectTransform, 4f);
        panel.colorText.enableAutoSizing = true;
        panel.colorText.fontSizeMin = 12f;
        panel.colorText.fontSizeMax = 20f;

        // HP bar.
        Image back = BattleUI.Box(rect, "HpBar", UIShapes.RoundedRect(14), UIShapes.HpBack);
        RectTransform backRect = back.rectTransform;
        backRect.anchorMin = new Vector2(0f, 0f);
        backRect.anchorMax = new Vector2(1f, 0f);
        backRect.pivot = new Vector2(0.5f, 0f);
        backRect.offsetMin = new Vector2(100f, 22f);
        backRect.offsetMax = new Vector2(-24f, 66f);

        panel.hpChip = BattleUI.Box(backRect, "Chip", UIShapes.RoundedRect(12), UIShapes.HpChip).rectTransform;
        panel.hpFill = BattleUI.Box(backRect, "Fill", UIShapes.RoundedRect(12), UIShapes.HpFill).rectTransform;
        foreach (RectTransform bar in new[] { panel.hpChip, panel.hpFill })
        {
            bar.anchorMin = Vector2.zero;
            bar.anchorMax = Vector2.one;
            bar.offsetMin = new Vector2(4f, 4f);
            bar.offsetMax = new Vector2(-4f, -4f);
        }

        panel.hpText = BattleUI.Text(backRect, "HpText", "", 26f, Color.white, FontStyles.Bold);
        panel.hpText.outlineWidth = 0.2f;
        panel.hpText.outlineColor = new Color32(15, 17, 24, 255);
        BattleUI.Stretch(panel.hpText.rectTransform);

        // Block badge (left of the bar).
        panel.blockBadge = BattleUI.Box(rect, "Block", UIShapes.Circle(128), UIShapes.BlockBlue, false);
        RectTransform badgeRect = panel.blockBadge.rectTransform;
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(0f, 0f);
        badgeRect.anchoredPosition = new Vector2(58f, 44f);
        badgeRect.sizeDelta = new Vector2(66f, 66f);
        panel.blockText = BattleUI.Text(badgeRect, "Text", "0", 28f, Color.white, FontStyles.Bold);
        BattleUI.Stretch(panel.blockText.rectTransform);

        return panel;
    }

    private void Refresh(StatusPanel panel)
    {
        if (panel == null || panel.entity == null)
        {
            return;
        }
        BattleEntity entity = panel.entity;
        int hp = entity.CurrentHealth;
        int max = Mathf.Max(1, entity.MaxHealth);
        float pct = Mathf.Clamp01((float)hp / max);

        if (hp != panel.lastHp || max != panel.lastMax)
        {
            bool tookDamage = hp < panel.lastHp && max == panel.lastMax;
            if (tookDamage)
            {
                panel.chipDelay = 0.4f; // the yellow chip lingers, then catches up
            }
            else
            {
                panel.shownPct = pct;
                panel.chipPct = pct;
            }
            panel.hpText.text = $"{hp} / {max}";
            panel.lastHp = hp;
            panel.lastMax = max;
        }

        panel.shownPct = Mathf.Lerp(panel.shownPct, pct, 1f - Mathf.Exp(-14f * Time.deltaTime));
        panel.chipDelay -= Time.deltaTime;
        if (panel.chipDelay <= 0f)
        {
            panel.chipPct = Mathf.Lerp(panel.chipPct, pct, 1f - Mathf.Exp(-5f * Time.deltaTime));
        }
        panel.chipPct = Mathf.Max(panel.chipPct, panel.shownPct);
        SetBar(panel.hpFill, panel.shownPct);
        SetBar(panel.hpChip, panel.chipPct);

        int block = entity.Block;
        if (block != panel.lastBlock)
        {
            panel.blockText.text = block.ToString();
            panel.blockBadge.color = block > 0 ? UIShapes.BlockBlue : new Color(0.25f, 0.27f, 0.35f, 0.7f);
            panel.blockText.color = block > 0 ? Color.white : UIShapes.TextMuted;
            if (panel.lastBlock != int.MinValue)
            {
                UITween.Punch(panel.blockBadge.transform, 0.3f);
            }
            panel.lastBlock = block;
        }

        CardColor color = entity.CurrentColor;
        string effect = entity.ColorEffectText(panel.isPlayer);
        if (color != panel.lastColor || effect != panel.lastEffect)
        {
            Color swatch = UIShapes.ForCardColor(color);
            panel.colorPill.color = swatch;
            bool darkText = color == CardColor.White || color == CardColor.Yellow || color == CardColor.Orange || color == CardColor.Green;
            panel.colorText.color = darkText ? UIShapes.Hex("14161F") : Color.white;
            panel.colorText.text = $"{color.ToString().ToUpperInvariant()}  ·  {effect}";
            if ((int)panel.lastColor >= 0)
            {
                UITween.Punch(panel.colorPill.transform, 0.15f);
            }
            panel.lastColor = color;
            panel.lastEffect = effect;
        }
    }

    private static void SetBar(RectTransform bar, float pct)
    {
        bar.anchorMax = new Vector2(pct, 1f);
        bar.gameObject.SetActive(pct > 0.005f);
    }

    // ---------- Energy, piles, End Turn ----------

    private void BuildEnergyOrb()
    {
        energyOrb = BattleUI.Rect(canvas.transform, "EnergyOrb");
        energyOrb.anchorMin = energyOrb.anchorMax = new Vector2(0f, 0f);
        energyOrb.anchoredPosition = new Vector2(115f, 330f);
        energyOrb.sizeDelta = new Vector2(140f, 140f);

        Image glow = BattleUI.Box(energyOrb, "Glow", UIShapes.Glow(128), new Color(1f, 0.79f, 0.24f, 0.35f), false);
        glow.rectTransform.sizeDelta = new Vector2(260f, 260f);
        Image ring = BattleUI.Box(energyOrb, "Ring", UIShapes.Circle(128), UIShapes.Hex("2A2416"), false);
        ring.rectTransform.sizeDelta = new Vector2(140f, 140f);
        energyFill = BattleUI.Box(energyOrb, "Fill", UIShapes.Circle(128), UIShapes.EnergyYellow, false);
        energyFill.rectTransform.sizeDelta = new Vector2(120f, 120f);

        energyText = BattleUI.Text(energyOrb, "Text", "", 46f, UIShapes.Hex("1A1A1A"), FontStyles.Bold);
        energyText.rectTransform.sizeDelta = new Vector2(140f, 80f);

        TMP_Text label = BattleUI.Text(energyOrb, "Label", "ENERGY", 20f, UIShapes.TextMuted, FontStyles.Bold);
        label.characterSpacing = 6f;
        label.rectTransform.sizeDelta = new Vector2(200f, 30f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -92f);
    }

    private void RefreshEnergy()
    {
        if (deck == null || energyText == null)
        {
            return;
        }
        int energy = deck.CurrentEnergy;
        if (energy == lastEnergy)
        {
            return;
        }
        energyText.text = $"{energy}/{deck.MaxEnergy}";
        energyFill.color = energy > 0 ? UIShapes.EnergyYellow : UIShapes.Hex("5A5D6B");
        if (lastEnergy != int.MinValue)
        {
            UITween.Punch(energyOrb, 0.15f);
        }
        lastEnergy = energy;
    }

    private void StylePile(string objectName, string label, bool isDiscard)
    {
        RectTransform pile = canvas.transform.Find(objectName) as RectTransform;
        if (pile == null)
        {
            return;
        }

        pile.sizeDelta = new Vector2(150f, 180f);
        Image image = pile.GetComponent<Image>();
        if (image != null)
        {
            if (deck != null && deck.CardBackSprite != null)
            {
                image.sprite = deck.CardBackSprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
                image.color = isDiscard ? new Color(0.6f, 0.62f, 0.72f) : Color.white;
            }
            else
            {
                image.sprite = UIShapes.RoundedRect(18);
                image.type = Image.Type.Sliced;
                image.color = UIShapes.Hex("23252B");
            }
        }

        // Count goes into a round badge on the corner.
        TMP_Text count = pile.GetComponentInChildren<TMP_Text>(true);
        Image badge = BattleUI.Box(pile, "CountBadge", UIShapes.Circle(128), isDiscard ? UIShapes.Hex("5A5D6B") : UIShapes.Accent, false);
        RectTransform badgeRect = badge.rectTransform;
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(isDiscard ? 0.12f : 0.88f, 0.9f);
        badgeRect.sizeDelta = new Vector2(58f, 58f);
        BattleUI.Shadow(badge, 3f);
        if (count != null)
        {
            count.transform.SetParent(badgeRect, false);
            BattleUI.Stretch(count.rectTransform);
            count.fontSize = 28f;
            count.fontStyle = FontStyles.Bold;
            count.color = Color.white;
            count.alignment = TextAlignmentOptions.Center;
        }

        TMP_Text caption = BattleUI.Text(pile, "Caption", label, 20f, UIShapes.TextMuted, FontStyles.Bold);
        caption.characterSpacing = 6f;
        RectTransform captionRect = caption.rectTransform;
        captionRect.anchorMin = captionRect.anchorMax = new Vector2(0.5f, 0f);
        captionRect.pivot = new Vector2(0.5f, 1f);
        captionRect.anchoredPosition = new Vector2(0f, 2f);
        captionRect.sizeDelta = new Vector2(200f, 30f);
    }

    private void StyleEndTurn()
    {
        RectTransform button = canvas.transform.Find("EndTurnButton") as RectTransform;
        if (button == null)
        {
            return;
        }

        button.anchorMin = button.anchorMax = new Vector2(1f, 0f);
        button.pivot = new Vector2(1f, 0f);
        button.anchoredPosition = new Vector2(-40f, 262f);
        button.sizeDelta = new Vector2(250f, 84f);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = UIShapes.RoundedRect(24);
            image.type = Image.Type.Sliced;
            image.color = UIShapes.Accent;
            BattleUI.Shadow(image);
        }

        Button uiButton = button.GetComponent<Button>();
        if (uiButton != null)
        {
            ColorBlock colors = uiButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
            colors.selectedColor = Color.white;
            uiButton.colors = colors;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "END TURN";
            text.fontSize = 30f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 4f;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;
            BattleUI.Stretch(text.rectTransform);
        }
    }

    // ---------- Hit effects ----------

    private void OnDamaged(EntityVisual visual, int hpLost, int blocked)
    {
        if (hpLost > 0)
        {
            if (visual != null)
            {
                visual.PlayHit(hpLost);
            }
            Float(visual, $"-{hpLost}", UIShapes.HpFill, 72f, 0f);
        }
        if (blocked > 0)
        {
            Float(visual, $"{blocked} BLOCKED", UIShapes.BlockBlue, 36f, -70f);
        }
    }

    private void Float(EntityVisual visual, string text, Color color, float size, float yOffset)
    {
        if (visual == null || visual.Body == null || Camera.main == null || canvas == null)
        {
            return;
        }
        Vector3 screen = Camera.main.WorldToScreenPoint(visual.Body.bounds.center);
        UITween.FloatingText(canvas.transform, (Vector2)screen + new Vector2(0f, yOffset), text, color, size);
    }

    // ---------- Old visuals and font ----------

    // The old pixel heart and slider live on the same objects as the BattleEntity scripts,
    // so they're hidden (graphics off) rather than deleted.
    private static void HideOldBar(BattleEntity entity)
    {
        if (entity == null)
        {
            return;
        }
        foreach (Graphic graphic in entity.GetComponentsInChildren<Graphic>(true))
        {
            graphic.enabled = false;
        }
    }

    // Uses the project's default TMP font everywhere (Project Settings > TextMesh Pro > Settings).
    private void ApplyDefaultFont()
    {
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
        {
            return;
        }
        foreach (TMP_Text text in canvas.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.font != font)
            {
                text.font = font;
            }
        }
    }
}
