using System.Linq;
using System.Threading;
using TexasHoldem.Logic.Cards;
using TexasHoldem.Logic.Helpers;
using TexasHoldem.Logic.Players;

public class HumanPlayer : BasePlayer
{
    public override string Name { get; }
    public override int BuyIn => -1; // use table default

    private readonly ManualResetEventSlim _waitHandle = new ManualResetEventSlim(false);
    private readonly IHandEvaluator _handEvaluator = new HandEvaluator();
    private PlayerAction _pendingAction;

    public HumanPlayer(string name)
    {
        Name = name;
    }

    public override void StartGame(IStartGameContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.OnGameStarted());
    }

    public override void StartHand(IStartHandContext context)
    {
        base.StartHand(context);
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowHoleCards(context.FirstCard, context.SecondCard));
    }

    public override void StartRound(IStartRoundContext context)
    {
        base.StartRound(context);
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowCommunityCards(context.CommunityCards, context.CurrentPot));

        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowMoney(context.MoneyLeft));

        if (context.CommunityCards.Count >= 3)
        {
            var bestHand = _handEvaluator.GetBestHand(
                new[] { FirstCard, SecondCard }.Concat(context.CommunityCards));
            ThreadManager.Enqueue(() =>
                PokerUIController.Instance.ShowHandType(bestHand.RankType));
        }
    }

    public override void EndRound(IEndRoundContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.OnRoundEnd());
    }

    public override void EndHand(IEndHandContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowShowdown(context));
    }

    public override void EndGame(IEndGameContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowGameOver(context.WinnerName));
    }

    public override PlayerAction PostingBlind(IPostingBlindContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowBlindPosted(Name, context.BlindAction));
        return context.BlindAction;
    }

    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        _waitHandle.Reset();

        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowActionPrompt(context, this));

        _waitHandle.Wait(); // blocks the ENGINE thread, not Unity's main thread
        return _pendingAction;
    }

    // Called from the main thread by button click handlers
    public void SubmitAction(PlayerAction action)
    {
        _pendingAction = action;
        _waitHandle.Set();
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
