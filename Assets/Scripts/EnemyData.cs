using System;
using System.Collections.Generic;
using UnityEngine;

// One enemy type: its stats, colour, art and the moves it cycles through.
// Create with Assets > Create > HueShift > Enemy Data, or HueShift > Build Enemies.
[CreateAssetMenu(menuName = "HueShift/Enemy Data", fileName = "Enemy_")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public int maxHealth = 40;
    [Tooltip("Fixed colour for this enemy. Its buffs apply to every move.")]
    public CardColor color = CardColor.Red;

    [Header("Art")]
    [Tooltip("Main sprite. If Tint Body is on, it's tinted with the enemy's colour (use a white fill).")]
    public Sprite bodySprite;
    [Tooltip("Optional outline drawn on top (the Outline child). Leave empty to hide it.")]
    public Sprite outlineSprite;
    [Tooltip("Off for art that is already coloured (Triangle, Circle).")]
    public bool tintBody = true;

    [Header("Moves (repeat in order)")]
    public List<EnemyMove> moves = new List<EnemyMove>();
}

public enum EnemyMoveType
{
    Attack,
    Block
}

[Serializable]
public class EnemyMove
{
    public EnemyMoveType type = EnemyMoveType.Attack;
    public int amount = 5;

    public EnemyMove() { }

    public EnemyMove(EnemyMoveType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
}
