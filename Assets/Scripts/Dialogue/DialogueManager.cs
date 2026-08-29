using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Owns the dialogue pool described in Assets/YarnScripts/DIALOGUE_MECHANICS.md:
/// builds a node index from the .yarn files, keeps the yarn variables in sync
/// with the poker engine, then filters and weight-picks a line on request.
///
/// Yarn files hold no logic - all control lives here and in the node headers.
/// Everything on this class is main-thread only; engine-thread callers must
/// marshal through ThreadManager first.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    /// <summary>The 15 NPC ids, matching the presence flags declared in init.yarn.</summary>
    public static readonly string[] AllNpcIds =
    {
        "beauty_xu", "eunuch_cai", "general_niu", "general_tian",
        "ghost_bride", "guard", "lord_xie", "madam_song", "mr_li",
        "mr_zhu", "poet", "rogue", "shaman", "wanderer", "yusen_zhou",
    };

    [Header("Yarn Scripts")]
    [Tooltip("init.yarn - its <<declare>> statements seed the variable store.")]
    [SerializeField] private TextAsset initScript;

    [Tooltip("Every NPC .yarn file to pull dialogue nodes from.")]
    [SerializeField] private TextAsset[] npcScripts;

    [Header("Tuning")]
    [Tooltip("How many recently played nodes are blocked from repeating (section 2D).")]
    [SerializeField] private int cooldownSize = 20;

    [Tooltip("Chance a phase change or action actually produces a line (section 2F).")]
    [Range(0f, 1f)]
    [SerializeField] private float dialogueChance = 0.4f;

    [SerializeField] private bool verboseLogging = true;

    private readonly List<DialogueNode> _nodeIndex = new List<DialogueNode>();
    private readonly HashSet<string> _recentlyUsed = new HashSet<string>();
    private readonly Queue<string> _cooldownQueue = new Queue<string>();

    private string[] _seatedNpcs = new string[0];

    public DialogueVariables Variables { get; } = new DialogueVariables();

    public IReadOnlyList<DialogueNode> NodeIndex => _nodeIndex;

    public IReadOnlyList<string> SeatedNpcs => _seatedNpcs;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        LoadDeclarations();
        BuildNodeIndex();
    }

    // ---------------- Startup ----------------

    private void LoadDeclarations()
    {
        if (initScript == null)
        {
            Debug.LogWarning("[Dialogue] No init.yarn assigned; variables start empty.", this);
            return;
        }

        var declarations = YarnScriptParser.ParseDeclarations(initScript.text);
        Variables.LoadDeclarations(declarations);

        if (verboseLogging)
        {
            Debug.Log($"[Dialogue] Loaded {declarations.Count} variable declarations from {initScript.name}.", this);
        }
    }

    /// <summary>Section 2C - build the node index once at startup.</summary>
    private void BuildNodeIndex()
    {
        _nodeIndex.Clear();

        if (npcScripts == null)
        {
            return;
        }

        foreach (var script in npcScripts)
        {
            if (script == null)
            {
                continue;
            }

            var nodes = YarnScriptParser.ParseNodes(script.text, script.name);
            _nodeIndex.AddRange(nodes);

            if (verboseLogging)
            {
                Debug.Log($"[Dialogue] {script.name}: {nodes.Count} nodes.", this);
            }
        }

        if (_nodeIndex.Count == 0)
        {
            Debug.LogWarning("[Dialogue] Node index is empty - assign the NPC .yarn files.", this);
        }
        else if (verboseLogging)
        {
            var bySpeaker = _nodeIndex.GroupBy(n => n.Speaker)
                .Select(g => $"{g.Key}={g.Count()}");
            Debug.Log($"[Dialogue] Node index built: {_nodeIndex.Count} nodes ({string.Join(", ", bySpeaker)}).", this);
        }
    }

    // ---------------- Section 2A: push state into yarn variables ----------------

    /// <summary>Sets which NPCs are seated, flipping their presence flags.</summary>
    public void SetSeatedNpcs(params string[] npcIds)
    {
        _seatedNpcs = npcIds.Where(id => !string.IsNullOrEmpty(id)).ToArray();

        foreach (string npc in AllNpcIds)
        {
            Variables.SetValue(npc, false);
        }

        foreach (string npc in _seatedNpcs)
        {
            if (!AllNpcIds.Contains(npc))
            {
                Debug.LogWarning(
                    $"[Dialogue] Seated npc '{npc}' is not one of the 15 known ids; "
                    + "its presence flag will not exist in init.yarn.", this);
            }

            Variables.SetValue(npc, true);
        }

        if (verboseLogging)
        {
            Debug.Log($"[Dialogue] Seated: {string.Join(", ", _seatedNpcs)}", this);
        }
    }

    /// <summary>Section 2A - the per-request state push.</summary>
    public void SyncGameState(
        string gamePhase, string potLevel, bool someoneAllIn,
        string playerLastAction, string playerChips, int playerStreak, bool playerFolded,
        string trigger, string actingNpc, string actingNpcAction,
        int roundNumber, int handsPlayed)
    {
        Variables.SetValue("game_phase", gamePhase);
        Variables.SetValue("pot_level", potLevel);
        Variables.SetValue("someone_all_in", someoneAllIn);
        Variables.SetValue("player_last_action", playerLastAction);
        Variables.SetValue("player_chips", playerChips);
        Variables.SetValue("player_streak", playerStreak);
        Variables.SetValue("player_folded", playerFolded);
        Variables.SetValue("trigger", trigger);
        Variables.SetValue("acting_npc", actingNpc);
        Variables.SetValue("acting_npc_action", actingNpcAction);
        Variables.SetValue("round_number", roundNumber);
        Variables.SetValue("hands_played", handsPlayed);
    }

    public void SetPhase(string phase) => Variables.SetValue("game_phase", phase);

    public void SetPlayersRemaining(int count) => Variables.SetValue("players_remaining", count);

    public int PlayersRemaining => Variables.GetInt("players_remaining");

    /// <summary>
    /// Section 2E - an NPC folding stops them being a valid speaker or target for
    /// the rest of the hand, and drops the remaining count the lines branch on.
    /// Call RequestSelfReaction BEFORE this if the folder should speak first.
    /// </summary>
    public void OnNpcFold(string npcId)
    {
        Variables.SetValue(npcId, false);
        _seatedNpcs = _seatedNpcs.Where(n => n != npcId).ToArray();
        SetPlayersRemaining(Mathf.Max(0, PlayersRemaining - 1));

        if (verboseLogging)
        {
            Debug.Log($"[Dialogue] {npcId} folded; players_remaining={PlayersRemaining}", this);
        }
    }

    /// <summary>The player folding also thins the table (section 2E).</summary>
    public void OnPlayerFold()
    {
        Variables.SetValue("player_folded", true);
        SetPlayersRemaining(Mathf.Max(0, PlayersRemaining - 1));
    }

    /// <summary>Resets per-hand state. Call at the start of every hand.</summary>
    public void BeginHand(int handNumber, string[] seatedNpcs)
    {
        SetSeatedNpcs(seatedNpcs);
        SetPlayersRemaining(seatedNpcs.Length + 1); // seated NPCs + the player
        Variables.SetValue("player_folded", false);
        Variables.SetValue("someone_all_in", false);
        Variables.SetValue("player_last_action", "none");
        Variables.SetValue("round_number", handNumber);
    }

    // ---------------- Section 2F: should we speak at all ----------------

    /// <summary>Rolls the gate that stops dialogue firing on every single event.</summary>
    public bool RollForDialogue() => Random.value < dialogueChance;

    // ---------------- Section 2B: filter, pick, play ----------------

    /// <summary>
    /// Picks a line from the shared pool - any seated NPC may speak. Returns null
    /// when nothing survives the filter.
    /// </summary>
    public DialogueSelection RequestDialogue(string[] categories, string actionFilter = "")
    {
        var candidates = FilterNodes(categories, actionFilter);
        if (candidates.Count == 0)
        {
            if (verboseLogging)
            {
                Debug.Log(
                    $"[Dialogue] No candidates for [{string.Join(",", categories)}] "
                    + $"action='{actionFilter}' phase='{Variables.GetString("game_phase")}'.", this);
            }

            return null;
        }

        return Select(candidates);
    }

    /// <summary>
    /// Section 2E - a line the acting NPC says about their OWN action. Speaker
    /// locked and target: self, so it is never pulled into the shared pool.
    /// </summary>
    public DialogueSelection RequestSelfReaction(string npcId, string action)
    {
        var candidates = _nodeIndex.Where(n =>
            n.Speaker == npcId &&
            n.Category == "react_action" &&
            n.Target == "self" &&
            (string.IsNullOrEmpty(n.ReactTo) || n.ReactTo == "any" || n.ReactTo == action) &&
            Variables.Evaluate(n.Requires) &&
            !_recentlyUsed.Contains(n.NodeName)
        ).ToList();

        if (candidates.Count == 0)
        {
            return null;
        }

        return Select(candidates);
    }

    private DialogueSelection Select(List<DialogueNode> candidates)
    {
        var node = WeightedRandom(candidates);
        RecordUsage(node.NodeName);

        if (verboseLogging)
        {
            Debug.Log($"[Dialogue] Picked {node} from {candidates.Count} candidates.", this);
        }

        return new DialogueSelection(node, node.Lines[0]);
    }

    private List<DialogueNode> FilterNodes(string[] categories, string actionFilter)
    {
        string currentPhase = Variables.GetString("game_phase");
        var categorySet = new HashSet<string>(categories);
        var seated = new HashSet<string>(_seatedNpcs);

        return _nodeIndex.Where(n =>
            seated.Contains(n.Speaker) &&
            categorySet.Contains(n.Category) &&
            (n.Phase == "any" || n.Phase == currentPhase) &&
            (string.IsNullOrEmpty(actionFilter) || string.IsNullOrEmpty(n.ReactTo) ||
             n.ReactTo == "any" || n.ReactTo == actionFilter) &&
            // "self" lines are reserved for RequestSelfReaction, never the shared pool.
            n.Target != "self" &&
            (n.Target == "any" || n.Target == "player" || seated.Contains(n.Target)) &&
            Variables.Evaluate(n.Requires) &&
            !_recentlyUsed.Contains(n.NodeName)
        ).ToList();
    }

    /// <summary>Section 4 - priority biases the roll, it never hard-excludes.</summary>
    private DialogueNode WeightedRandom(List<DialogueNode> candidates)
    {
        float[] weights = { 0f, 1f, 2f, 3f, 4f }; // index = priority (1..4)
        float total = candidates.Sum(c => weights[Mathf.Clamp(c.Priority, 1, 4)]);
        float roll = Random.Range(0f, total);
        float cumulative = 0f;

        foreach (var c in candidates)
        {
            cumulative += weights[Mathf.Clamp(c.Priority, 1, 4)];
            if (roll <= cumulative)
            {
                return c;
            }
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>Section 2D - keep the last N nodes out of the candidate pool.</summary>
    private void RecordUsage(string nodeName)
    {
        _recentlyUsed.Add(nodeName);
        _cooldownQueue.Enqueue(nodeName);

        while (_cooldownQueue.Count > cooldownSize)
        {
            _recentlyUsed.Remove(_cooldownQueue.Dequeue());
        }
    }
}
