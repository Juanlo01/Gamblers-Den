using System.Collections.Generic;
using TexasHoldem.Logic;

public static class HandRankMapper
{
    private static readonly Dictionary<HandRankType, string> DisplayNames = new Dictionary<HandRankType, string>
    {
        { HandRankType.HighCard, "High Card" },
        { HandRankType.Pair, "Pair" },
        { HandRankType.TwoPairs, "Two Pair" },
        { HandRankType.ThreeOfAKind, "Three of a Kind" },
        { HandRankType.Straight, "Straight" },
        { HandRankType.Flush, "Flush" },
        { HandRankType.FullHouse, "Full House" },
        { HandRankType.FourOfAKind, "Four of a Kind" },
        { HandRankType.StraightFlush, "Straight Flush" },
    };

    public static string ToDisplayText(HandRankType rankType)
    {
        return DisplayNames[rankType];
    }
}
