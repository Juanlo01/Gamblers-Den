using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Reads .yarn files as plain text, without the Yarn Spinner runtime.
///
/// This is viable because the project's scripts use no Yarn scripting features:
/// init.yarn is nothing but &lt;&lt;declare&gt;&gt; statements, and every NPC node body is a
/// single "Speaker Name: [auto/] line" with //START and //END comment markers.
/// If real Yarn control flow is ever added to a node body, this parser will need
/// to grow with it (or be swapped for the real runtime).
///
/// File shape it expects:
///   title: NodeName
///   speaker: general_niu
///   ...more headers...
///   ---
///   //START: id
///   General Niu: [auto/] The spoken line.
///   //END: id
///   ===
/// </summary>
public static class YarnScriptParser
{
    private static readonly Regex DeclareRegex = new Regex(
        @"<<declare\s+\$(?<name>\w+)\s*=\s*(?<value>""[^""]*""|[^>]+?)\s*>>",
        RegexOptions.Compiled);

    private static readonly Regex LineRegex = new Regex(
        @"^(?<speaker>[^:]+):\s*(?:\[auto/\])?\s*(?<text>.*)$",
        RegexOptions.Compiled);

    /// <summary>
    /// Parses the &lt;&lt;declare&gt;&gt; statements in init.yarn into their names, default
    /// values and types, so the variable store starts out matching the script.
    /// </summary>
    public static Dictionary<string, object> ParseDeclarations(string yarnText)
    {
        var declared = new Dictionary<string, object>();
        if (string.IsNullOrEmpty(yarnText))
        {
            return declared;
        }

        foreach (Match match in DeclareRegex.Matches(yarnText))
        {
            string name = match.Groups["name"].Value;
            string raw = match.Groups["value"].Value.Trim();

            declared[name] = ParseLiteral(raw);
        }

        return declared;
    }

    private static object ParseLiteral(string raw)
    {
        if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
        {
            return raw.Substring(1, raw.Length - 2);
        }

        if (bool.TryParse(raw, out bool b))
        {
            return b;
        }

        if (int.TryParse(raw, out int i))
        {
            return i;
        }

        if (float.TryParse(raw, out float f))
        {
            return f;
        }

        return raw;
    }

    /// <summary>
    /// Parses every dialogue node in a .yarn file. Nodes with no "speaker" header
    /// (such as init.yarn's declaration node) are skipped, matching the guidance
    /// in DIALOGUE_MECHANICS.md section 2C.
    /// </summary>
    public static List<DialogueNode> ParseNodes(string yarnText, string sourceName = "")
    {
        var nodes = new List<DialogueNode>();
        if (string.IsNullOrEmpty(yarnText))
        {
            return nodes;
        }

        // "===" terminates a node; everything before the first "---" is headers.
        string[] blocks = yarnText.Split(new[] { "\n===" }, StringSplitOptions.None);

        foreach (string rawBlock in blocks)
        {
            string block = rawBlock.Trim('\r', '\n', ' ', '\t', '=');
            if (block.Length == 0 || block.IndexOf("title:", StringComparison.Ordinal) < 0)
            {
                continue;
            }

            int separator = block.IndexOf("\n---", StringComparison.Ordinal);
            if (separator < 0)
            {
                continue;
            }

            string headerText = block.Substring(0, separator);
            string bodyText = block.Substring(separator + 4);

            var headers = ParseHeaders(headerText);
            if (!headers.ContainsKey("speaker") || string.IsNullOrEmpty(headers["speaker"]))
            {
                continue; // declaration-only nodes such as init.yarn's
            }

            var node = new DialogueNode
            {
                NodeName = Header(headers, "title"),
                Speaker = Header(headers, "speaker"),
                Category = Header(headers, "category"),
                Target = Header(headers, "target", "any"),
                Phase = Header(headers, "phase", "any"),
                Requires = Header(headers, "requires"),
                ReactTo = Header(headers, "react_to"),
                Priority = int.TryParse(Header(headers, "priority"), out int p) ? p : 1,
            };

            node.Lines = ParseBody(bodyText);

            if (node.Lines.Count == 0)
            {
                Debug.LogWarning($"[Yarn] {sourceName} node '{node.NodeName}' has no spoken lines; skipping.");
                continue;
            }

            nodes.Add(node);
        }

        return nodes;
    }

    private static Dictionary<string, string> ParseHeaders(string headerText)
    {
        var headers = new Dictionary<string, string>();

        foreach (string rawLine in headerText.Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("//"))
            {
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            string key = line.Substring(0, colon).Trim().ToLowerInvariant();
            string value = line.Substring(colon + 1).Trim();
            headers[key] = value;
        }

        return headers;
    }

    private static List<DialogueLine> ParseBody(string bodyText)
    {
        var lines = new List<DialogueLine>();

        foreach (string rawLine in bodyText.Split('\n'))
        {
            string line = rawLine.Trim();

            // Skip blanks, the //START and //END markers, and any other comment.
            if (line.Length == 0 || line.StartsWith("//"))
            {
                continue;
            }

            var match = LineRegex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            string speaker = match.Groups["speaker"].Value.Trim();
            string text = match.Groups["text"].Value.Trim();

            if (text.Length > 0)
            {
                lines.Add(new DialogueLine(speaker, text));
            }
        }

        return lines;
    }

    private static string Header(Dictionary<string, string> headers, string key, string fallback = "")
    {
        return headers.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value) ? value : fallback;
    }
}
