using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexasHoldem.Logic.GameMechanics;
using TexasHoldem.Logic.Players;
using TexasHoldem.AI.SmartPlayer;
using TexasHoldem.AI.DummyPlayer;
using SimpleAudioSystem;

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
    [SerializeField] private DialogueBoxAnimator dialogueRunner;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Endgame")]
    [SerializeField] private PauseMenu pauseMenu;

    [Tooltip("Player money at or below this triggers the endgame screen (0 = only a literal bust).")]
    [SerializeField] private int minimumBuyIn = 0;

    private const float EngineStartupTimeoutSeconds = 5f;

    private int currentScore;
    private int bestScore;
    private bool gameOverTriggered;

    // Set from the engine thread the moment it actually begins running, read on
    // the main thread by WatchEngineStartup().
    private volatile bool _engineThreadStarted;

    // Seat order the table was dealt in, used to work out each player's
    // position relative to whoever holds the button in the current hand.
    private List<string> seatOrder;

    // Yarn-facing state the engine feeds in as the hand progresses.
    private string gamePhase = "idle";
    private int handsPlayed;
    private int currentPot;
    private int playerStreak;
    private int lastHandEndMoney;
    private string playerLastAction = "none";
    private bool someoneAllIn;

    // While the player is deciding, the table stays quiet: existing dialogue is
    // closed and no new line may open, so nothing pops up over their prompt.
    private bool playerTurnActive;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        currentScore = initialMoney;
        bestScore = initialMoney;
        lastHandEndMoney = initialMoney;

        if (dialogueManager == null)
        {
            dialogueManager = DialogueManager.Instance;
        }

        var humanPlayer = new HumanPlayer("You");
        var players = new List<IPlayer>{
            humanPlayer,
            new SlowedPlayer(MakeBot(bot1Controller, humanPlayer), npcActionDelaySeconds, "Bot 1"),
            new SlowedPlayer(MakeBot(bot2Controller, humanPlayer), npcActionDelaySeconds, "Bot 2"),
            new SlowedPlayer(MakeBot(bot3Controller, humanPlayer), npcActionDelaySeconds, "Bot 3")
        };
        seatOrder = players.Select(p => p.Name).ToList();

        var game = new TexasHoldemGame(players, initialMoney);

        StartEngine(game);
    }

    // The engine is a blocking, synchronous library: it parks a thread in
    // HumanPlayer.GetTurn() until the player clicks, and Thread.Sleep()s between
    // CPU turns. That only works if it gets a real thread of its own, which is
    // why this is a Task.Run rather than a coroutine.
    private void StartEngine(TexasHoldemGame game)
    {
        try
        {
            Task.Run(() =>
            {
                _engineThreadStarted = true;

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
        catch (System.Exception ex)
        {
            // Scheduling itself can throw where threads are unavailable.
            Debug.LogError($"[Poker] Could not schedule the engine thread: {ex.GetType().Name}: {ex.Message}");
        }

        StartCoroutine(WatchEngineStartup());
    }

    // Single-threaded players (notably Web/WebGL builds without threading
    // support) accept Task.Run and then never run the delegate, so the table
    // just sits there with no error. Turn that silence into a real message.
    private IEnumerator WatchEngineStartup()
    {
        yield return new WaitForSecondsRealtime(EngineStartupTimeoutSeconds);

        if (_engineThreadStarted) yield break;

        Debug.LogError(
            $"[Poker] The engine thread never started after {EngineStartupTimeoutSeconds}s. " +
            $"This build has no usable worker threads (Application.platform={Application.platform}), " +
            "so no hand will be dealt and no action can be submitted. " +
            "The engine has to be driven cooperatively on this platform.");
    }

    private static IPlayer MakeBot(CPU_Controller seat, HumanPlayer human)
    {
        return new CheatingPlayer(new SmartPlayer(), seat, human);
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

    // ---------------- Yarn state sync (DIALOGUE_MECHANICS.md section 2A) ----------------

    /// <summary>Seat index (1-3) of whichever seat that yarn id is sitting in, or 0.</summary>
    private int GetSeatForYarnId(string yarnId)
    {
        if (string.IsNullOrEmpty(yarnId)) return 0;

        for (int seat = 1; seat <= 3; seat++)
        {
            var controller = GetController(seat);
            if (controller != null && controller.YarnId == yarnId) return seat;
        }

        return 0;
    }

    private string[] SeatedYarnIds()
    {
        return new[] { bot1Controller, bot2Controller, bot3Controller }
            .Where(c => c != null && !string.IsNullOrEmpty(c.YarnId))
            .Select(c => c.YarnId)
            .ToArray();
    }

    // Section 5's pot table, measured against the starting stack as the
    // stand-in for average stack (per-player stacks are not exposed by the engine).
    private string PotLevel()
    {
        if (initialMoney <= 0) return "low";
        float ratio = currentPot / (float)initialMoney;

        if (ratio > 1f) return "massive";
        if (ratio >= 0.5f) return "high";
        if (ratio >= 0.2f) return "medium";
        return "low";
    }

    // Section 5's chip table, as a fraction of the starting stack.
    private string PlayerChipLevel()
    {
        if (initialMoney <= 0) return "comfortable";
        float ratio = currentScore / (float)initialMoney;

        if (ratio > 2f) return "dominant";
        if (ratio >= 1f) return "wealthy";
        if (ratio >= 0.4f) return "comfortable";
        if (ratio >= 0.1f) return "low";
        return "desperate";
    }

    private void Sync(string trigger, string actingNpc = "", string actingNpcAction = "none")
    {
        if (dialogueManager == null) return;

        dialogueManager.SyncGameState(
            gamePhase, PotLevel(), someoneAllIn,
            playerLastAction, PlayerChipLevel(), playerStreak,
            dialogueManager.Variables.GetBool("player_folded"),
            trigger, actingNpc, actingNpcAction,
            handsPlayed, handsPlayed);
    }

    // ---------------- Engine event hooks ----------------

    /// <summary>A new hand was dealt. Resets per-hand yarn state.</summary>
    public void OnHandStarted(int handNumber)
    {
        AudioManager.Instance?.PlayOneShot("card_deal");

        handsPlayed = handNumber;
        gamePhase = "pre_flop";
        playerLastAction = "none";
        someoneAllIn = false;
        currentPot = 0;

        if (dialogueManager != null)
        {
            dialogueManager.BeginHand(handNumber, SeatedYarnIds());
        }

        Sync("hand_start");
    }

    /// <summary>A betting round began. Drives $game_phase.</summary>
    public void OnPhaseChanged(string phase, int pot)
    {
        gamePhase = phase;
        currentPot = pot;

        if (phase == "flop" || phase == "turn" || phase == "river")
        {
            AudioManager.Instance?.PlayOneShot("card_flip");
        }

        Sync("phase_change");

        // Section 2F - not every phase change should produce a line.
        if (dialogueManager != null && dialogueManager.RollForDialogue())
        {
            PlayFromPool(new[] { "casual", "react_action" });
        }
    }

    /// <summary>The hand finished. Showdown, then the between-hands lull.</summary>
    public void OnHandEnded(int playerMoney)
    {
        if (playerMoney > lastHandEndMoney) playerStreak = Mathf.Max(1, playerStreak + 1);
        else if (playerMoney < lastHandEndMoney) playerStreak = Mathf.Min(-1, playerStreak - 1);
        lastHandEndMoney = playerMoney;

        gamePhase = "showdown";
        Sync("hand_end");
        PlayFromPool(new[] { "showdown", "player" });

        gamePhase = "between_hands";
        Sync("between_hands");

        // Section 2F - between_hands is the one beat worth firing unconditionally.
        PlayFromPool(new[] { "idle", "pair", "lore", "trio" });
    }

    /// <summary>The player's turn began - clear the floor and hold it clear.</summary>
    public void OnPlayerTurnStarted()
    {
        playerTurnActive = true;

        if (dialogueRunner != null)
        {
            dialogueRunner.CloseAll();
        }
    }

    /// <summary>The human acted. Section 2E: folding thins the table.</summary>
    public void OnPlayerAction(string action)
    {
        playerTurnActive = false;
        playerLastAction = action;
        if (action == "all_in") someoneAllIn = true;
        PlayActionSound(action);

        if (dialogueManager != null && action == "fold")
        {
            dialogueManager.OnPlayerFold();
        }

        Sync("player_action");

        if (dialogueManager != null && dialogueManager.RollForDialogue())
        {
            PlayFromPool(new[] { "react_action", "casual" }, action);
        }
    }

    // Called on the main thread (via ThreadManager) when a CPU seat starts its
    // turn. Section 2E ordering: the acting NPC gets their self-line while still
    // seated, and only if they stay quiet does the rest of the table react.
    public void OnCpuTurnStarted(int seatIndex, string action = "")
    {
        var controller = GetController(seatIndex);
        if (controller == null)
        {
            Debug.LogWarning($"[Dialogue] ABORT: no CPU_Controller assigned for seat {seatIndex}.", this);
            return;
        }

        PlayActionSound(action);

        if (action == "all_in") someoneAllIn = true;

        string yarnId = controller.YarnId;
        Sync("npc_action", yarnId, string.IsNullOrEmpty(action) ? "none" : action);

        // Only ask for a line if it could actually be shown - the floor has to be
        // free and it must not be the player's turn. Checked before CallDialogue
        // so a node is not spent on its cooldown for a line that never plays.
        bool canSpeak = dialogueRunner != null && !playerTurnActive && !dialogueRunner.IsDialogueOpen;
        bool spokeForSelf = false;

        if (canSpeak)
        {
            // The acting NPC's own line, chosen from their target: self nodes.
            string line = controller.CallDialogue(action);
            if (!string.IsNullOrEmpty(line))
            {
                spokeForSelf = dialogueRunner.OpenDialogue(seatIndex, controller.CharacterName, line);
            }
        }

        // Table bookkeeping runs whether or not anyone spoke - this is game
        // state, not presentation. Still after their own line, never before.
        if (action == "fold" && dialogueManager != null && !string.IsNullOrEmpty(yarnId))
        {
            dialogueManager.OnNpcFold(yarnId);
        }

        // Only let the table answer if the actor stayed quiet, so one event never
        // produces two lines back to back.
        if (!spokeForSelf && dialogueManager != null && dialogueManager.RollForDialogue())
        {
            PlayFromPool(new[] { "react_action", "casual" }, action);
        }
    }

    /// <summary>
    /// Pulls a line from the shared pool and shows it above whichever seat its
    /// speaker occupies. Silently does nothing when nothing matches.
    /// </summary>
    private void PlayFromPool(string[] categories, string actionFilter = "")
    {
        if (dialogueManager == null || dialogueRunner == null) return;

        // Stay quiet during the player's turn, and never talk over whoever
        // already has the floor - checked before picking so the chosen node
        // is not burned on its cooldown for a line that never plays.
        if (playerTurnActive || dialogueRunner.IsDialogueOpen) return;

        var selection = dialogueManager.RequestDialogue(categories, actionFilter);
        if (selection == null) return;

        int seat = GetSeatForYarnId(selection.SpeakerId);
        if (seat == 0)
        {
            Debug.LogWarning(
                $"[Dialogue] '{selection.SpeakerId}' has no seat; check the CPU_Animator YarnIds.", this);
            return;
        }

        GetController(seat)?.Speak(selection.SpeakerDisplayName, selection.Text);
        dialogueRunner.OpenDialogue(seat, selection.SpeakerDisplayName, selection.Text);
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

    private static void PlayActionSound(string action)
    {
        string audioId = action switch
        {
            "all_in" => "move_all_in",
            "check" => "move_check",
            "fold" => "move_fold",
            "call" => "move_call",
            "raise" => "move_raise",
            _ => null,
        };

        if (audioId != null)
        {
            AudioManager.Instance?.PlayOneShot(audioId);
        }
    }

    // Called from the main thread whenever the human player's money changes.
    public void UpdateScore(int money)
    {
        currentScore = money;
        if (currentScore > bestScore)
        {
            bestScore = currentScore;
        }

        // Game over: the player can no longer meaningfully continue. Guarded so
        // this fires once - UpdateScore runs every round for the rest of the game.
        if (!gameOverTriggered && currentScore <= minimumBuyIn)
        {
            gameOverTriggered = true;

            if (pauseMenu != null)
            {
                pauseMenu.OpenEndgame();
            }
            else
            {
                Debug.LogWarning("[PokerGameManager] Player is out of money but no PauseMenu is assigned.", this);
            }
        }
    }

    public int GetCurrentScore() => currentScore;

    public int GetBestScore() => bestScore;
}
