using TexasHoldem.Logic.Cards;

public static class CardMapper
{
    public static string ToDisplayText(Card card)
    {
        return $"{card.Type} of {card.Suit}s";
    }

    /// <summary>
    /// Maps a card to the file name (without extension) of its sprite in Assets/Resources/Cards.
    /// </summary>
    public static string ToSpriteName(Card card)
    {
        return $"{RankToken(card.Type)}_{SuitToken(card.Suit)}";
    }

    private static string RankToken(CardType type)
    {
        return type switch
        {
            CardType.Ace => "A",
            CardType.King => "K",
            CardType.Queen => "Q",
            CardType.Jack => "J",
            _ => ((int)type).ToString(),
        };
    }

    private static string SuitToken(CardSuit suit)
    {
        return suit switch
        {
            CardSuit.Club => "clubs",
            CardSuit.Diamond => "diamonds",
            CardSuit.Heart => "hearts",
            CardSuit.Spade => "spades",
            _ => suit.ToString().ToLowerInvariant(),
        };
    }
}
