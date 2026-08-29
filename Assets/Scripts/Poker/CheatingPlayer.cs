using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TexasHoldem.Logic.Cards;
using TexasHoldem.Logic.Helpers;
using TexasHoldem.Logic.Players;

// Wraps an IPlayer (e.g. SmartPlayer) and, whenever this bot has an
// uncaught cheat pending, gives it an edge on its next decision:
//
// - Post-flop, if it can see the human's hole cards (the "peek" cheat),
//   it compares its own real hand against the human's real hand and
//   plays accordingly - pressing when ahead, backing off when behind.
// - Otherwise (pre-flop, or no human reference wired up) it falls back
//   to a purely behavioral nudge using only info already on IGetTurnContext:
//   a cheap fold becomes a call, a raise gets pressed harder.
//
// Either way it only ever compares against the human specifically - it
// has no visibility into the other bots' hole cards.
public class CheatingPlayer : PlayerDecorator
{
    private static readonly IHandEvaluator Evaluator = new HandEvaluator();

    private readonly CPU_Controller _seat;
    private readonly HumanPlayer _human;

    private Card _firstCard;
    private Card _secondCard;
    private IReadOnlyCollection<Card> _communityCards = new List<Card>();

    public CheatingPlayer(IPlayer player, CPU_Controller seat, HumanPlayer human = null) : base(player)
    {
        _seat = seat;
        _human = human;
    }

    public override void StartHand(IStartHandContext context)
    {
        _firstCard = context.FirstCard;
        _secondCard = context.SecondCard;
        base.StartHand(context);
    }

    public override void StartRound(IStartRoundContext context)
    {
        _communityCards = context.CommunityCards;
        base.StartRound(context);
    }

    public override PlayerAction GetTurn(IGetTurnContext context)
    {
        var action = base.GetTurn(context);

        // Resolved fresh each turn: whichever NPC currently occupies the seat.
        var cheat = _seat != null ? _seat.ActiveCheatState : null;
        if (cheat == null || !cheat.Active)
        {
            return action;
        }

        cheat.Active = false; // one-shot: spend the cheat on this decision

        if (_human != null && _communityCards.Count >= 3)
        {
            return DecideWithPeek(action, context);
        }

        return Nudge(action, context);
    }

    private PlayerAction DecideWithPeek(PlayerAction honestAction, IGetTurnContext context)
    {
        _human.PeekHoleCards(out var humanFirst, out var humanSecond);

        var myHand = Evaluator.GetBestHand(new[] { _firstCard, _secondCard }.Concat(_communityCards));
        var humanHand = Evaluator.GetBestHand(new[] { humanFirst, humanSecond }.Concat(_communityCards));
        var aheadOfHuman = myHand.CompareTo(humanHand) > 0;

        Debug.Log($"[Cheat] Peeked: mine={myHand.RankType}, human's={humanHand.RankType}, ahead={aheadOfHuman}. Honest action was {honestAction}.");

        if (aheadOfHuman)
        {
            if (honestAction.Type == PlayerActionType.Fold)
            {
                Debug.Log("[Cheat] Ahead of human - turning a fold into a call.");
                return PlayerAction.CheckOrCall();
            }

            if (honestAction.Type == PlayerActionType.CheckCall && context.CanRaise)
            {
                Debug.Log($"[Cheat] Ahead of human - turning a check/call into a raise of {context.MinRaise}.");
                return PlayerAction.Raise(context.MinRaise);
            }

            if (honestAction.Type == PlayerActionType.Raise && context.CanRaise)
            {
                Debug.Log($"[Cheat] Ahead of human - doubling raise to {honestAction.Money * 2}.");
                return PlayerAction.Raise(honestAction.Money * 2);
            }
        }
        else if (honestAction.Type == PlayerActionType.Raise)
        {
            // Behind the human's actual hand - don't overcommit into it.
            Debug.Log("[Cheat] Behind the human - downgrading a raise into a call.");
            return PlayerAction.CheckOrCall();
        }
        else if (honestAction.Type == PlayerActionType.CheckCall && context.MoneyToCall > 0)
        {
            Debug.Log("[Cheat] Behind the human - folding instead of calling.");
            return PlayerAction.Fold();
        }

        Debug.Log("[Cheat] Peeked but nothing to nudge this turn.");
        return honestAction;
    }

    private static PlayerAction Nudge(PlayerAction honestAction, IGetTurnContext context)
    {
        if (honestAction.Type == PlayerActionType.Fold && context.MoneyToCall <= context.CurrentPot / 4)
        {
            return PlayerAction.CheckOrCall();
        }

        if (honestAction.Type == PlayerActionType.Raise && context.CanRaise)
        {
            return PlayerAction.Raise(honestAction.Money * 2);
        }

        return honestAction;
    }
}
