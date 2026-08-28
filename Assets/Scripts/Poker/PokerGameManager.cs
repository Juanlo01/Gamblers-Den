using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexasHoldem.Logic.GameMechanics;
using TexasHoldem.Logic.Players;
using TexasHoldem.AI.SmartPlayer;
using TexasHoldem.AI.DummyPlayer;

public class PokerGameManager : MonoBehaviour
{
    public static PokerGameManager Instance { get; private set; }

    [SerializeField] private float npcActionDelaySeconds = 1.5f;
    [SerializeField] private int initialMoney = 1000;

    [Header("CPU Seats")]
    [SerializeField] private CPU_Controller bot1Controller;
    [SerializeField] private CPU_Controller bot2Controller;
    [SerializeField] private CPU_Controller bot3Controller;

    private int currentScore;
    private int bestScore;

    // Seat order the table was dealt in, used to work out each player's
    // position relative to whoever holds the button in the current hand.
    private List<string> seatOrder;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentScore = initialMoney;
        bestScore = initialMoney;

        var humanPlayer = new HumanPlayer("You");
        var players = new List<IPlayer>{
            humanPlayer,
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 1", bot1Controller),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 2", bot2Controller),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 3", bot3Controller)
        };
        seatOrder = players.Select(p => p.Name).ToList();

        var game = new TexasHoldemGame(players, initialMoney);

        Task.Run(() =>
        {
            var winner = game.Start();
            ThreadManager.Enqueue(() => Debug.Log($"Game over. Winner: {winner.Name}"));
        });
    }

    // Works out playerName's position for the current hand, given the name of
    // whoever holds the button (IStartHandContext.FirstPlayerName).
    public TablePosition GetPosition(string playerName, string buttonName)
    {
        int buttonIndex = seatOrder.IndexOf(buttonName);
        int seatIndex = seatOrder.IndexOf(playerName);
        int offset = ((seatIndex - buttonIndex) % seatOrder.Count + seatOrder.Count) % seatOrder.Count;

        return offset switch
        {
            0 => TablePosition.Button,
            1 => TablePosition.SmallBlind,
            2 => TablePosition.BigBlind,
            _ => TablePosition.Cutoff,
        };
    }

    // Called from the main thread whenever the human player's money changes.
    public void UpdateScore(int money)
    {
        currentScore = money;
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
        }
    }

    public int GetCurrentScore() => currentScore;

    public int GetBestScore() => bestScore;
}
