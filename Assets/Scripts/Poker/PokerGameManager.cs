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

    // Optional: drag the seat GameObject (with a CPU_Controller, e.g. CPU1/CPU2/CPU3)
    // that visually represents each bot, so its cheat mechanic can grant it a poker edge.
    [SerializeField] private CPU_Controller bot1Seat;
    [SerializeField] private CPU_Controller bot2Seat;
    [SerializeField] private CPU_Controller bot3Seat;

    void Start()
    {
        var humanPlayer = new HumanPlayer("You");
        var players = new List<IPlayer>{
            humanPlayer,
            new SlowedPlayer(MakeBot(bot1Seat, humanPlayer), npcActionDelaySeconds, "Bot 1"),
            new SlowedPlayer(MakeBot(bot2Seat, humanPlayer), npcActionDelaySeconds, "Bot 2"),
            new SlowedPlayer(MakeBot(bot3Seat, humanPlayer), npcActionDelaySeconds, "Bot 3")
        };
        var game = new TexasHoldemGame(players, initialMoney: 1000);

        Task.Run(() =>
        {
            try
            {
                var winner = game.Start();
                ThreadManager.Enqueue(() => Debug.Log($"Game over. Winner: {winner.Name}"));
            }
            catch (System.Exception ex)
            {
                // Without this, an exception on the engine thread dies silently
                // and the game just appears to freeze after the last UI update.
                Debug.LogException(ex);
            }
        });
    }

    private static IPlayer MakeBot(CPU_Controller seat, HumanPlayer human)
    {
        return new CheatingPlayer(new SmartPlayer(), seat, human);
    }
}
