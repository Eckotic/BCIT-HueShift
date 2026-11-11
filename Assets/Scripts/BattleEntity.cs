using System;
using UnityEngine;

public class BattleEntity : MonoBehaviour
{

    [SerializeField] private int currentHealth;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int block;
    public HealthBar healthBar;

    [Header("Colour")]
    [Tooltip("The colour this entity starts the fight in. Player: White. Each enemy: its own fixed colour.")]
    [SerializeField] private CardColor startingColor = CardColor.White;

    [Header("Colour buffs")]
    [SerializeField] private int redDamageBonus = 3;
    [SerializeField] private int blueBlockBonus = 3;
    [SerializeField] private int purpleDamageBonus = 2;
    [SerializeField] private int purpleBlockBonus = 2;
    [Tooltip("HP recovered at the start of this entity's turn while Green.")]
    [SerializeField] private int greenHealPerTurn = 3;

    private CardColor currentColor;

    public CardColor CurrentColor => currentColor;
    public bool IsDead => currentHealth <= 0;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public int Block => block;

    public event Action<CardColor> OnCardColorChanged;
    // Fired once when health reaches 0.
    public event Action OnDied;
    // (HP actually lost, amount absorbed by block). Used for hit effects and damage numbers.
    public event Action<int, int> OnDamaged;
    public event Action<int> OnBlockGained;
    public event Action<int> OnHealed;

    void Awake()
    {
        currentColor = startingColor;
        currentHealth = maxHealth;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
            healthBar.SetHealth(currentHealth);
        }
        OnCardColorChanged?.Invoke(currentColor);
    }

    // Resets this entity for a new fight (used when the next enemy comes in).
    public void Setup(int newMaxHealth, CardColor color)
    {
        maxHealth = Mathf.Max(1, newMaxHealth);
        currentHealth = maxHealth;
        block = 0;
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }
        SetCardColor(color);
    }

    //TODO: make this look nicer in the future
    public void TakeDamage(int amount)
    {
        if (amount <= 0 || currentHealth <= 0){
            return;
        }

        // Orange: damage received is doubled.
        if (currentColor == CardColor.Orange){
            amount *= 2;
        }

        int absorbed = Mathf.Min(block, amount);
        block -= absorbed;
        int remaining = amount - absorbed;

        if (remaining > 0){
            currentHealth = Mathf.Clamp(currentHealth - remaining, 0, maxHealth);
            if (healthBar != null){
                healthBar.SetHealth(currentHealth);
            }
        }

        Debug.Log($"{gameObject.name} took {amount} damage ({absorbed} absorbed by block). HP: {currentHealth}/{maxHealth}, Block: {block}");
        OnDamaged?.Invoke(remaining, absorbed);

        if (currentHealth <= 0){
            Debug.Log($"{gameObject.name} is now dead!");
            OnDied?.Invoke();
        }
    }

    public void GainBlock(int amount)
    {
        if (amount <= 0){
            return;
        }

        block += amount;
        Debug.Log($"{gameObject.name} Block: {block}");
        OnBlockGained?.Invoke(amount);
    }

    public void ResetBlock()
    {
        block = 0;
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0){
            return;
        }

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        if (healthBar != null){
            healthBar.SetHealth(currentHealth);
        }
        Debug.Log($"{gameObject.name} healed {amount}. HP: {currentHealth}/{maxHealth}");
        OnHealed?.Invoke(amount);
    }

    // ---------- Colour ----------

    // Sets the colour directly (no mixing).
    public void SetCardColor(CardColor cardColor)
    {
        currentColor = cardColor;
        OnCardColorChanged?.Invoke(currentColor);
    }

    // What this entity's colour would become if a card of this colour were played.
    public CardColor PreviewShift(CardColor added)
    {
        return ColorRules.Mix(currentColor, added);
    }

    // Mixes a card's colour into this entity's colour (paint mixing, see ColorRules).
    public void ShiftColor(CardColor added)
    {
        CardColor next = ColorRules.Mix(currentColor, added);
        if (next != currentColor)
        {
            Debug.Log($"{gameObject.name} shifted {currentColor} + {added} = {next}");
        }
        SetCardColor(next);
    }

    // Start-of-turn colour effects. Call once at the start of this entity's turn.
    public void StartOwnTurn()
    {
        if (currentColor == CardColor.Black)
        {
            Debug.Log($"{gameObject.name}'s Black wore off.");
            SetCardColor(CardColor.White);
        }
        else if (currentColor == CardColor.Green)
        {
            Heal(greenHealPerTurn);
        }
    }

    // Short text for the HUD: what this entity's current colour does.
    public string ColorEffectText(bool isPlayer)
    {
        switch (currentColor)
        {
            case CardColor.Red: return $"+{redDamageBonus} damage";
            case CardColor.Blue: return $"+{blueBlockBonus} block";
            case CardColor.Purple: return $"+{purpleDamageBonus} dmg, +{purpleBlockBonus} block";
            case CardColor.Orange: return "Deals and takes x2";
            case CardColor.Green: return $"Heals {greenHealPerTurn} each turn";
            case CardColor.Yellow: return isPlayer ? "+1 energy each turn" : "Acts twice";
            case CardColor.Black: return "Locked until next turn";
            default: return "No effect";
        }
    }

    public int OutgoingDamage(int baseDamage)
    {
        return OutgoingDamage(baseDamage, currentColor);
    }

    // Damage this entity would deal while in the given colour.
    public int OutgoingDamage(int baseDamage, CardColor asColor)
    {
        switch (asColor)
        {
            case CardColor.Red: return baseDamage + redDamageBonus;
            case CardColor.Purple: return baseDamage + purpleDamageBonus;
            case CardColor.Orange: return baseDamage * 2;
            default: return baseDamage;
        }
    }

    // Block this entity would gain while in the given colour.
    public int BlockGain(int baseBlock, CardColor asColor)
    {
        switch (asColor)
        {
            case CardColor.Blue: return baseBlock + blueBlockBonus;
            case CardColor.Purple: return baseBlock + purpleBlockBonus;
            default: return baseBlock;
        }
    }

}
