using UnityEngine;

public class DefendExample : Card
{
    [SerializeField] public BattleEntity target;
    int block = 5;

    // Base block plus the colour buff the player will have when this resolves.
    int FinalBlock => owner != null ? owner.BlockGain(block, ColorWhenPlayed) : block;

    public override string Description => $"Gain {FinalBlock} block.";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("I cost " + cost + " energy and my color is " + color);
    }

    public override void playFromHand()
    {
        CardManager.playerGainsBlock(FinalBlock, target);
    }
}
