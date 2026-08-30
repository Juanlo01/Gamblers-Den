namespace TexasHoldem.Logic.Players
{
    using System.Collections.Generic;

    using TexasHoldem.Logic.Cards;

    public interface IEndHandContext
    {
        Dictionary<string, ICollection<Card>> ShowdownCards { get; }

        IReadOnlyDictionary<string, int> Winnings { get; }

        // MODIFIED (project): the pot is already awarded by the time EndHand
        // fires, so this is the player's true post-hand stack - unlike
        // IStartRoundContext.MoneyLeft, it reflects this hand's outcome (e.g. an
        // all-in that won the pot back, not just what they last committed).
        int MoneyLeft { get; }
    }
}
