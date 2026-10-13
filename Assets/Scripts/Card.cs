using UnityEngine;
using System.Collections;

public class Card : MonoBehaviour
{

    [SerializeField] protected int cost;
    [SerializeField] protected CardColor color;

    public virtual void playFromHand()
    {
        Debug.Log("I'm not yet implemented!");
    }

}

public enum CardColor
{
    Red,
    Orange,
    Yellow,
    Green,
    Blue,
    Purple
}