using TexasHoldem.Logic.Players;

public enum TablePosition
{
    Button,
    SmallBlind,
    BigBlind,
    Cutoff
}

// Per-update snapshot of a player's status at the table, published to a CPU_Controller.
public struct PlayerTableStatus
{
    public PlayerAction LastAction;
    public bool IsFolded;
    public int Money;
    public int CurrentBet;
    public TablePosition Position;
}
