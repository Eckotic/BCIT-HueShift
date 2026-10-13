using UnityEngine;

public class DefendExample : Card
{
    int block = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("I cost " + cost + " energy and my color is " + color);
        playFromHand();
    }

    public override void playFromHand()
    {
        CardManager.playerGainsBlock(block);
    }
}
