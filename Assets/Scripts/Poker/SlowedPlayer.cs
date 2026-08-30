using System.Collections;
using UnityEngine;
using TexasHoldem.Logic.Players;

// Wraps an IPlayer (e.g. SmartPlayer) so its actions are announced in the UI,
// its table status is published to a CPU_Controller, and the hand pauses
// briefly afterward, giving time to read them.
//
// The pause is why this overrides the *Routine methods rather than the
// synchronous ones: the engine drives those, and yielding is the only way to
// wait without a thread. The inherited synchronous GetTurn/PostingBlind simply
// forward to the wrapped player and are not used by the engine.
public class SlowedPlayer : PlayerDecorator
{
    private readonly float _delaySeconds;
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
        _delaySeconds = delaySeconds;
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

    public override IEnumerator PostingBlindRoutine(IPostingBlindContext context, TurnResult result)
    {
        var inner = new TurnResult();
        yield return Player.PostingBlindRoutine(context, inner);
        var action = inner.Action;

        yield return Announce(action);

        _lastAction = action;
        _currentBet = action.Money;
        _money = context.CurrentStackSize;
        Publish();

        result.Action = action;
    }

    public override IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result)
    {
        var inner = new TurnResult();
        yield return Player.GetTurnRoutine(context, inner);
        var action = inner.Action;

        // The turn is "on screen" for the length of the Announce delay below, so
        // the dialogue opens before that pause and closes once it has elapsed.
        if (_seatIndex > 0)
        {
            PokerGameManager.Instance.OnCpuTurnStarted(_seatIndex, ToYarnAction(action, context));
        }

        yield return Announce(action);

        if (_seatIndex > 0)
        {
            PokerGameManager.Instance.OnCpuTurnEnded(_seatIndex);
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

        result.Action = action;
    }

    // Maps the engine's action types onto the vocabulary the yarn "react_to"
    // headers use: raise, fold, all_in, call, check. The engine has no separate
    // check/call or all-in type, so they are derived the same way the UI does it.
    public static string ToYarnAction(PlayerAction action, IGetTurnContext context)
    {
        if (action == null) return "none";

        switch (action.Type)
        {
            case PlayerActionType.Fold:
                return "fold";

            case PlayerActionType.Raise:
                return action.Money >= context.MoneyLeft ? "all_in" : "raise";

            case PlayerActionType.CheckCall:
                return context.MoneyToCall <= 0 ? "check" : "call";

            default:
                return "none";
        }
    }

    private IEnumerator Announce(PlayerAction action)
    {
        PokerUIController.Instance.ShowNpcAction(Name, action);
        yield return new WaitForSeconds(_delaySeconds);
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

        _cpuController.UpdateStatus(status);
    }
}
