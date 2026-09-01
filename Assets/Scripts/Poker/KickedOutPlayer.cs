using System.Collections;
using TexasHoldem.Logic.Players;

// Wraps an IPlayer and forces an unconditional fold on every turn while this
// seat's CPU_Controller reports it as kicked out (caught cheating; the seat is
// visually empty or mid-swap until the next hand's Replenish()). The wrapped
// player is never even asked for a decision, so no cheat-advantage or strategy
// logic downstream can override the fold.
//
// This must wrap OUTSIDE CheatingPlayer, not inside it: if CheatingPlayer ran
// first it could see "fold" as the honest action and cash in an uncaught cheat
// by turning it into a call - exactly the kind of decision a kicked-out seat
// must not get to make.
//
// Money is untouched here on purpose: this is the same InternalPlayer/seat for
// the whole game (only the on-stage NPC model swaps), so whatever chips it had
// when caught are exactly what the next character at this seat plays with -
// nothing needs to be copied or handed over.
public class KickedOutPlayer : PlayerDecorator
{
    private readonly CPU_Controller _seat;

    public KickedOutPlayer(IPlayer player, CPU_Controller seat) : base(player)
    {
        _seat = seat;
    }

    private bool IsKickedOut => _seat != null && _seat.IsKickedOutThisHand;

    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        return IsKickedOut ? PlayerAction.Fold() : base.GetTurn(context);
    }

    public override IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result)
    {
        if (IsKickedOut)
        {
            result.Action = PlayerAction.Fold();
            yield break;
        }

        yield return base.GetTurnRoutine(context, result);
    }
}
