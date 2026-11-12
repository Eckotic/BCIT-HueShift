using System;
using DG.Tweening;
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
    private BattleEntity playerEntity;
    private BattleEntity enemyEntity;
    private RectTransform playerPanelRect;
    private RectTransform enemyPanelRect;
    private Image energyGlow;
    private Image energyOuterRing;
    private Image energyInnerRing;
    private RectTransform energyPips;
    private RectTransform endTurnRect;
    private Button endTurnButton;

    private const float PileWidth = 230f;
    private const float PileHeight = 276f;

    // Greys out End Turn while the enemy is acting.
    public void SetEndTurnInteractable(bool interactable)
    {
        if (endTurnButton != null)
        {
            endTurnButton.interactable = interactable;
        }
    }

    // Screen point at the middle of the bottom edge of the enemy's status panel.
    public Vector3 EnemyPanelBottomCentre()
    {
        if (enemyPanelRect == null)
        {
            return Vector3.zero;
        }
        Vector3[] corners = new Vector3[4];
        enemyPanelRect.GetWorldCorners(corners);
        return (corners[0] + corners[3]) / 2f;
    }

    // Screen y of the bottom edge of the enemy's status panel (so labels can stay below it).
    public float EnemyPanelBottom()
    {
        if (enemyPanelRect == null)
        {
            return float.MaxValue;
        }
        Vector3[] corners = new Vector3[4];
        enemyPanelRect.GetWorldCorners(corners);
        return corners[0].y;
    }

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

        playerEntity = player;
        enemyEntity = enemy;
        if (player != null)
        {
            playerPanel = BuildPanel(player, true);
            player.OnDamaged += (hp, blocked) => OnDamaged(player, playerVisual, hp, blocked);
            player.OnBlockGained += amount => OnBlock(playerVisual, amount);
            player.OnHealed += amount => Float(playerVisual, $"+{amount}", UIShapes.HealGreen, 56f, 0f);
        }
        if (enemy != null)
        {
            enemyPanel = BuildPanel(enemy, false);
            enemy.OnDamaged += (hp, blocked) => OnDamaged(enemy, enemyVisual, hp, blocked);
            enemy.OnBlockGained += amount => OnBlock(enemyVisual, amount);
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
        if (deck != null)
        {
            deck.PlayAnimation = AnimatePlay;
            deck.DiscardAnimation = AnimateDiscard;
        }

        IntroAnimation();

        Material spriteMaterial = null;
        if (enemyVisual != null && enemyVisual.Body != null)
        {
            spriteMaterial = enemyVisual.Body.sharedMaterial;
        }
        else if (playerVisual != null && playerVisual.Body != null)
        {
            spriteMaterial = playerVisual.Body.sharedMaterial;
        }
        BattleBackdrop backdrop = BattleBackdrop.Create(Camera.main, spriteMaterial);
        if (backdrop != null)
        {
            backdrop.AddBacklight(playerVisual);
            backdrop.AddBacklight(enemyVisual);
        }
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

        Image root = BattleUI.Box(canvas.transform, isPlayer ? "PlayerStatus" : "EnemyStatus", UIShapes.FramedPanel(22, 3), Color.white);
        RectTransform rect = root.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = isPlayer ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rect.anchoredPosition = isPlayer ? new Vector2(32f, -28f) : new Vector2(-32f, -28f);
        rect.sizeDelta = new Vector2(560f, 150f);
        BattleUI.Shadow(root);
        if (isPlayer)
        {
            playerPanelRect = rect;
        }
        else
        {
            enemyPanelRect = rect;
        }

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

    // A rune orb: amber gem in the middle, two gold rune rings turning in opposite directions,
    // a breathing glow, the number on the gem, and a row of diamond pips (one per energy).
    private void BuildEnergyOrb()
    {
        energyOrb = BattleUI.Rect(canvas.transform, "EnergyOrb");
        energyOrb.anchorMin = energyOrb.anchorMax = new Vector2(0f, 0f);
        energyOrb.anchoredPosition = new Vector2(145f, 440f);
        energyOrb.sizeDelta = new Vector2(124f, 124f);

        energyGlow = BattleUI.Box(energyOrb, "Glow", UIShapes.Glow(128), new Color(1f, 0.72f, 0.25f, 0.4f), false);
        energyGlow.rectTransform.sizeDelta = new Vector2(210f, 210f);
        energyGlow.DOFade(0.18f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(energyGlow.gameObject);
        energyGlow.rectTransform.DOScale(1.1f, 1.4f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(energyGlow.gameObject);

        energyOuterRing = BattleUI.Box(energyOrb, "OuterRunes", UIShapes.RuneRing(256, 12), UIShapes.Gold, false);
        energyOuterRing.rectTransform.sizeDelta = new Vector2(124f, 124f);
        energyOuterRing.rectTransform.DORotate(new Vector3(0f, 0f, -360f), 40f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1).SetLink(energyOuterRing.gameObject);

        energyInnerRing = BattleUI.Box(energyOrb, "InnerRunes", UIShapes.RuneRing(256, 8), new Color(UIShapes.GoldLight.r, UIShapes.GoldLight.g, UIShapes.GoldLight.b, 0.55f), false);
        energyInnerRing.rectTransform.sizeDelta = new Vector2(99f, 99f);
        energyInnerRing.rectTransform.DORotate(new Vector3(0f, 0f, 360f), 25f, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1).SetLink(energyInnerRing.gameObject);

        energyFill = BattleUI.Box(energyOrb, "Core", UIShapes.EnergyCore(256), Color.white, false);
        energyFill.rectTransform.sizeDelta = new Vector2(77f, 77f);

        TextMeshProUGUI number = BattleUI.Text(energyOrb, "Text", "", 44f, UIShapes.Hex("2B1405"), FontStyles.Bold);
        number.rectTransform.sizeDelta = new Vector2(80f, 60f);
        energyText = number;

        energyPips = BattleUI.Rect(energyOrb, "Pips");
        energyPips.anchoredPosition = new Vector2(0f, -78f);
        energyPips.sizeDelta = new Vector2(160f, 18f);
        HorizontalLayoutGroup pipLayout = energyPips.gameObject.AddComponent<HorizontalLayoutGroup>();
        pipLayout.spacing = 6f;
        pipLayout.childAlignment = TextAnchor.MiddleCenter;
        pipLayout.childControlWidth = false;
        pipLayout.childControlHeight = false;
        pipLayout.childForceExpandWidth = false;
        pipLayout.childForceExpandHeight = false;

        TMP_Text label = BattleUI.Text(energyOrb, "Label", "ENERGY", 17f, UIShapes.Gold, FontStyles.Bold);
        label.characterSpacing = 8f;
        label.rectTransform.sizeDelta = new Vector2(200f, 24f);
        label.rectTransform.anchoredPosition = new Vector2(0f, -100f);
    }

    private void RefreshPips(int energy, int max)
    {
        int count = Mathf.Max(energy, max);
        while (energyPips.childCount < count)
        {
            Image pip = BattleUI.Box(energyPips, "Pip", UIShapes.Diamond(true), UIShapes.Gold, false);
            pip.rectTransform.sizeDelta = new Vector2(16f, 16f);
        }
        for (int i = 0; i < energyPips.childCount; i++)
        {
            Image pip = energyPips.GetChild(i).GetComponent<Image>();
            pip.gameObject.SetActive(i < count);
            bool lit = i < energy;
            pip.sprite = UIShapes.Diamond(lit);
            pip.color = lit ? (i >= max ? UIShapes.GoldLight : UIShapes.Gold) : new Color(UIShapes.Gold.r, UIShapes.Gold.g, UIShapes.Gold.b, 0.45f);
        }
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
        energyText.text = energy.ToString();
        RefreshPips(energy, deck.MaxEnergy);
        bool hasEnergy = energy > 0;
        energyFill.color = hasEnergy ? Color.white : new Color(0.4f, 0.4f, 0.45f);
        energyText.color = hasEnergy ? UIShapes.Hex("2B1405") : UIShapes.Hex("D0D0D8");
        energyOuterRing.color = hasEnergy ? UIShapes.Gold : new Color(UIShapes.Gold.r, UIShapes.Gold.g, UIShapes.Gold.b, 0.35f);
        if (energyGlow != null)
        {
            energyGlow.enabled = hasEnergy;
        }
        if (lastEnergy != int.MinValue)
        {
            energyOrb.DOKill(true);
            energyOrb.localScale = Vector3.one;
            energyOrb.DOPunchScale(Vector3.one * (energy > lastEnergy ? 0.3f : 0.15f), 0.35f, 7, 0.6f);
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

        // Close to the size of a card in hand, in the bottom corners.
        pile.anchorMin = pile.anchorMax = pile.pivot = isDiscard ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
        pile.anchoredPosition = isDiscard ? new Vector2(-30f, 40f) : new Vector2(30f, 40f);
        pile.sizeDelta = new Vector2(PileWidth, PileHeight);

        // A second card back peeking out behind, so it reads as a stack.
        if (deck != null && deck.CardBackSprite != null)
        {
            Image under = BattleUI.Box(pile, "StackBehind", deck.CardBackSprite, isDiscard ? new Color(0.45f, 0.46f, 0.55f) : new Color(0.7f, 0.7f, 0.75f), false);
            under.preserveAspect = true;
            BattleUI.Stretch(under.rectTransform);
            under.rectTransform.anchoredPosition = new Vector2(isDiscard ? -8f : 8f, -6f);
            under.rectTransform.localRotation = Quaternion.Euler(0f, 0f, isDiscard ? 4f : -4f);
            under.transform.SetAsFirstSibling();
            // The pile's own Image draws first (it's the parent), so draw the top card as a child instead.
            Image top = BattleUI.Box(pile, "TopCard", deck.CardBackSprite, isDiscard ? new Color(0.6f, 0.62f, 0.72f) : Color.white, false);
            top.preserveAspect = true;
            BattleUI.Stretch(top.rectTransform);
            top.transform.SetSiblingIndex(1);
        }

        Image image = pile.GetComponent<Image>();
        if (image != null)
        {
            if (deck != null && deck.CardBackSprite != null)
            {
                // The pile's own Image becomes an invisible click area (the draw pile is still a button).
                image.sprite = null;
                image.color = new Color(1f, 1f, 1f, 0f);
            }
            else
            {
                image.sprite = UIShapes.RoundedRect(18);
                image.type = Image.Type.Sliced;
                image.color = UIShapes.Hex("23252B");
            }
        }

        // Count goes into a round badge on the corner.
        // Count in a gold-rimmed badge on the inner top corner.
        TMP_Text count = pile.GetComponentInChildren<TMP_Text>(true);
        Image rim = BattleUI.Box(pile, "CountBadge", UIShapes.Circle(128), UIShapes.Gold, false);
        RectTransform badgeRect = rim.rectTransform;
        badgeRect.anchorMin = badgeRect.anchorMax = new Vector2(isDiscard ? 0.14f : 0.86f, 0.88f);
        badgeRect.sizeDelta = new Vector2(74f, 74f);
        BattleUI.Shadow(rim, 4f);
        Image inner = BattleUI.Box(badgeRect, "Inner", UIShapes.Circle(128), UIShapes.Navy, false);
        BattleUI.Stretch(inner.rectTransform, 5f);
        if (count != null)
        {
            count.transform.SetParent(badgeRect, false);
            count.transform.SetAsLastSibling();
            BattleUI.Stretch(count.rectTransform);
            count.fontSize = 34f;
            count.fontStyle = FontStyles.Bold;
            count.color = UIShapes.GoldLight;
            count.alignment = TextAlignmentOptions.Center;
        }

        TMP_Text caption = BattleUI.Text(pile, "Caption", label, 22f, UIShapes.Gold, FontStyles.Bold);
        caption.characterSpacing = 8f;
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

        // A gold-framed plaque with pointed ends, above the discard pile, centred over it.
        const float width = 300f;
        const float height = 96f;
        button.anchorMin = button.anchorMax = new Vector2(1f, 0f);
        button.pivot = new Vector2(0.5f, 0.5f); // scale from the middle on hover
        button.anchoredPosition = new Vector2(-30f - PileWidth / 2f - 5f, 40f + PileHeight + 30f + height / 2f);
        button.sizeDelta = new Vector2(width, height);
        endTurnRect = button;
        button.gameObject.AddComponent<UIHover>();

        // Soft gold glow behind it that breathes, so it reads as the thing to press.
        // (A sibling placed just before the button, so it draws behind it.)
        Image glow = BattleUI.Box(button.parent, "EndTurnGlow", UIShapes.Glow(128), new Color(UIShapes.Gold.r, UIShapes.Gold.g, UIShapes.Gold.b, 0.28f), false);
        glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = button.anchorMin;
        glow.rectTransform.anchoredPosition = button.anchoredPosition;
        glow.rectTransform.sizeDelta = new Vector2(width * 1.35f, height * 2.4f);
        glow.transform.SetSiblingIndex(button.GetSiblingIndex());
        glow.DOFade(0.1f, 1.3f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetLink(glow.gameObject);

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = UIShapes.Plaque((int)width, (int)height);
            image.type = Image.Type.Simple;
            image.color = Color.white;
            image.alphaHitTestMinimumThreshold = 0f;
            BattleUI.Shadow(image, 6f, 0.5f);
        }

        Button uiButton = button.GetComponent<Button>();
        if (uiButton != null)
        {
            endTurnButton = uiButton;
            ColorBlock colors = uiButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.95f, 0.85f);
            colors.pressedColor = new Color(0.8f, 0.75f, 0.65f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.6f, 0.75f);
            uiButton.colors = colors;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = "END TURN";
            text.fontSize = 34f;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 6f;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(UIShapes.GoldLight, UIShapes.GoldLight, UIShapes.Gold, UIShapes.Gold);
            text.alignment = TextAlignmentOptions.Center;
            BattleUI.Stretch(text.rectTransform);
            text.transform.SetAsLastSibling();
            if (text is TextMeshProUGUI ugui)
            {
                BattleUI.TextShadow(ugui, 0.3f, -0.5f);
            }
        }
    }

    // ---------- Hit effects ----------

    private void OnDamaged(BattleEntity entity, EntityVisual visual, int hpLost, int blocked)
    {
        if (hpLost > 0)
        {
            if (visual != null)
            {
                visual.PlayHit(hpLost);
            }
            Float(visual, $"-{hpLost}", UIShapes.HpFill, 72f, 0f);
            Sfx.Play(t => t.hit);

            // Shards in the attacker's colour fly off the target.
            BattleEntity attacker = entity == playerEntity ? enemyEntity : playerEntity;
            Color shardColor = attacker != null ? UIShapes.ForCardColor(attacker.CurrentColor) : Color.white;
            Burst(visual, shardColor, Mathf.Clamp(6 + hpLost, 8, 18));

            if (entity == playerEntity && Camera.main != null)
            {
                Camera.main.transform.DOComplete();
                Camera.main.transform.DOShakePosition(0.3f, Mathf.Clamp(hpLost / 25f, 0.1f, 0.45f), 25, 90f, false, true);
            }
        }
        if (blocked > 0)
        {
            Float(visual, $"{blocked} BLOCKED", UIShapes.BlockBlue, 36f, -70f);
            if (hpLost == 0)
            {
                Sfx.Play(t => t.blockGain, 0.8f, 0.15f);
            }
        }
    }

    private void OnBlock(EntityVisual visual, int amount)
    {
        Float(visual, $"+{amount} BLOCK", UIShapes.BlockBlue, 44f, 0f);
        Sfx.Play(t => t.blockGain, 0.7f);
        if (visual != null)
        {
            visual.Hop();
        }
    }

    // Little circles, triangles and squares bursting out of a sprite.
    private void Burst(EntityVisual visual, Color color, int count)
    {
        if (visual == null || visual.Body == null || Camera.main == null)
        {
            return;
        }
        Vector2 origin = Camera.main.WorldToScreenPoint(visual.VisibleBounds().center);
        for (int i = 0; i < count; i++)
        {
            Image shard = BattleUI.Box(canvas.transform, "Shard", UIShapes.Outline(i % 3, 64, 14f), color, false);
            RectTransform rect = shard.rectTransform;
            float size = UnityEngine.Random.Range(22f, 46f);
            rect.sizeDelta = new Vector2(size, size);
            rect.position = origin + UnityEngine.Random.insideUnitCircle * 30f;
            rect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            Vector2 direction = UnityEngine.Random.insideUnitCircle.normalized;
            float distance = UnityEngine.Random.Range(140f, 320f);
            float duration = UnityEngine.Random.Range(0.45f, 0.75f);
            Sequence fly = DOTween.Sequence().SetLink(shard.gameObject);
            fly.Append(rect.DOMove(rect.position + (Vector3)(direction * distance), duration).SetEase(Ease.OutCubic));
            fly.Join(rect.DORotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-360f, 360f)), duration, RotateMode.FastBeyond360));
            fly.Join(rect.DOScale(0.2f, duration).SetEase(Ease.InQuad));
            fly.Join(shard.DOFade(0f, duration).SetEase(Ease.InQuad));
            fly.OnComplete(() => Destroy(shard.gameObject));
        }
    }

    // ---------- Card animations ----------

    // A played card pops up, then shoots at its target (the enemy for attacks, you otherwise) and fades.
    private void AnimatePlay(GameObject cardObject, Card card)
    {
        RectTransform rect = cardObject.transform as RectTransform;
        if (rect == null || canvas == null)
        {
            Destroy(cardObject);
            return;
        }
        PrepareFlyingCard(cardObject, rect, out CanvasGroup group);

        bool isAttack = card is AttackExample;
        EntityVisual target = isAttack ? enemyVisual : playerVisual;
        Vector3 targetPos = rect.position + Vector3.up * 300f;
        if (target != null && target.Body != null && Camera.main != null)
        {
            targetPos = Camera.main.WorldToScreenPoint(target.VisibleBounds().center);
        }

        if (isAttack && playerVisual != null && enemyVisual != null)
        {
            playerVisual.Lunge(enemyVisual.transform.position);
        }
        Sfx.Play(t => t.cardPlay, 0.8f, 0.1f);

        Sequence play = DOTween.Sequence().SetLink(cardObject);
        play.Append(rect.DOMove(rect.position + Vector3.up * 90f, 0.12f).SetEase(Ease.OutQuad));
        play.Join(rect.DORotate(Vector3.zero, 0.12f));
        play.Join(rect.DOScale(1.25f, 0.12f).SetEase(Ease.OutQuad));
        play.Append(rect.DOMove(targetPos, 0.24f).SetEase(Ease.InQuad));
        play.Join(rect.DOScale(0.35f, 0.24f).SetEase(Ease.InQuad));
        play.Join(group.DOFade(0f, 0.24f).SetEase(Ease.InQuart));
        play.OnComplete(() => Destroy(cardObject));
    }

    // End of turn: each card in hand flies to the discard pile, one after another.
    private void AnimateDiscard(GameObject cardObject, int index)
    {
        RectTransform rect = cardObject.transform as RectTransform;
        RectTransform pile = canvas != null ? canvas.transform.Find("DiscardPile") as RectTransform : null;
        if (rect == null || pile == null)
        {
            Destroy(cardObject);
            return;
        }
        PrepareFlyingCard(cardObject, rect, out CanvasGroup group);
        Vector3 pileCentre = pile.TransformPoint(pile.rect.center);

        Sequence discard = DOTween.Sequence().SetLink(cardObject);
        discard.AppendInterval(index * 0.06f);
        discard.AppendCallback(() => Sfx.Play(t => t.cardDiscard, 0.35f, 0.15f));
        discard.Append(rect.DOMove(pileCentre, 0.32f).SetEase(Ease.InCubic));
        discard.Join(rect.DOScale(0.3f, 0.32f).SetEase(Ease.InQuad));
        discard.Join(rect.DORotate(new Vector3(0f, 0f, -40f), 0.32f));
        discard.Join(group.DOFade(0f, 0.32f).SetEase(Ease.InQuart));
        discard.OnComplete(() =>
        {
            Destroy(cardObject);
            pile.DOKill(true);
            pile.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 0.5f);
        });
    }

    // Takes a card out of the hand (so the fan closes up) and stops it being clickable.
    private void PrepareFlyingCard(GameObject cardObject, RectTransform rect, out CanvasGroup group)
    {
        Button button = cardObject.GetComponent<Button>();
        if (button != null)
        {
            button.enabled = false;
        }
        group = cardObject.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = cardObject.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = false;
        rect.SetParent(canvas.transform, true);
        rect.SetAsLastSibling();
    }

    // ---------- Banners and intro ----------

    // "YOUR TURN" / "ENEMY TURN" band that sweeps across the middle of the screen.
    public float ShowBanner(string text, Color accent)
    {
        if (canvas == null)
        {
            return 0f;
        }
        Image band = BattleUI.Box(canvas.transform, "Banner", null, new Color(0.05f, 0.06f, 0.09f, 0.82f), false);
        RectTransform bandRect = band.rectTransform;
        bandRect.anchorMin = new Vector2(0f, 0.5f);
        bandRect.anchorMax = new Vector2(1f, 0.5f);
        bandRect.sizeDelta = new Vector2(0f, 150f);
        bandRect.anchoredPosition = new Vector2(0f, 60f);
        bandRect.SetAsLastSibling();
        CanvasGroup group = band.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;

        Image stripeTop = BattleUI.Box(bandRect, "StripeTop", null, accent, false);
        stripeTop.rectTransform.anchorMin = new Vector2(0f, 1f);
        stripeTop.rectTransform.anchorMax = new Vector2(1f, 1f);
        stripeTop.rectTransform.sizeDelta = new Vector2(0f, 6f);
        Image stripeBottom = BattleUI.Box(bandRect, "StripeBottom", null, accent, false);
        stripeBottom.rectTransform.anchorMin = new Vector2(0f, 0f);
        stripeBottom.rectTransform.anchorMax = new Vector2(1f, 0f);
        stripeBottom.rectTransform.sizeDelta = new Vector2(0f, 6f);

        TextMeshProUGUI label = BattleUI.Text(bandRect, "Text", text, 84f, Color.white, FontStyles.Bold);
        label.characterSpacing = 12f;
        label.enableVertexGradient = true;
        label.colorGradient = new VertexGradient(Color.white, Color.white, accent, accent);
        label.rectTransform.sizeDelta = new Vector2(1400f, 140f);

        const float hold = 0.55f;
        Sequence banner = DOTween.Sequence().SetLink(band.gameObject);
        bandRect.localScale = new Vector3(1f, 0f, 1f);
        label.rectTransform.anchoredPosition = new Vector2(-500f, 0f);
        group.alpha = 1f;
        banner.Append(bandRect.DOScaleY(1f, 0.18f).SetEase(Ease.OutQuad));
        banner.Join(label.rectTransform.DOAnchorPosX(0f, 0.35f).SetEase(Ease.OutCubic));
        banner.AppendInterval(hold);
        banner.Append(label.rectTransform.DOAnchorPosX(500f, 0.25f).SetEase(Ease.InCubic));
        banner.Join(group.DOFade(0f, 0.25f));
        banner.OnComplete(() => Destroy(band.gameObject));
        Sfx.Play(t => t.turnStart, 0.6f, 0f);
        return 0.35f + hold; // how long until the banner starts leaving
    }

    // Panels slide in from above, the orb and End Turn pop in.
    private void IntroAnimation()
    {
        SlideIn(playerPanelRect, 0.1f);
        SlideIn(enemyPanelRect, 0.2f);
        if (energyOrb != null)
        {
            energyOrb.localScale = Vector3.zero;
            energyOrb.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.35f);
        }
        if (endTurnRect != null)
        {
            endTurnRect.localScale = Vector3.zero;
            endTurnRect.DOScale(1f, 0.45f).SetEase(Ease.OutBack).SetDelay(0.45f);
        }
    }

    private static void SlideIn(RectTransform rect, float delay)
    {
        if (rect == null)
        {
            return;
        }
        Vector2 target = rect.anchoredPosition;
        rect.anchoredPosition = target + new Vector2(0f, 220f);
        rect.DOAnchorPos(target, 0.5f).SetEase(Ease.OutBack).SetDelay(delay);
    }

    void OnDestroy()
    {
        DOTween.KillAll(false);
    }

    private void Float(EntityVisual visual, string text, Color color, float size, float yOffset)
    {
        if (visual == null || visual.Body == null || Camera.main == null || canvas == null)
        {
            return;
        }
        Vector3 screen = Camera.main.WorldToScreenPoint(visual.VisibleBounds().center);
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

        // The display font has no "/", "+" or "·", so missing characters fall back to Unity's sans font.
        TMP_FontAsset fallback = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (fallback != null && fallback != font)
        {
            if (font.fallbackFontAssetTable == null)
            {
                font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            if (!font.fallbackFontAssetTable.Contains(fallback))
            {
                font.fallbackFontAssetTable.Add(fallback);
            }
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
