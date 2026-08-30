namespace TexasHoldem.Logic.Players
{
    using System.Collections;

    public interface IPlayer
    {
        string Name { get; }

        int BuyIn { get; }

        void StartGame(IStartGameContext context);

        void StartHand(IStartHandContext context);

        void StartRound(IStartRoundContext context);

        PlayerAction PostingBlind(IPostingBlindContext context);

        PlayerAction GetTurn(IGetTurnContext context);

        // MODIFIED (project): coroutine forms of the two decision points. A
        // player that has to wait - a human clicking, or a bot held on screen -
        // does it by yielding instead of blocking a thread, because Unity Web
        // builds are single-threaded and have no thread to spare. Players that
        // decide instantly do not need to implement these: the defaults on
        // BasePlayer just call the synchronous methods above.
        IEnumerator PostingBlindRoutine(IPostingBlindContext context, TurnResult result);

        IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result);

        void EndRound(IEndRoundContext context);

        void EndHand(IEndHandContext context);

        void EndGame(IEndGameContext context);
    }
}
