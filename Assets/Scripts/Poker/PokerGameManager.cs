using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Logic.GameMechanics;
using TexasHoldem.Logic.Players;
using TexasHoldem.AI.SmartPlayer;
using TexasHoldem.AI.DummyPlayer;

public class PokerGameManager : MonoBehaviour
{
    [SerializeField] private float npcActionDelaySeconds = 1.5f;

    void Start()
    {
        var humanPlayer = new HumanPlayer("You");
        var players = new List<IPlayer>{
            humanPlayer,
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 1"),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 2"),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 3"),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 4")
        };
        var game = new TexasHoldemGame(players, initialMoney: 1000);

        Task.Run(() =>
        {
            var winner = game.Start();
            ThreadManager.Enqueue(() => Debug.Log($"Game over. Winner: {winner.Name}"));
        });
    }
}
