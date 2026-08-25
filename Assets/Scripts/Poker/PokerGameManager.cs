using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using TexasHoldem.Logic.GameMechanics;
using TexasHoldem.Logic.Players;
using TexasHoldem.AI.SmartPlayer;
using TexasHoldem.AI.DummyPlayer;

public class PokerGameManager : MonoBehaviour
{
    void Start()
    {
        var players = new List<IPlayer>{
            new SmartPlayer(),
            new SmartPlayer(),
            new SmartPlayer(),
        };
        var game = new TexasHoldemGame(players, initialMoney: 1000);

        Task.Run(() =>
        {
            var winner = game.Start();
            ThreadManager.Enqueue(() => Debug.Log($"Game over. Winner: {winner.Name}"));
        });
    }
}
