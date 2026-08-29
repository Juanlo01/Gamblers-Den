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

    [Tooltip("How long each CPU's turn is held on screen after it decides, in seconds. Consumed by SlowedPlayer.Announce().")]
    [SerializeField] private float npcActionDelaySeconds = 5f;
    [SerializeField] private int initialMoney = 1000;

    [Header("CPU Seats")]
    [SerializeField] private CPU_Controller bot1Controller;
    [SerializeField] private CPU_Controller bot2Controller;
    [SerializeField] private CPU_Controller bot3Controller;

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;

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
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 1", bot1Controller, 1),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 2", bot2Controller, 2),
            new SlowedPlayer(new SmartPlayer(), npcActionDelaySeconds, "Bot 3", bot3Controller, 3)
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

    private CPU_Controller GetController(int seatIndex)
    {
        switch (seatIndex)
        {
            case 1: return bot1Controller;
            case 2: return bot2Controller;
            case 3: return bot3Controller;
            default: return null;
        }
    }

    // Called on the main thread (via ThreadManager) when a CPU seat starts its
    // turn. Asks that CPU for a line and, if it has one, opens its dialogue box.
    public void OnCpuTurnStarted(int seatIndex)
    {
        Debug.Log($"[Dialogue] OnCpuTurnStarted({seatIndex})");

        var controller = GetController(seatIndex);
        if (controller == null)
        {
            Debug.LogWarning($"[Dialogue] ABORT: no CPU_Controller assigned for seat {seatIndex}.", this);
            return;
        }

        string line = controller.CallDialogue();
        if (string.IsNullOrEmpty(line))
        {
            Debug.Log($"[Dialogue] seat {seatIndex} returned an empty line; not opening.");
            return;
        }

        if (dialogueRunner == null)
        {
            Debug.LogWarning("[Dialogue] ABORT: dialogueRunner is not assigned on PokerGameManager.", this);
            return;
        }

        string speaker = controller.CharacterName;
        Debug.Log($"[Dialogue] OpenDialogue({seatIndex}, name=\"{speaker}\", text=\"{line}\")");
        dialogueRunner.OpenDialogue(seatIndex, speaker, line);
    }

    // Called on the main thread when a CPU seat's turn ends.
    public void OnCpuTurnEnded(int seatIndex)
    {
        Debug.Log($"[Dialogue] OnCpuTurnEnded({seatIndex})");

        if (dialogueRunner == null)
        {
            Debug.LogWarning("[Dialogue] ABORT: dialogueRunner is not assigned on PokerGameManager.", this);
            return;
        }

        Debug.Log($"[Dialogue] CloseDialogue({seatIndex})");
        dialogueRunner.CloseDialogue(seatIndex);
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
