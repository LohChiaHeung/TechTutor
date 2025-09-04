using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class PanelNarration : MonoBehaviour
{
    [Header("Common (all optional)")]
    public TMP_Text title;
    public TMP_Text subtitle;
    public TMP_Text description;

    [Header("Extra UI text fields (optional, any count)")]
    public List<TMP_Text> extraTextFields = new();   // drag any TMP_Texts here

    [Header("Extra plain strings (optional, any count)")]
    [TextArea] public List<string> extraStrings = new();

    [Header("Formatting")]
    [Tooltip("Text inserted between parts when building narration.")]
    public string separator = ".";
    [Tooltip("Trim whitespace and skip empty/null parts.")]
    public bool skipEmpty = true;

    /// <summary>
    /// Build narration text from all assigned parts, in this order:
    /// title → subtitle → description → extraTextFields → extraStrings
    /// </summary>
    public string BuildNarrationText()
    {
        var parts = new List<string>(8);

        Add(parts, title ? title.GetParsedText() : null);
        Add(parts, subtitle ? subtitle.GetParsedText() : null);
        Add(parts, description ? description.GetParsedText() : null);

        if (extraTextFields != null)
            foreach (var t in extraTextFields)
                Add(parts, t ? t.GetParsedText() : null);

        if (extraStrings != null)
            foreach (var s in extraStrings)
                Add(parts, s);

        return Join(parts, separator);
    }

    void Add(List<string> list, string raw)
    {
        if (!skipEmpty) { list.Add(raw); return; }
        if (!string.IsNullOrWhiteSpace(raw))
            list.Add(raw.Trim());
    }

    string Join(List<string> parts, string sep)
    {
        var sb = new StringBuilder();
        bool first = true;
        foreach (var p in parts)
        {
            if (skipEmpty && string.IsNullOrWhiteSpace(p)) continue;
            if (!first) sb.Append(sep);
            sb.Append(p.Trim());
            first = false;
        }
        return sb.ToString().Trim();
    }
}
