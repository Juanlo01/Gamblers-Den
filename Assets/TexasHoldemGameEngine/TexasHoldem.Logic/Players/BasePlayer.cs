namespace TexasHoldem.Logic.Players
{
    using System.Collections;
    using System.Collections.Generic;

    using TexasHoldem.Logic.Cards;

    public abstract class BasePlayer : IPlayer
    {
        public abstract string Name { get; }

        public abstract int BuyIn { get; }

        protected IReadOnlyCollection<Card> CommunityCards { get; private set; }

        protected Card FirstCard { get; private set; }

        protected Card SecondCard { get; private set; }

        public virtual void StartGame(IStartGameContext context)
        {
        }

        public virtual void StartHand(IStartHandContext context)
        {
            this.FirstCard = context.FirstCard;
            this.SecondCard = context.SecondCard;
        }

        public virtual void StartRound(IStartRoundContext context)
        {
            this.CommunityCards = context.CommunityCards;
        }

        public abstract PlayerAction PostingBlind(IPostingBlindContext context);

        public abstract PlayerAction GetTurn(IGetTurnContext context);

        // MODIFIED (project): a player that decides instantly - every bot - needs
        // nothing more than its synchronous method, so these resolve in the same
        // frame without yielding. Only players that genuinely wait override them.
        public virtual IEnumerator PostingBlindRoutine(IPostingBlindContext context, TurnResult result)
        {
            result.Action = this.PostingBlind(context);
            yield break;
        }

        public virtual IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result)
        {
            result.Action = this.GetTurn(context);
            yield break;
        }

        public virtual void EndRound(IEndRoundContext context)
        {
        }

        public virtual void EndHand(IEndHandContext context)
        {
        }

        public virtual void EndGame(IEndGameContext context)
        {
        }
    }
}
