using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class KeywordMatcher
{
    // Minimal normalizer
    static string Norm(string s) => new string((s ?? "").Trim().ToLowerInvariant()
        .Where(ch => !char.IsPunctuation(ch)).ToArray());

    // Jaro-Winkler-ish quick score (0..1). Good enough for UI labels.
    public static float FuzzyScore(string a, string b)
    {
        a = Norm(a); b = Norm(b);
        if (a.Length == 0 || b.Length == 0) return 0f;
        if (a == b) return 1f;
        // cheap overlap ratio
        int L = Mathf.Min(a.Length, b.Length);
        int same = 0;
        for (int i = 0; i < L; i++) if (a[i] == b[i]) same++;
        float prefix = 0f; for (int i = 0; i < L && a[i] == b[i]; i++) prefix += 0.02f; // small prefix bonus
        return Mathf.Clamp01((2f * same) / (a.Length + b.Length) + prefix);
    }

    public struct Found
    {
        public Rect rectPx;   // in OCR pixel space
        public float score;
        public string matchedText;
    }

    /// Find best rect for a phrase (can span multiple OCR tokens)
    public static Found? FindBest(string phrase, OcrResponse ocr, float threshold = 0.82f, int maxNgram = 5)
    {
        if (ocr == null || ocr.words == null || ocr.words.Length == 0) return null;
        var p = Norm(phrase);
        if (string.IsNullOrEmpty(p)) return null;

        // Preprocess tokens (reading order: top->bottom, left->right)
        var tokens = ocr.words.Select(w => new
        {
            w,
            n = Norm(w.text)
        })
        .Where(t => !string.IsNullOrEmpty(t.n))
        .OrderBy(t => t.w.y).ThenBy(t => t.w.x)
        .ToArray();

        // 1) exact single-token
        foreach (var t in tokens)
        {
            if (t.n == p)
                return new Found { rectPx = new Rect(t.w.x, t.w.y, t.w.w, t.w.h), score = 1f, matchedText = t.w.text };
        }

        // 2) n-gram (phrase = multiple adjacent tokens)
        var pParts = p.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        int need = pParts.Length;

        if (need >= 2)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                int maxLen = Mathf.Min(maxNgram, tokens.Length - i);
                for (int k = need; k >= 2 && k <= maxLen; k--) // try phrase-length first
                {
                    var slice = tokens.Skip(i).Take(k).ToArray();
                    string join = string.Join(" ", slice.Select(s => s.n));
                    float sc = FuzzyScore(join, p);
                    if (sc >= threshold)
                    {
                        Rect r = ToUnion(slice.Select(s => new Rect(s.w.x, s.w.y, s.w.w, s.w.h)));
                        return new Found { rectPx = r, score = sc, matchedText = string.Join(" ", slice.Select(s => s.w.text)) };
                    }
                }
            }
        }

        // 3) fuzzy single-token fallback
        var best = tokens
            .Select(t => new { t, sc = FuzzyScore(t.n, p) })
            .OrderByDescending(x => x.sc)
            .FirstOrDefault();

        if (best != null && best.sc >= threshold - 0.1f)
        {
            return new Found
            {
                rectPx = new Rect(best.t.w.x, best.t.w.y, best.t.w.w, best.t.w.h),
                score = best.sc,
                matchedText = best.t.w.text
            };
        }

        return null;
    }

    static Rect ToUnion(IEnumerable<Rect> rects)
    {
        bool first = true;
        float xMin = 0, yMin = 0, xMax = 0, yMax = 0;
        foreach (var r in rects)
        {
            if (first) { xMin = r.xMin; yMin = r.yMin; xMax = r.xMax; yMax = r.yMax; first = false; }
            else { xMin = Mathf.Min(xMin, r.xMin); yMin = Mathf.Min(yMin, r.yMin); xMax = Mathf.Max(xMax, r.xMax); yMax = Mathf.Max(yMax, r.yMax); }
        }
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }
}
