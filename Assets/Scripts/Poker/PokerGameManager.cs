using UnityEngine;
using System.Collections.Generic;
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

    private int currentScore;
    private int bestScore;

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
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 1"),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 2"),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 3")
        };
        var game = new TexasHoldemGame(players, initialMoney);

        Task.Run(() =>
        {
            var winner = game.Start();
            ThreadManager.Enqueue(() => Debug.Log($"Game over. Winner: {winner.Name}"));
        });
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
