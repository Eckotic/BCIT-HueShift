using UnityEngine;

public static class CardManager : object
{

    public static void playerAttacksEnemy(int x)
    {
        Debug.Log("I attacked the enemy for " + x + " damage!");
    }
    public static void playerGainsBlock(int x)
    {
        Debug.Log("I gained " + x + " block!");
    }
    public static void playerGainsEnergy(int x)
    {
        Debug.Log("I gained " + x + " energy!");
    }
}
