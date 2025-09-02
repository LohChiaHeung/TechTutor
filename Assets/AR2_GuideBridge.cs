using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Normalized step model used in this scene.
/// </summary>
[Serializable]
public class AR2_Step
{
    public string title;
    public string instruction;
    public string[] keywords;
}

public class AR2_GuideBridge : MonoBehaviour
{
    [Header("Output for Step Driver")]
    public Texture2D screenshotOut;
    public AR2_Step[] stepsOut;

    [Header("Derivation")]
    public int maxKeywordsPerStep = 6;

    void Awake()
    {
        // 1) Prefer GuideRunContext if present
        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null && GuideRunContext.I.guide != null)
        {
            screenshotOut = GuideRunContext.I.screenshot;

            try
            {
                // Expecting guide.steps[] with fields: title, instruction, keywords (if your AIGuide differs, map here).
                var guide = GuideRunContext.I.guide;
                var list = new List<AR2_Step>();
                foreach (var s in guide.steps)
                {
                    var kws = (s.keywords != null && s.keywords.Length > 0)
                        ? s.keywords
                        : DeriveKeywords(s.title, s.instruction, maxKeywordsPerStep);

                    list.Add(new AR2_Step
                    {
                        title = s.title,
                        instruction = s.instruction,
                        keywords = kws
                    });
                }
                stepsOut = list.ToArray();
                return;
            }
            catch { /* if structure not matching, fall through to fallback */ }
        }

        // 2) Fallback to last plain reply saved in PlayerPrefs (Step 1 / → ...)
        if (screenshotOut == null)
        {
            Debug.LogWarning("[AR2] No screenshot found. Please pass one via GuideRunContext.");
        }

        var text = PlayerPrefs.GetString("last_reply", "");
        if (!string.IsNullOrWhiteSpace(text))
        {
            stepsOut = ParsePlainSteps(text, maxKeywordsPerStep);
        }
        else
        {
            // 3) final fallback demo
            stepsOut = new[]
            {
                new AR2_Step{ title="Step 1: Open the folder",   instruction="→ Click the 'Scenes' folder", keywords=new[]{"Scenes"} },
                new AR2_Step{ title="Step 2: Copy the folder",   instruction="→ Right-click and choose Copy", keywords=new[]{"Copy"} },
                new AR2_Step{ title="Step 3: Paste elsewhere",   instruction="→ Go to 'English Training' and Paste", keywords=new[]{"English","Training","Paste"} }
            };
        }
    }

    AR2_Step[] ParsePlainSteps(string text, int maxK)
    {
        var lines = (text ?? "").Replace("\r", "").Split('\n');
        var list = new List<AR2_Step>();
        AR2_Step cur = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("Step "))
            {
                if (cur != null)
                {
                    if (cur.keywords == null || cur.keywords.Length == 0)
                        cur.keywords = DeriveKeywords(cur.title, cur.instruction, maxK);
                    list.Add(cur);
                }
                cur = new AR2_Step { title = line, instruction = "", keywords = Array.Empty<string>() };
            }
            else if (line.StartsWith("→") && cur != null)
            {
                cur.instruction = line.Length > 1 ? line.Substring(1).Trim() : "";
            }
        }
        if (cur != null)
        {
            if (cur.keywords == null || cur.keywords.Length == 0)
                cur.keywords = DeriveKeywords(cur.title, cur.instruction, maxK);
            list.Add(cur);
        }

        return list.ToArray();
    }

    static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the","a","an","to","and","or","of","in","on","at","for","with","by","from","into","your",
        "this","that","it","is","are","as","be","then","now","you","me","we","i",
        "button","icon","menu","panel","window","app","application","screen","page","tab",
        "file","folder","document","documents","text","box","field","name","type","click","press","open","select"
    };

    string[] DeriveKeywords(string title, string instruction, int maxK)
    {
        var found = new List<string>();
        ExtractQuoted(title, found);
        ExtractQuoted(instruction, found);
        ExtractUiTokens(title, found);
        ExtractUiTokens(instruction, found);
        // Also add “after verb” phrases (Click X, Open Settings, etc.)
        ExtractAfterVerbs(title, found);
        ExtractAfterVerbs(instruction, found);

        // Cleanup
        var cleaned = new List<string>();
        foreach (var s in found)
        {
            var t = TrimPunct(s.Trim());
            if (t.Length < 2 || Stop.Contains(t)) continue;
            if (!cleaned.Contains(t, StringComparer.OrdinalIgnoreCase)) cleaned.Add(t);
        }
        if (cleaned.Count > maxK) cleaned = cleaned.GetRange(0, maxK);
        return cleaned.ToArray();
    }

    void ExtractQuoted(string s, List<string> outList)
    {
        if (string.IsNullOrEmpty(s)) return;
        var m1 = System.Text.RegularExpressions.Regex.Matches(s, "\"([^\"]+)\"");
        foreach (System.Text.RegularExpressions.Match m in m1) outList.Add(m.Groups[1].Value);
        var m2 = System.Text.RegularExpressions.Regex.Matches(s, "'([^']+)'");
        foreach (System.Text.RegularExpressions.Match m in m2) outList.Add(m.Groups[1].Value);
    }

    void ExtractUiTokens(string s, List<string> outList)
    {
        if (string.IsNullOrEmpty(s)) return;
        var rx = new System.Text.RegularExpressions.Regex(@"\b([A-Z][a-zA-Z0-9\+\-_/]{1,30}|[A-Z0-9\+\-_/]{2,30})\b");
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(s)) outList.Add(m.Value);
        int colon = s.IndexOf(':');
        if (colon >= 0 && colon + 1 < s.Length) outList.Add(s[(colon + 1)..].Trim());
    }

    void ExtractAfterVerbs(string src, List<string> outList)
    {
        if (string.IsNullOrEmpty(src)) return;
        string[] verbs = { "click", "press", "select", "choose", "open", "tap", "type", "enter", "go to", "navigate to", "search for", "use", "pick" };
        string verbsRx = string.Join("|", Array.ConvertAll(verbs, v => System.Text.RegularExpressions.Regex.Escape(v)));
        var rx = new System.Text.RegularExpressions.Regex(@"\b(" + verbsRx + @")\b\s+([^\.\,\n\r\(\)]{1,60})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(src))
        {
            var phrase = m.Groups[2].Value.Trim().Trim('.', ',', ')', '(', ':', ';');
            if (!string.IsNullOrEmpty(phrase)) outList.Add(phrase);
        }
    }

    string TrimPunct(string s)
    {
        int i = 0, j = s.Length - 1;
        while (i <= j && char.IsPunctuation(s[i])) i++;
        while (j >= i && char.IsPunctuation(s[j])) j--;
        return j >= i ? s.Substring(i, j - i + 1) : "";
    }
}
