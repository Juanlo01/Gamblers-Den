# Dialogue System — Unity Integration Guide

Yarn Spinner dialogue wired to NikolayIT's Texas Hold'em engine. 15 NPCs total, 3 seated per session + player + dealer. Dialogue is not scripted per-moment — the engine filters a pool of `.yarn` nodes down to the ones valid for the current game state, then randomly (weighted) picks one and plays it. `.yarn` files hold no logic; all control lives in the header metadata below and in the C# runner.

## 1. Schema

### Node headers

Every dialogue node needs this header block. Read via `YarnProject.GetHeaders(nodeName)`.

```
speaker:    beauty_xu          // who speaks (snake_case, must be one of the 15 NPC names)
category:   casual             // idle, casual, react_action, showdown, player, pair, lore
                                //   idle    = phase: between_hands only. No game reference — quiet,
                                //             reflective, character-voice lines for the lull.
                                //   casual  = phase: any, fires during active hand phases
                                //             (pre_flop..river). Light banter, can reference the
                                //             game generally, but is NOT a reaction to a specific action.
target:     any                // any, player, self, or a specific character name
                                //   self = (react_action only) this line is the ACTING npc reacting
                                //          to their own action — e.g. their own fold. Requested via
                                //          RequestSelfReaction, never mixed into the general react pool.
phase:      any                // any, pre_flop, flop, turn, river, showdown, between_hands
requires:   $poet               // boolean expression, "and"-combinable. empty = no requirement
react_to:   any                 // react_action nodes ONLY: raise, fold, all_in, call, check, any
priority:   1                   // 1=generic, 2=phase-specific, 3=pair, 4=trio
```

### Yarn variables (declared in `init.yarn`)

Yarn never writes these — the engine sets them before every dialogue request, yarn only reads them to gate lines via `requires:` or `<<if>>`.

