using System.Threading;
using TexasHoldem.Logic.Players;

// Wraps an IPlayer (e.g. SmartPlayer) so its actions are announced in the UI,
// its table status is published to a CPU_Controller, and the engine thread
// pauses briefly afterward, giving time to read them.
public class SlowedPlayer : PlayerDecorator
{
    private readonly int _delayMs;
    private readonly string _displayName;
    private readonly CPU_Controller _cpuController;
    private readonly int _seatIndex;

    private PlayerAction _lastAction;
    private bool _isFolded;
    private int _money;
    private int _currentBet;
    private TablePosition _position;

    public SlowedPlayer(IPlayer player, float delaySeconds, string displayName = null, CPU_Controller cpuController = null, int seatIndex = 0) : base(player)
    {
        _delayMs = (int)(delaySeconds * 1000);
        _displayName = displayName;
        _cpuController = cpuController;
        _seatIndex = seatIndex;
    }

    public override string Name => _displayName ?? base.Name;

    public override void StartHand(IStartHandContext context)
    {
        base.StartHand(context);

        _isFolded = false;
        _currentBet = 0;
        _lastAction = null;
        _money = context.MoneyLeft;
        _position = PokerGameManager.Instance.GetPosition(Name, context.FirstPlayerName);
        Publish();
    }

    public override void StartRound(IStartRoundContext context)
    {
        base.StartRound(context);

        _currentBet = 0;
        _money = context.MoneyLeft;
        Publish();
    }

    public override PlayerAction PostingBlind(IPostingBlindContext context)
    {
        var action = base.PostingBlind(context);
        Announce(action);

        _lastAction = action;
        _currentBet = action.Money;
        _money = context.CurrentStackSize;
        Publish();

        return action;
    }

    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        var action = base.GetTurn(context);

        // The turn is "on screen" for the length of the Announce delay below, so
        // the dialogue opens before that pause and closes once it has elapsed.
        if (_seatIndex > 0)
        {
            int seat = _seatIndex;
            UnityEngine.Debug.Log($"[Dialogue] SlowedPlayer '{Name}' queueing turn START for seat {seat} (engine thread)");
            ThreadManager.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[Dialogue] -> main thread: OnCpuTurnStarted({seat})");
                PokerGameManager.Instance.OnCpuTurnStarted(seat);
            });
        }

        Announce(action);

        if (_seatIndex > 0)
        {
            int seat = _seatIndex;
            UnityEngine.Debug.Log($"[Dialogue] SlowedPlayer '{Name}' queueing turn END for seat {seat} (engine thread)");
            ThreadManager.Enqueue(() =>
            {
                UnityEngine.Debug.Log($"[Dialogue] -> main thread: OnCpuTurnEnded({seat})");
                PokerGameManager.Instance.OnCpuTurnEnded(seat);
            });
        }

        _lastAction = action;
        _isFolded = action.Type == PlayerActionType.Fold;
        _currentBet = action.Type switch
        {
            PlayerActionType.Raise => context.CurrentMaxBet + action.Money,
            PlayerActionType.CheckCall => context.CurrentMaxBet,
            _ => _currentBet,
        };
        _money = context.MoneyLeft;
        Publish();

        return action;
    }

    private void Announce(PlayerAction action)
    {
        ThreadManager.Enqueue(() => PokerUIController.Instance.ShowNpcAction(Name, action));
        Thread.Sleep(_delayMs);
    }

    private void Publish()
    {
        if (_cpuController == null) return;

        var status = new PlayerTableStatus
        {
            LastAction = _lastAction,
            IsFolded = _isFolded,
            Money = _money,
            CurrentBet = _currentBet,
            Position = _position
        };

        ThreadManager.Enqueue(() => _cpuController.UpdateStatus(status));
    }
}
