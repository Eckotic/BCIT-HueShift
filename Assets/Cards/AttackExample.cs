using UnityEngine;
using System.Collections;

public class AttackExample : Card
{

    int damage = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("I cost " + cost + " energy and my color is " + color);
    }

    public override void playFromHand()
    {
        CardManager.playerAttacksEnemy(damage);
    }

}
