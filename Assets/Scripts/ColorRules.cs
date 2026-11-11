// How colours mix, like paint.
// Each colour is a set of primaries: White = none, Red/Blue/Yellow = one,
// Purple/Orange/Green = two, Black = all three.
// Mixing adds the new card's primaries to what you already have:
//   White + Red = Red, Red + Blue = Purple, Purple + Red = Purple, Purple + Yellow = Black.
// Black is unshiftable: nothing mixes out of it (it clears to White at the start of the next turn).
public static class ColorRules
{
    private const int RedBit = 1;
    private const int BlueBit = 2;
    private const int YellowBit = 4;

    public static CardColor Mix(CardColor current, CardColor added)
    {
        if (current == CardColor.Black)
        {
            return CardColor.Black;
        }
        return FromPrimaries(ToPrimaries(current) | ToPrimaries(added));
    }

    private static int ToPrimaries(CardColor color)
    {
        switch (color)
        {
            case CardColor.Red: return RedBit;
            case CardColor.Blue: return BlueBit;
            case CardColor.Yellow: return YellowBit;
            case CardColor.Purple: return RedBit | BlueBit;
            case CardColor.Orange: return RedBit | YellowBit;
            case CardColor.Green: return BlueBit | YellowBit;
            case CardColor.Black: return RedBit | BlueBit | YellowBit;
            default: return 0; // White
        }
    }

    private static CardColor FromPrimaries(int primaries)
    {
        switch (primaries)
        {
            case RedBit: return CardColor.Red;
            case BlueBit: return CardColor.Blue;
            case YellowBit: return CardColor.Yellow;
            case RedBit | BlueBit: return CardColor.Purple;
            case RedBit | YellowBit: return CardColor.Orange;
            case BlueBit | YellowBit: return CardColor.Green;
            case RedBit | BlueBit | YellowBit: return CardColor.Black;
            default: return CardColor.White;
        }
    }
}
