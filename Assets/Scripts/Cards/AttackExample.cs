using UnityEngine;
using System.Collections;

public class AttackExample : Card
{

    [SerializeField] public BattleEntity target;
    int damage = 5;

    // Base damage plus the colour buff the player will have when this resolves.
    int FinalDamage => owner != null ? owner.OutgoingDamage(damage, ColorWhenPlayed) : damage;

    public override string Description => $"Deal {FinalDamage} damage.";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("I cost " + cost + " energy and my color is " + color);
    }

    public override void playFromHand()
    {
        CardManager.playerAttacksEnemy(FinalDamage, target);
    }

}
