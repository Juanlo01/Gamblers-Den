using System.Collections;
using System.Linq;
using TexasHoldem.Logic.Cards;
using TexasHoldem.Logic.Helpers;
using TexasHoldem.Logic.Players;

public class HumanPlayer : BasePlayer
{
    public override string Name { get; }
    public override int BuyIn => -1; // use table default

    private readonly IHandEvaluator _handEvaluator = new HandEvaluator();
    private PlayerAction _pendingAction;
    private bool _hasSubmitted;

    public HumanPlayer(string name)
    {
        Name = name;
    }

    // Every hook below runs inside the engine coroutine, which Unity drives on
    // the main thread, so these call straight into the UI - no marshalling.
    public override void StartGame(IStartGameContext context)
    {
        PokerUIController.Instance.OnGameStarted();
    }

    public override void StartHand(IStartHandContext context)
    {
        base.StartHand(context);
        PokerUIController.Instance.ShowHoleCards(context.FirstCard, context.SecondCard);
        PokerGameManager.Instance.OnHandStarted(context.HandNumber);
    }

    public override void StartRound(IStartRoundContext context)
    {
        base.StartRound(context);
        PokerUIController.Instance.ShowCommunityCards(context.CommunityCards, context.CurrentPot);
        PokerUIController.Instance.ShowMoney(context.MoneyLeft);
        PokerGameManager.Instance.UpdateScore(context.MoneyLeft);

        // Drives $game_phase - StartRound is the only hook that sees every round.
        PokerGameManager.Instance.OnPhaseChanged(ToYarnPhase(context.RoundType), context.CurrentPot);

        if (context.CommunityCards.Count >= 3)
        {
            var bestHand = _handEvaluator.GetBestHand(
                new[] { FirstCard, SecondCard }.Concat(context.CommunityCards));
            PokerUIController.Instance.ShowHandType(bestHand.RankType);
        }
    }

    public override void EndRound(IEndRoundContext context)
    {
        PokerUIController.Instance.OnRoundEnd();
    }

    public override void EndHand(IEndHandContext context)
    {
        PokerUIController.Instance.ShowShowdown(context);

        // context.MoneyLeft is the true post-hand stack (pot already awarded),
        // unlike the last StartRound snapshot GetCurrentScore() would return -
        // that can read $0 mid-hand for a player who is all-in but still live,
        // long before the hand (and the bust check) is actually decided.
        PokerGameManager.Instance.UpdateScore(context.MoneyLeft);
        PokerGameManager.Instance.OnHandEnded(context.MoneyLeft);
    }

    // Maps the engine's round types onto the $game_phase vocabulary in init.yarn.
    private static string ToYarnPhase(TexasHoldem.Logic.GameRoundType roundType)
    {
        switch (roundType)
        {
            case TexasHoldem.Logic.GameRoundType.PreFlop: return "pre_flop";
            case TexasHoldem.Logic.GameRoundType.Flop: return "flop";
            case TexasHoldem.Logic.GameRoundType.Turn: return "turn";
            case TexasHoldem.Logic.GameRoundType.River: return "river";
            default: return "idle";
        }
    }

    public override void EndGame(IEndGameContext context)
    {
        PokerUIController.Instance.ShowGameOver(context.WinnerName);
    }

    public override PlayerAction PostingBlind(IPostingBlindContext context)
    {
        PokerUIController.Instance.ShowBlindPosted(Name, context.BlindAction);
        return context.BlindAction;
    }

    // The engine never calls this - a human has no answer to give on the spot.
    // GetTurnRoutine below is the real implementation.
    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        throw new System.NotSupportedException(
            "HumanPlayer decides asynchronously - the engine must call GetTurnRoutine.");
    }

    public override IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result)
    {
        _hasSubmitted = false;

        // Clear the table's chatter before handing control over.
        PokerGameManager.Instance.OnPlayerTurnStarted();
        PokerUIController.Instance.ShowActionPrompt(context, this);

        // Hands each frame back to Unity until a button handler calls
        // SubmitAction. This replaces the wait handle the engine used to block a
        // thread on, which no single-threaded (Web) build could ever support.
        while (!_hasSubmitted)
        {
            yield return null;
        }

        PokerGameManager.Instance.OnPlayerAction(SlowedPlayer.ToYarnAction(_pendingAction, context));

        result.Action = _pendingAction;
    }

    // Called by the action buttons' click handlers.
    public void SubmitAction(PlayerAction action)
    {
        _pendingAction = action;
        _hasSubmitted = true;
    }

    // Cheat window: lets a CheatingPlayer see the human's actual hole cards.
    // FirstCard/SecondCard are protected on BasePlayer - this is the one
    // deliberate hole through that encapsulation.
    public void PeekHoleCards(out Card first, out Card second)
    {
        first = FirstCard;
        second = SecondCard;
    }
}
