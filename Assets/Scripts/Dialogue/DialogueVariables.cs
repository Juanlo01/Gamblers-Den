using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Holds the yarn variables from init.yarn. The engine writes these before every
/// dialogue request; nodes only read them, via their "requires:" header.
///
/// Names are stored without the leading '$'; both forms are accepted on lookup.
/// </summary>
public class DialogueVariables
{
    private readonly Dictionary<string, object> _values = new Dictionary<string, object>();

    public IReadOnlyDictionary<string, object> All => _values;

    /// <summary>Seeds the store from init.yarn's &lt;&lt;declare&gt;&gt; statements.</summary>
    public void LoadDeclarations(Dictionary<string, object> declarations)
    {
        foreach (var pair in declarations)
        {
            _values[Normalize(pair.Key)] = pair.Value;
        }
    }

    public void SetValue(string name, bool value) => _values[Normalize(name)] = value;

    public void SetValue(string name, int value) => _values[Normalize(name)] = value;

    public void SetValue(string name, string value) => _values[Normalize(name)] = value ?? string.Empty;

    public bool TryGetValue(string name, out object value) => _values.TryGetValue(Normalize(name), out value);

    public bool GetBool(string name)
    {
        return _values.TryGetValue(Normalize(name), out object v) && v is bool b && b;
    }

    public int GetInt(string name)
    {
        return _values.TryGetValue(Normalize(name), out object v) && v is int i ? i : 0;
    }

    public string GetString(string name)
    {
        return _values.TryGetValue(Normalize(name), out object v) && v != null ? v.ToString() : string.Empty;
    }

    /// <summary>
    /// Evaluates a node's "requires:" header. Supports the forms the scripts use
    /// today — a bare "$flag", several joined by "and", optional "not", and
    /// ==/!= comparisons against a literal. An empty expression always passes.
    /// </summary>
    public bool Evaluate(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return true;
        }

        string[] terms = expression.Split(new[] { " and " }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string rawTerm in terms)
        {
            if (!EvaluateTerm(rawTerm.Trim()))
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateTerm(string term)
    {
        if (term.Length == 0)
        {
            return true;
        }

        bool negate = false;
        if (term.StartsWith("not ", StringComparison.OrdinalIgnoreCase))
        {
            negate = true;
            term = term.Substring(4).Trim();
        }

        bool result;

        int eq = term.IndexOf("==", StringComparison.Ordinal);
        int neq = term.IndexOf("!=", StringComparison.Ordinal);

        if (eq >= 0)
        {
            result = CompareLiteral(term.Substring(0, eq), term.Substring(eq + 2), expectEqual: true);
        }
        else if (neq >= 0)
        {
            result = CompareLiteral(term.Substring(0, neq), term.Substring(neq + 2), expectEqual: false);
        }
        else
        {
            // Bare flag: true when the variable is a true bool, a non-zero int,
            // or a non-empty string.
            result = Truthy(Lookup(term));
        }

        return negate ? !result : result;
    }

    private bool CompareLiteral(string left, string right, bool expectEqual)
    {
        object value = Lookup(left.Trim());
        string literal = right.Trim().Trim('"');

        string asText = value == null
            ? string.Empty
            : Convert.ToString(value, CultureInfo.InvariantCulture);

        bool equal = string.Equals(asText, literal, StringComparison.OrdinalIgnoreCase);
        return expectEqual ? equal : !equal;
    }

    private object Lookup(string token)
    {
        string name = Normalize(token.Trim());
        return _values.TryGetValue(name, out object value) ? value : null;
    }

    private static bool Truthy(object value)
    {
        switch (value)
        {
            case null: return false;
            case bool b: return b;
            case int i: return i != 0;
            case float f: return !Mathf.Approximately(f, 0f);
            case string s: return s.Length > 0;
            default: return true;
        }
    }

    private static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return string.Empty;
        }

        name = name.Trim();
        return name.StartsWith("$") ? name.Substring(1) : name;
    }
}
