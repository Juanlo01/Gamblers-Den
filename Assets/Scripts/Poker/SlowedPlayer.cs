using System.Threading;
using TexasHoldem.Logic.Players;

// Wraps an IPlayer (e.g. SmartPlayer) so its actions are announced in the UI
// and the engine thread pauses briefly afterward, giving time to read them.
public class SlowedPlayer : PlayerDecorator
{
    private readonly int _delayMs;
    private readonly string _displayName;

    public SlowedPlayer(IPlayer player, float delaySeconds, string displayName = null) : base(player)
    {
        _delayMs = (int)(delaySeconds * 1000);
        _displayName = displayName;
    }

    public override string Name => _displayName ?? base.Name;

    public override PlayerAction PostingBlind(IPostingBlindContext context)
    {
        var action = base.PostingBlind(context);
        Announce(action);
        return action;
    }

    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        var action = base.GetTurn(context);
        Announce(action);
        return action;
    }

    private void Announce(PlayerAction action)
    {
        ThreadManager.Enqueue(() => PokerUIController.Instance.ShowNpcAction(Name, action));
        Thread.Sleep(_delayMs);
    }
}
