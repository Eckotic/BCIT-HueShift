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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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