| Variable | Type | Values |
|---|---|---|
| `$beauty_xu` … `$yusen_zhou` (15 total) | bool | true if that NPC is one of the 3 seated |
| `$game_phase` | string | `idle`, `pre_flop`, `flop`, `turn`, `river`, `showdown`, `between_hands` |
| `$round_number` | int | current hand number |
| `$hands_played` | int | total hands this session |
| `$pot_level` | string | `low`, `medium`, `high`, `massive` — **declared, currently unused by any dialogue.** Not a header field; if you use it, branch on it inline (`<<if $pot_level == "massive">>`) inside an existing node. No obligation to write per-state content. |
| `$someone_all_in` | bool | |
| `$players_remaining` | int | `2`–`4`. Counts NPC seats still in the hand + player. Starts at 4 each hand, decrements when an NPC or the player folds. Drives "just us three/two" lines — see [Node Skeletons](#6-node-skeletons-for-new-dialogue) below. |
| `$player_last_action` | string | `none`, `check`, `call`, `raise`, `fold`, `all_in` |
| `$player_chips` | string | `desperate`, `low`, `comfortable`, `wealthy`, `dominant` — also unused today. If you build this out, consider collapsing to 3 (`low`/`comfortable`/`dominant`) to cut the writing surface across 15 NPCs. |
| `$player_streak` | int | positive = win streak, negative = loss streak |
| `$player_folded` | bool | |
| `$trigger` | string | why this dialogue request happened, e.g. `idle`, `phase_change`, `npc_action` |
| `$acting_npc` | string | which NPC just acted (empty if none) |
| `$acting_npc_action` | string | what they did |

## 2. What you have to build (dev side)

Yarn files are static content. Everything dynamic — who can speak, when, how often — is your job in C#. Three things:

**A. Push state into yarn variables before every dialogue request.**

```csharp
variableStorage.SetValue("$game_phase", "flop");
variableStorage.SetValue("$pot_level", "medium");
variableStorage.SetValue("$player_last_action", "raise");
// ...and so on for every variable in the table above
```

Full example — call this from your poker engine's phase-change / action callbacks:

```csharp
public void SyncGameState(
    string[] npcsAtTable, string gamePhase, string potLevel, bool someoneAllIn,
    string playerLastAction, string playerChips, int playerStreak, bool playerFolded,
    string trigger, string actingNpc, string actingNpcAction)
{
    string[] allNPCs = { "beauty_xu", "eunuch_cai", "general_niu", "general_tian",
        "ghost_bride", "guard", "lord_xie", "madam_song", "mr_li",
        "mr_zhu", "poet", "rogue", "shaman", "wanderer", "yusen_zhou" };

    foreach (var npc in allNPCs) variableStorage.SetValue("$" + npc, false);
    foreach (var npc in npcsAtTable) variableStorage.SetValue("$" + npc, true);

    variableStorage.SetValue("$game_phase", gamePhase);
    variableStorage.SetValue("$pot_level", potLevel);
    variableStorage.SetValue("$someone_all_in", someoneAllIn);
    variableStorage.SetValue("$player_last_action", playerLastAction);
    variableStorage.SetValue("$player_chips", playerChips);
    variableStorage.SetValue("$player_streak", playerStreak);
    variableStorage.SetValue("$player_folded", playerFolded);
    variableStorage.SetValue("$trigger", trigger);
    variableStorage.SetValue("$acting_npc", actingNpc);
    variableStorage.SetValue("$acting_npc_action", actingNpcAction);
}
```

**B. Filter + pick a node, then play it.** This is the whole reason the header schema exists — it's a query filter, not documentation.

```csharp
public bool RequestDialogue(string[] categories, string actionFilter = "")
{
    var candidates = FilterNodes(categories, actionFilter);
    if (candidates.Count == 0) return false;

    var selected = WeightedRandom(candidates);   // see section 4
    RecordUsage(selected.nodeName);               // cooldown, see below
    dialogueRunner.StartDialogue(selected.nodeName);
    return true;
}

List<DialogueNode> FilterNodes(string[] categories, string actionFilter)
{
    variableStorage.TryGetValue("$game_phase", out string currentPhase);
    var categorySet = new HashSet<string>(categories);

    return nodeIndex.Where(n =>
        seatedNPCs.Contains(n.speaker) &&
        categorySet.Contains(n.category) &&
        (n.phase == "any" || n.phase == currentPhase) &&
        (string.IsNullOrEmpty(actionFilter) || string.IsNullOrEmpty(n.reactTo) ||
         n.reactTo == "any" || n.reactTo == actionFilter) &&
        (n.target == "any" || n.target == "player" || seatedNPCs.Contains(n.target)) &&
        EvaluateRequires(n.requires) &&
        !recentlyUsed.Contains(n.nodeName)
    ).ToList();
}
```

Call `RequestDialogue` from your own game-event hooks, e.g.:

```csharp
public void OnFlop()
{
    UpdatePhase("flop");
    dialogueManager.RequestDialogue(new[] { "casual", "react_action" });
}

public void OnNPCAction(string npcName, string action)
{
    dialogueManager.RequestDialogue(new[] { "react_action", "casual" }, action);
    if (action == "fold") dialogueManager.OnNPCFold(npcName);
}
```

**C. Build the node index once at startup** by reading every node's headers out of the `YarnProject`:

```csharp
void BuildNodeIndex()
{
    foreach (string nodeName in yarnProject.NodeNames)
    {
        var headers = yarnProject.GetHeaders(nodeName);
        if (!headers.ContainsKey("speaker")) continue; // skip Yarn's own built-in nodes

        nodeIndex.Add(new DialogueNode {
            nodeName = nodeName,
            speaker = GetHeader(headers, "speaker"),
            category = GetHeader(headers, "category"),
            target = GetHeader(headers, "target"),
            phase = GetHeader(headers, "phase"),
            requires = GetHeader(headers, "requires"),
            reactTo = GetHeader(headers, "react_to"),
            priority = int.TryParse(GetHeader(headers, "priority"), out int p) ? p : 1
        });
    }
}
```

**D. Cooldown, so the same line doesn't repeat back-to-back.** Track the last N node names played and exclude them from candidates (`COOLDOWN_SIZE = 20` is a reasonable default, tune per NPC line count):

```csharp
void RecordUsage(string nodeName)
{
    recentlyUsed.Add(nodeName);
    cooldownQueue.Enqueue(nodeName);
    while (cooldownQueue.Count > COOLDOWN_SIZE)
        recentlyUsed.Remove(cooldownQueue.Dequeue());
}
```

**E. Fold handling.** When an NPC folds, they stop being a valid speaker/target for the rest of the hand — flip their bool, drop them from `seatedNPCs`, **and decrement `$players_remaining` before requesting the reaction line**, or the line will branch on the stale count:

```csharp
public void OnNPCFold(string npcName)
{
    variableStorage.SetValue("$" + npcName, false);
    seatedNPCs = seatedNPCs.Where(n => n != npcName).ToArray();

    variableStorage.TryGetValue("$players_remaining", out int remaining);
    variableStorage.SetValue("$players_remaining", remaining - 1);
}
```

Same idea for the player folding out of a hand (a different code path, `OnPlayerAction`) — decrement `$players_remaining` there too before calling `RequestDialogue`. Reset back to `4` at the start of every new hand.

**Ordering matters, and it's easy to get backwards.** `FilterNodes` requires `seatedNPCs.Contains(n.speaker)` — so if you call `OnNPCFold` (which removes the NPC from `seatedNPCs`) *before* requesting dialogue, that NPC can never be picked as a speaker for their own fold reaction. Every `react_to: fold` line becomes something only the *other* NPCs can say — there's no way for the folding NPC to react to their own fold. If you want that (a "well, that's that" line from the person who just folded), request their self-reaction line **first**, while they're still seated, then remove them from the table, then let everyone else react:

```csharp
// A self-reaction line is speaker-locked and target: self — never pulled into the general pool.
public bool RequestSelfReaction(string npcName, string action)
{
    var candidates = nodeIndex.Where(n =>
        n.speaker == npcName &&
        n.category == "react_action" &&
        n.target == "self" &&
        (n.reactTo == "any" || n.reactTo == action) &&
        EvaluateRequires(n.requires) &&
        !recentlyUsed.Contains(n.nodeName)
    ).ToList();

    if (candidates.Count == 0) return false;
    var selected = WeightedRandom(candidates);
    RecordUsage(selected.nodeName);
    dialogueRunner.StartDialogue(selected.nodeName);
    return true;
}

public void OnNPCAction(string npcName, string action)
{
    if (action == "fold")
    {
        // still seated at this point — eligible to speak about their own fold
        bool spokeForSelf = dialogueManager.RequestSelfReaction(npcName, "fold");
        dialogueManager.OnNPCFold(npcName); // now removed, $players_remaining decremented

        // only let the table react if the folder didn't already get a line —
        // avoids two fold lines firing back to back for one event
        if (!spokeForSelf)
            dialogueManager.RequestDialogue(new[] { "react_action", "casual" }, action);
    }
    else
    {
        dialogueManager.RequestDialogue(new[] { "react_action", "casual" }, action);
    }
}
```

This same self-vs-others split applies to any action, not just fold — an NPC going all-in might get their own line too — but fold is the one where the ordering bug actually bites, since it's the only action that removes the speaker from the table.

**F. Gate how often dialogue fires at all.** Not every phase change/action should trigger a line — roll a 30–50% chance before calling `RequestDialogue`, or dialogue will be constant and grating. `between_hands` is the one moment worth firing dialogue unconditionally, since it's a natural pause (good spot for `pair`/`lore`).

## 3. `category: react_action` ↔ `react_to`

These two headers work together and only mean something in combination:

- `category: react_action` marks a node as *reactive* — it's a response to something that just happened, not ambient chatter. It's what makes the node eligible when you call `RequestDialogue(new[] { "react_action" }, actionFilter)`.
- `react_to` narrows *which* action it reacts to: `raise`, `fold`, `all_in`, `call`, `check`, or `any`. It is **only read on `react_action` nodes** — setting it on an `idle`/`casual`/etc. node does nothing.

The filter logic (`FilterNodes` above) matches a node when `react_to == "any"` or `react_to == actionFilter`. So:

```
category: react_action
react_to: raise
```
→ only fires when you call `RequestDialogue(new[] {"react_action"}, "raise")`, i.e. right after someone raises.

```
category: react_action
react_to: any
```
→ fires for react_action requests regardless of which action triggered it (a generic "ooh" reaction node).

Concretely: `OnNPCAction(npc, "raise")` calls `RequestDialogue(new[] {"react_action","casual"}, "raise")`. That pulls in every `react_action` node with `react_to: raise` or `react_to: any`, plus every `casual` node (which ignores `actionFilter` entirely since `react_to` is empty on non-react nodes). If you want a category that is *purely* reactive with no casual filler mixed in, just pass `new[] {"react_action"}` alone.

**All of this is *other people* reacting.** `RequestDialogue`'s candidate pool is drawn from `seatedNPCs` at large — it has no concept of "the NPC who just acted" vs. everyone else, so a `react_action`/`react_to: fold` node is by default a line any other seated NPC says about someone folding, never a line the folder says about their own fold. If you want that specific "I'm reacting to my own action" line, mark the node `target: self` and pull it separately via `RequestSelfReaction` (see section 2E) — it's a distinct request path, not a filter on the shared pool.

## 4. Weight / priority randomness

`priority` is not a guarantee, it's a bias. Given the candidate pool that survives filtering, `WeightedRandom` assigns each candidate a weight equal to its priority value and rolls proportionally:

```csharp
DialogueNode WeightedRandom(List<DialogueNode> candidates)
{
    float[] weights = { 0, 1f, 2f, 3f, 4f }; // index = priority (1..4)
    float totalWeight = candidates.Sum(c => weights[Mathf.Clamp(c.priority, 1, 4)]);
    float roll = Random.Range(0f, totalWeight);
    float cumulative = 0f;
    foreach (var c in candidates)
    {
        cumulative += weights[Mathf.Clamp(c.priority, 1, 4)];
        if (roll <= cumulative) return c;
    }
    return candidates[candidates.Count - 1];
}
```

**Your instinct is right, with one caveat:** it's exactly a 40/30/20/10 split for priorities 4/3/2/1 — *but only when all four priorities are present in the filtered candidate list.* Weights are relative to whatever survived `FilterNodes`, not fixed percentages, since the pool changes every call (phase, seated NPCs, `requires` all shrink it first).

| Priorities present in pool | Weight sum | Odds per candidate |
|---|---|---|
| 4, 3, 2, 1 (one each) | 10 | 4→40%, 3→30%, 2→20%, 1→10% |
| 4, 3, 2, 1, but two priority-4 nodes | 11 | each P4 → ~18.2%, they jointly dominate at ~36% |
| Only 2 and 1 present (no trio/pair match) | 3 | 2→67%, 1→33% |
| Only priority 1 present | 1 | 1→100% |

Nothing is ever excluded once it passes the filter — a priority-1 (generic) line can always fire, it's just outweighed whenever a more specific (phase/pair/trio) match exists for that moment. Priority only controls *likelihood among nodes that already passed every other filter* (speaker seated, category, phase, `requires`, `react_to`, not on cooldown) — it never overrides those hard filters.

## 5. Pot / chip level mapping

| Pot Level | When |
|-----------|------|
| `low` | Pot < 20% of average stack |
| `medium` | Pot 20–50% of average stack |
| `high` | Pot 50–100% of average stack |
| `massive` | Pot > average stack |

| Player Chips | When |
|-------------|------|
| `desperate` | < 10% of starting stack |
| `low` | 10–40% of starting stack |
| `comfortable` | 40–100% of starting stack |
| `wealthy` | 100–200% of starting stack |
| `dominant` | > 200% of starting stack |

## 6. Node skeletons for new dialogue

Copy the block below per NPC, per new dialogue instance you're adding. Fill in the `{...}` placeholders once at the top of the block, then write into the `//FIXME` lines — delete any numbered node you don't end up using. Aim for 4–6 variants per instance so cooldown (section 2D) doesn't repeat the same line every few folds.

The two-line file header (brief description, then cause of death) already exists at the top of each NPC's `.yarn` file — these blocks get appended into that same file, they don't need their own header.

```
// ============================================================
// REACT_ACTION — {instance name} ({N} nodes)
// ============================================================

title: {Npc}_ReactAction_{Instance}_01
tags: react_action
speaker: {npc_snake_case}
category: react_action
target: any
phase: any
requires:
priority: 2
react_to: fold
---
//START: FIXME_{instance}_01
<<if $players_remaining == 3>>
    {Speaker Name}: [auto/] //FIXME: line for "down to three" (one NPC just folded)
<<elseif $players_remaining == 2>>
    {Speaker Name}: [auto/] //FIXME: line for "down to two" (heads-up vs. player)
<<endif>>
//END: FIXME_{instance}_01
===

title: {Npc}_ReactAction_{Instance}_02
tags: react_action
speaker: {npc_snake_case}
category: react_action
target: any
phase: any
requires:
priority: 2
react_to: fold
---
//START: FIXME_{instance}_02
<<if $players_remaining == 3>>
    {Speaker Name}: [auto/] //FIXME
<<elseif $players_remaining == 2>>
    {Speaker Name}: [auto/] //FIXME
<<endif>>
//END: FIXME_{instance}_02
===

// ... repeat _03 through _05 or _06, same shape, delete what you don't fill in
```

`priority: 2` here is a suggestion, not a rule — bumped one above the generic react_action lines (which sit at `priority: 1` in the existing files) so a "table's thinning out" moment stands a better chance of winning the weighted roll than an ordinary raise/fold reaction, since it's a rarer, more narratively notable beat. Drop it to `1` if you'd rather it blend in.

### Filled example — `beauty_xu.yarn`, players-remaining reaction

```
// ============================================================
// REACT_ACTION — table thins out (4 nodes)
// ============================================================

title: BeautyXu_ReactAction_PlayersRemaining_01
tags: react_action
speaker: beauty_xu
category: react_action
target: any
phase: any
requires:
priority: 2
react_to: fold
---
//START: FIXME_players_remaining_01
<<if $players_remaining == 3>>
    Beauty Xu: [auto/] Just the three of us now. Fewer voices, easier to hear the truth in them.
<<elseif $players_remaining == 2>>
    Beauty Xu: [auto/] Down to two. No more hiding behind a crowd.
<<endif>>
//END: FIXME_players_remaining_01
===

title: BeautyXu_ReactAction_PlayersRemaining_02
tags: react_action
speaker: beauty_xu
category: react_action
target: any
phase: any
requires:
priority: 2
react_to: fold
---
//START: FIXME_players_remaining_02
<<if $players_remaining == 3>>
    Beauty Xu: [auto/] //FIXME
<<elseif $players_remaining == 2>>
    Beauty Xu: [auto/] //FIXME
<<endif>>
//END: FIXME_players_remaining_02
===
```

### Filled example — self-fold reaction (the folding NPC's own line)

`target: self` is what routes this to `RequestSelfReaction` instead of the shared react_action pool — without it, this node would just sit in the general pool where, per the ordering issue above, it could never actually be picked for its own speaker's fold.

```
// ============================================================
// REACT_ACTION — self-fold (4 nodes)
// ============================================================

title: BeautyXu_ReactAction_SelfFold_01
tags: react_action
speaker: beauty_xu
category: react_action
target: self
phase: any
requires:
priority: 1
react_to: fold
---
//START: FIXME_self_fold_01
Beauty Xu: [auto/] I fold. Some songs aren't worth finishing.
//END: FIXME_self_fold_01
===

title: BeautyXu_ReactAction_SelfFold_02
tags: react_action
speaker: beauty_xu
category: react_action
target: self
phase: any
requires:
priority: 1
react_to: fold
---
//START: FIXME_self_fold_02
Beauty Xu: [auto/] //FIXME
//END: FIXME_self_fold_02
===

// ... repeat _03, _04
```

### Filled example — bluff-succeeds variant (no new variable needed)

This one doesn't touch `$players_remaining` at all — it's just new content on the existing player-fold reaction pool (`category: react_action`, `target: player`, `react_to: fold`), fired whenever `OnPlayerAction("fold")` runs after an NPC raised into the player:

```
title: BeautyXu_ReactAction_PlayerFold_Bluff_01
tags: react_action
speaker: beauty_xu
category: react_action
target: player
phase: any
requires:
priority: 1
react_to: fold
---
//START: FIXME_bluff_succeeds_01
Beauty Xu: [auto/] //FIXME: line for "and the bluff succeeds" — the player folded to a raise
//END: FIXME_bluff_succeeds_01
===
```
