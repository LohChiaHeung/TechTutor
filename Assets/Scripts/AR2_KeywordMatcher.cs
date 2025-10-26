using UnityEngine;

public static class AR2_KeywordMatcher
{
    public static OcrItem FindBest(string[] keywords, OcrResponse resp, float minScore = 0.55f)
    {
        if (resp == null || resp.words == null || resp.words.Length == 0) return null;
        if (keywords == null || keywords.Length == 0) return null;

        OcrItem best = null;
        float bestScore = minScore;

        foreach (var kw in keywords)
        {
            var k = Normalize(kw);
            if (string.IsNullOrEmpty(k)) continue;

            foreach (var w in resp.words)
            {
                float s = Similarity(k, Normalize(w.text));
                if (s > bestScore) { bestScore = s; best = w; }
            }
        }
        return best;
    }

    static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        System.Text.StringBuilder b = new();
        foreach (char c in s) if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)) b.Append(c);
        return System.Text.RegularExpressions.Regex.Replace(b.ToString(), @"\s+", " ").Trim();
    }

    static float Similarity(string a, string b)
    {
        if (a == b) return 1f;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;

        // Use Levenshtein distance for better matching
        int distance = LevenshteinDistance(a, b);
        int maxLength = Mathf.Max(a.Length, b.Length);
        return 1f - (float)distance / maxLength;
    }

    static int LevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; i++)
            d[i, 0] = i;
        for (int j = 0; j <= m; j++)
            d[0, j] = j;

        for (int j = 1; j <= m; j++)
        {
            for (int i = 1; i <= n; i++)
            {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(
                    Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
