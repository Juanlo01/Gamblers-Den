using TexasHoldem.Logic.Cards;

public static class CardMapper
{
    public static string ToDisplayText(Card card)
    {
        return $"{card.Type} of {card.Suit}s";
    }
}   