using System.Threading;
using TexasHoldem.Logic.Players;

public class HumanPlayer : BasePlayer
{
    public override string Name { get; }
    public override int BuyIn => -1; // use table default

    private readonly ManualResetEventSlim _waitHandle = new ManualResetEventSlim(false);
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
    }

    public override void EndRound(IEndRoundContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.OnRoundEnd());
    }

    public override void EndHand(IEndHandContext context)
    {
        ThreadManager.Enqueue(() =>
            PokerUIController.Instance.ShowShowdown(context.ShowdownCards));
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
}
