namespace TexasHoldem.Logic.GameMechanics
{
    using System.Collections;

    using TexasHoldem.Logic.Players;

    public interface ITexasHoldemGame
    {
        int HandsPlayed { get; }

        // MODIFIED (project): the game is now driven as a coroutine, which cannot
        // return a value, so the winner is published here once Start() finishes.
        IPlayer Winner { get; }

        IEnumerator Start();
    }
}
