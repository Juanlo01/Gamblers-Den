namespace TexasHoldem.Logic.Players
{
    using System.Collections;

    public abstract class PlayerDecorator : IPlayer
    {
        protected PlayerDecorator(IPlayer player)
        {
            this.Player = player;
        }

        public virtual string Name => this.Player.Name;

        public int BuyIn => this.Player.BuyIn;

        protected IPlayer Player { get; }

        public virtual void StartGame(IStartGameContext context)
        {
            this.Player.StartGame(context);
        }

        public virtual void StartHand(IStartHandContext context)
        {
            this.Player.StartHand(context);
        }

        public virtual void StartRound(IStartRoundContext context)
        {
            this.Player.StartRound(context);
        }

        public virtual PlayerAction PostingBlind(IPostingBlindContext context)
        {
            return this.Player.PostingBlind(context);
        }

        public virtual PlayerAction GetTurn(IGetTurnContext context)
        {
            return this.Player.GetTurn(context);
        }

        // MODIFIED (project): these forward to the wrapped player's coroutine so
        // that a decorator around someone who waits (InternalPlayer around the
        // human) still waits. NOTE: that means overriding only the synchronous
        // GetTurn/PostingBlind above is NOT enough for a decorator that changes
        // the decision - the engine calls these, so an override there would be
        // skipped. Such a decorator must override these too (see CheatingPlayer).
        public virtual IEnumerator PostingBlindRoutine(IPostingBlindContext context, TurnResult result)
        {
            return this.Player.PostingBlindRoutine(context, result);
        }

        public virtual IEnumerator GetTurnRoutine(IGetTurnContext context, TurnResult result)
        {
            return this.Player.GetTurnRoutine(context, result);
        }

        public virtual void EndRound(IEndRoundContext context)
        {
            this.Player.EndRound(context);
        }

        public virtual void EndHand(IEndHandContext context)
        {
            this.Player.EndHand(context);
        }

        public virtual void EndGame(IEndGameContext context)
        {
            this.Player.EndGame(context);
        }
    }
}
