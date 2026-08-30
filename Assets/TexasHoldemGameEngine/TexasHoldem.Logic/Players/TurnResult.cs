namespace TexasHoldem.Logic.Players
{
    /// <summary>
    /// Carries a player's decision back out of a coroutine, since an
    /// <see cref="System.Collections.IEnumerator"/> cannot return a value.
    /// The engine creates one per decision it asks for.
    /// </summary>
    public class TurnResult
    {
        public PlayerAction Action { get; set; }
    }
}
