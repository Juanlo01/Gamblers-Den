using System.Collections.Generic;

/// <summary>
/// One dialogue node parsed out of a .yarn file, holding the header block that
/// DIALOGUE_MECHANICS.md defines as the query filter plus the spoken lines.
/// </summary>
public class DialogueNode
{
    public string NodeName;
    public string Speaker;      // snake_case npc id, e.g. general_niu
    public string Category;     // idle, casual, react_action, showdown, player, pair, lore, trio
    public string Target;       // any, player, self, or a specific npc id
    public string Phase;        // any, pre_flop, flop, turn, river, showdown, between_hands
    public string Requires;     // boolean expression, "and"-combinable; empty = no requirement
    public string ReactTo;      // react_action only: raise, fold, all_in, call, check, any
    public int Priority = 1;    // 1=generic, 2=phase-specific, 3=pair, 4=trio

    /// <summary>Spoken lines in order. Today every node holds exactly one.</summary>
    public List<DialogueLine> Lines = new List<DialogueLine>();

    public override string ToString()
    {
        return $"{NodeName} (speaker={Speaker}, category={Category}, phase={Phase}, priority={Priority})";
    }
}

/// <summary>A single spoken line: the display name and what they say.</summary>
public class DialogueLine
{
    public DialogueLine(string speakerDisplayName, string text)
    {
        SpeakerDisplayName = speakerDisplayName;
        Text = text;
    }

    /// <summary>Human-facing name as written in the .yarn file, e.g. "General Niu".</summary>
    public string SpeakerDisplayName { get; }

    public string Text { get; }
}

/// <summary>What a dialogue request resolved to, ready to hand to the UI.</summary>
public class DialogueSelection
{
    public DialogueSelection(DialogueNode node, DialogueLine line)
    {
        Node = node;
        Line = line;
    }

    public DialogueNode Node { get; }

    public DialogueLine Line { get; }

    /// <summary>snake_case id of whoever speaks, used to find their seat.</summary>
    public string SpeakerId => Node.Speaker;

    public string SpeakerDisplayName => Line.SpeakerDisplayName;

    public string Text => Line.Text;
}
