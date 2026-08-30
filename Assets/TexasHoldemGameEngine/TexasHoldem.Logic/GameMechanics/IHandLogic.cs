namespace TexasHoldem.Logic.GameMechanics
{
    using System.Collections;

    public interface IHandLogic
    {
        // MODIFIED (project): coroutine - see BettingLogic.Bet.
        IEnumerator Play();
    }
}
