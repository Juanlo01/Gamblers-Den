namespace TexasHoldem.Logic.Players
{
    using System.Collections.Generic;

    using TexasHoldem.Logic.Cards;

    public class EndHandContext : IEndHandContext
    {
        public EndHandContext(
            Dictionary<string, ICollection<Card>> showdownCards,
            IReadOnlyDictionary<string, int> winnings)
        {
            this.ShowdownCards = showdownCards;
            this.Winnings = winnings;
        }

        public Dictionary<string, ICollection<Card>> ShowdownCards { get; private set; }

        public IReadOnlyDictionary<string, int> Winnings { get; private set; }
    }
}
