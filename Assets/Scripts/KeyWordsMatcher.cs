using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public static class KeywordsMatcher
{
    // Minimal OCR model: adapt if your OcrResponse differs
    [Serializable] public class OcrWord { public string text; public float x, y, w, h; public float conf; }
    [Serializable] public class OcrResponse { public int width, height; public List<OcrWord> words; }

    public class MatchResult
    {
        public OcrWord word;
        public string keyword;
        public float score;
    }

    public static MatchResult FindBest(IEnumerable<string> keywords, OcrResponse resp, float minScore = 0.55f)
    {
        if (resp == null || resp.words == null || resp.words.Count == 0) return null;
        if (keywords == null) return null;

        var best = new MatchResult { score = -1f };
        foreach (var kw in keywords)
        {
            var k = Normalize(kw);
            if (string.IsNullOrEmpty(k)) continue;

            foreach (var w in resp.words)
            {
                var t = Normalize(w.text);
                float s = Similarity(k, t);
                if (s > best.score)
                {
                    best.score = s;
                    best.keyword = kw;
                    best.word = w;
                }
            }
        }
        return best.score >= minScore ? best : null;
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        // remove punctuation/extra spaces
        var chars = s.Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c));
        return string.Join(" ", new string(chars.ToArray()).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
    }

    // Soft similarity: exact → 1.0, contains → 0.8, prefix/suffix → 0.7, token overlap → 0.6, else Jaccard-ish
    static float Similarity(string a, string b)
    {
        if (a == b) return 1f;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        if (b.Contains(a)) return 0.85f;
        if (a.Contains(b)) return 0.75f;
        if (b.StartsWith(a) || b.EndsWith(a) || a.StartsWith(b) || a.EndsWith(b)) return 0.7f;

        var at = a.Split(' ');
        var bt = b.Split(' ');
        var inter = at.Intersect(bt).Count();
        var union = at.Union(bt).Count();
        return union == 0 ? 0f : Mathf.Clamp01(inter / (float)union * 0.9f); // cap under 1
    }
}
