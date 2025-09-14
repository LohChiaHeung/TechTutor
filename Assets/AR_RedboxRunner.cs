////using UnityEngine;
////using UnityEngine.Networking;
////using UnityEngine.UI;
////using System.Collections;
////using System.Text;
////using System.IO;
////using System.Collections.Generic;
////using System.Linq;

////public class AR_RedboxRunner : MonoBehaviour
////{
////    [Header("Refs")]
////    public OcrRedboxOverlayController overlay;
////    public Button nextButton;             // assign a UI Button
////    public Text statusText;               // optional: show “1/3 matches”

////    [Header("Server")]
////    public string serverUrl = "http://192.168.1.23:5000/ocr";

////    [Header("Fallback image")]
////    public string fallbackStreamingAsset = "test_screenshot.jpg";

////    [Header("Run")]
////    public bool runOnStart = true;
////    public float confMin = 0f;

////    [Header("Keyword filter (temporary)")]
////    [Tooltip("Words to highlight (case-insensitive). Later we can pull from GuideRunContext.I.guide.")]
////    public string[] keywords = new string[] { "file", "edit", "view" };

////    // state
////    Texture2D _tex;
////    OcrRedboxResponse _resp;
////    List<int> _matchIdx = new List<int>();
////    int _cur = -1;

////    static readonly Color GREEN = new Color(0f, 1f, 0f, 1f);

////    void Start()
////    {
////        if (nextButton) nextButton.onClick.AddListener(NextMatch);
////        if (runOnStart) StartCoroutine(Co_Run());
////    }

////    [ContextMenu("Run OCR Now")]
////    public void RunNow() => StartCoroutine(Co_Run());

////    IEnumerator Co_Run()
////    {
////        // 1) pick texture (GuideRunContext screenshot preferred)
////        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
////            _tex = GuideRunContext.I.screenshot;
////        else
////        {
////            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
////            byte[] imgBytes;
////#if UNITY_ANDROID && !UNITY_EDITOR
////            using (var www = UnityWebRequest.Get(path))
////            {
////                yield return www.SendWebRequest();
////                if (www.result != UnityWebRequest.Result.Success) { Debug.LogError(www.error); yield break; }
////                imgBytes = www.downloadHandler.data;
////            }
////#else
////            if (!File.Exists(path)) { Debug.LogError("Fallback not found: " + path); yield break; }
////            imgBytes = File.ReadAllBytes(path);
////#endif
////            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
////            _tex.LoadImage(imgBytes);
////        }

////        // 2) show texture
////        overlay.SetTexture(_tex);

////        // 3) POST to server
////        byte[] bytes = _tex.EncodeToJPG(60);
////        string b64 = System.Convert.ToBase64String(bytes);
////        string payload = "{\"image_base64\":\"" + b64 + "\"}";

////        var req = new UnityWebRequest(serverUrl, "POST");
////        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
////        req.downloadHandler = new DownloadHandlerBuffer();
////        req.SetRequestHeader("Content-Type", "application/json");

////        yield return req.SendWebRequest();
////        if (req.result != UnityWebRequest.Result.Success)
////        {
////            Debug.LogError("[Redbox] Server error: " + req.error);
////            SetStatus("Server error");
////            yield break;
////        }

////        // 4) parse
////        _resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
////        if (_resp == null || _resp.words == null) { Debug.LogError("[Redbox] Bad JSON"); SetStatus("Bad JSON"); yield break; }

////        // 5) build match list (case-insensitive, exact or contains)
////        _matchIdx.Clear();
////        var kw = new HashSet<string>(keywords.Select(k => k.Trim().ToLowerInvariant()).Where(k => k.Length > 0));
////        for (int i = 0; i < _resp.words.Count; i++)
////        {
////            var w = _resp.words[i];
////            if (w.conf < confMin) continue;
////            var t = (w.text ?? "").Trim().ToLowerInvariant();

////            // exact OR contains (choose one; here we allow contains)
////            bool isMatch = kw.Any(k => t == k || t.Contains(k));
////            if (isMatch) _matchIdx.Add(i);
////        }

////        _cur = -1;  // reset index
////        if (_matchIdx.Count == 0)
////        {
////            overlay.ClearBoxes();
////            SetStatus("No keyword matches");
////        }
////        else
////        {
////            NextMatch(); // show first match
////        }
////    }

////    public void NextMatch()
////    {
////        if (_resp == null || _matchIdx.Count == 0) { SetStatus("No matches"); return; }
////        _cur = (_cur + 1) % _matchIdx.Count;
////        int i = _matchIdx[_cur];
////        var word = _resp.words[i];
////        overlay.DrawOneWord(word, _resp.width, _resp.height, GREEN);
////        SetStatus($"Match {_cur + 1}/{_matchIdx.Count}: “{word.text}”");
////    }

////    void SetStatus(string s)
////    {
////        if (statusText) statusText.text = s;
////        Debug.Log("[Redbox] " + s);
////    }
////}


//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;
//using System.Collections;
//using System.Text;
//using System.IO;
//using System.Collections.Generic;
//using System.Linq;
//using GuideModel = global::AIGuide;
//using GuideStep = global::AIGuideStep;
//using System;

//public class AR_RedboxRunner : MonoBehaviour
//{
//    [Header("Refs")]
//    public OcrRedboxOverlayController overlay;
//    public Button nextKeywordButton;   // onClick → NextKeyword()
//    public Button nextStepButton;      // (optional) onClick → NextStep()
//    public Button prevStepButton;      // (optional) onClick → PrevStep()
//    public Text statusText;            // shows “Match 1/3: <word>”
//    public Text stepText;              // shows “Step 2/5 — <title>” (optional)

//    [Header("Server")]
//    public string serverUrl = "http://192.168.1.23:5000/ocr"; // set to your Flask URL

//    [Header("Fallback image")]
//    public string fallbackStreamingAsset = "test_screenshot.jpg";

//    [Header("Run")]
//    public bool runOnStart = false;
//    public float confMin = 0f;

//    // --- OCR STATE ---
//    Texture2D _tex;
//    OcrRedboxResponse _resp;

//    // --- MATCH STATE (for keywords in current step) ---
//    List<OcrRedboxWord> _matchedBoxes = new List<OcrRedboxWord>();
//    int _curMatch = -1;

//    // --- GUIDE STATE ---
//    AIGuide _guide;       // from GuideRunContext.I
//    int _curStep = 0;

//    static readonly Color GREEN = new Color(0f, 1f, 0f, 1f);
//    [SerializeField] bool useContextScreenshotOnly = true; // keep ON for phone builds

//    int _drawEpoch = 0;          // bumps when step changes
//    string _lastMatched = null;  // remember the last chosen token (to avoid looking stuck)




//    void Start()
//    {
//        // hook buttons
//        if (nextKeywordButton) nextKeywordButton.onClick.AddListener(NextKeyword);
//        if (nextStepButton) nextStepButton.onClick.AddListener(NextStep);
//        if (prevStepButton) prevStepButton.onClick.AddListener(PrevStep);

//        // capture guide (if available)
//        if (GuideRunContext.I != null)
//            _guide = GuideRunContext.I.guide;

//        if (runOnStart) StartCoroutine(Co_RunOcrOnce());

//        if (GuideRunContext.I?.guide?.steps != null)
//        {
//            var steps = GuideRunContext.I.guide.steps;
//            for (int i = 0; i < steps.Length; i++)
//            {
//                var kws = steps[i].keywords;
//                var list = (kws == null || kws.Length == 0) ? "(none)" : string.Join(", ", kws);
//                Debug.Log($"[Guide] Step {i + 1} \"{steps[i].title}\": keywords = {list}");
//            }
//        }

//    }


//    public void OnPlacedAndReady()
//    {
//        StartCoroutine(Co_WaitAndRun());
//    }

//    IEnumerator Co_WaitAndRun()
//    {
//        // 1) Wait for screenshot (from TechTutorAskUI/GuideRunContext)
//        yield return new WaitUntil(() => GuideRunContext.I?.screenshot != null);
//        _tex = GuideRunContext.I.screenshot;

//        // 2) Set texture FIRST so overlay has something to size from
//        overlay.SetTexture(_tex);

//        // 3) Wait for layout to settle (overlayRoot gains width/height)
//        yield return new WaitForEndOfFrame();
//        yield return new WaitUntil(() => overlay.overlayRoot.rect.width > 50f && overlay.overlayRoot.rect.height > 50f);

//        // 4) Now it’s safe to OCR
//        yield return Co_RunOcrOnce();
//    }


//    static string Norm(string s)
//    {
//        if (string.IsNullOrEmpty(s)) return "";
//        s = s.Trim().ToLowerInvariant();
//        // normalize i/l/1 confusion
//        s = s.Replace('ł', 'l'); // just in case
//        return s;
//    }

//    // allow tiny OCR noise; also treat i/l/1 as close
//    static bool FuzzyEq(string a, string b)
//    {
//        a = Norm(a); b = Norm(b);
//        if (a == b) return true;

//        string ai = a.Replace('1', 'i').Replace('l', 'i');
//        string bi = b.Replace('1', 'i').Replace('l', 'i');
//        if (ai == bi) return true;

//        // small Levenshtein (<=1 edit for short tokens)
//        int n = ai.Length, m = bi.Length;
//        if (n == 0 || m == 0) return false;
//        if (Mathf.Abs(n - m) > 1) return false;
//        int[,] d = new int[n + 1, m + 1];
//        for (int i = 0; i <= n; i++) d[i, 0] = i;
//        for (int j = 0; j <= m; j++) d[0, j] = j;
//        for (int i = 1; i <= n; i++)
//            for (int j = 1; j <= m; j++)
//            {
//                int cost = ai[i - 1] == bi[j - 1] ? 0 : 1;
//                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
//            }
//        int dist = d[n, m];
//        return (Mathf.Max(n, m) <= 5 && dist <= 1) || (1f - dist / (float)Mathf.Max(n, m) >= 0.82f);
//    }

//    // Jump to a step and (optionally) show the first matched keyword box
//    public void GotoStep(int index, bool showFirstMatch)
//    {
//        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0) return;
//        _curStep = Mathf.Clamp(index, 0, _guide.steps.Length - 1);
//        Debug.Log($"[Guide/Step] GotoStep({_curStep + 1}/{_guide.steps.Length})");
//        ApplyStepKeywords();                  // rebuild matches for this step
//        if (showFirstMatch) NextKeyword();    // draw the first green box (if any)
//    }

//    // Optional getter if you want UI to read titles from runner instead of context
//    public (int cur, int total, string title, string instruction) GetStepInfo()
//    {
//        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0)
//            return (0, 0, "", "");
//        var s = _guide.steps[_curStep];
//        return (_curStep + 1, _guide.steps.Length, s.title, s.instruction);
//    }

//    // try to find consecutive OCR words matching tokens[0..k-1]
//    bool TryFindPhraseSpan(List<OcrRedboxWord> words, string[] tokens, out int start, out int end)
//    {
//        start = end = -1;
//        if (tokens.Length == 0 || words == null || words.Count == 0) return false;

//        for (int i = 0; i < words.Count; i++)
//        {
//            if (!FuzzyEq(words[i].text, tokens[0])) continue;

//            int j = 1;
//            int idx = i + 1;
//            while (j < tokens.Length && idx < words.Count && FuzzyEq(words[idx].text, tokens[j]))
//            {
//                j++; idx++;
//            }
//            if (j == tokens.Length)
//            {
//                start = i;
//                end = idx - 1;
//                return true;
//            }
//        }
//        return false;
//    }

//    // merge bounding boxes from words[start..end]
//    OcrRedboxWord MergeSpan(List<OcrRedboxWord> words, int start, int end, string label)
//    {
//        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, conf = 1f;
//        for (int i = start; i <= end; i++)
//        {
//            var w = words[i];
//            minX = Mathf.Min(minX, w.x);
//            minY = Mathf.Min(minY, w.y);
//            maxX = Mathf.Max(maxX, w.x + w.w);
//            maxY = Mathf.Max(maxY, w.y + w.h);
//            conf = Mathf.Min(conf, w.conf);
//        }
//        return new OcrRedboxWord
//        {
//            text = label,
//            x = minX,
//            y = minY,
//            w = maxX - minX,
//            h = maxY - minY,
//            conf = conf
//        };
//    }


//    [ContextMenu("Run OCR Now")]
//    public void RunNow() => StartCoroutine(Co_RunOcrOnce());

//    //    IEnumerator Co_RunOcrOnce()
//    //    {
//    //        // 1) pick texture (GuideRunContext screenshot preferred)
//    //        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
//    //            _tex = GuideRunContext.I.screenshot;
//    //        else
//    //        {
//    //            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
//    //            byte[] imgBytes;
//    //#if UNITY_ANDROID && !UNITY_EDITOR
//    //            using (var www = UnityWebRequest.Get(path))
//    //            {
//    //                yield return www.SendWebRequest();
//    //                if (www.result != UnityWebRequest.Result.Success) { Debug.LogError(www.error); yield break; }
//    //                imgBytes = www.downloadHandler.data;
//    //            }
//    //#else
//    //            if (!File.Exists(path)) { Debug.LogError("Fallback not found: " + path); yield break; }
//    //            imgBytes = File.ReadAllBytes(path);
//    //#endif
//    //            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
//    //            _tex.LoadImage(imgBytes);
//    //        }

//    //        //// 2) show texture
//    //        //overlay.SetTexture(_tex);

//    //        //// 3) POST to server
//    //        //byte[] bytes = _tex.EncodeToJPG(60);
//    //        //string b64 = System.Convert.ToBase64String(bytes);
//    //        //string payload = "{\"image_base64\":\"" + b64 + "\"}";

//    //        //var req = new UnityWebRequest(serverUrl, "POST");
//    //        //req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
//    //        //req.downloadHandler = new DownloadHandlerBuffer();
//    //        //req.SetRequestHeader("Content-Type", "application/json");

//    //        //yield return req.SendWebRequest();
//    //        //if (req.result != UnityWebRequest.Result.Success)
//    //        //{
//    //        //    Debug.LogError("[Redbox] Server error: " + req.error);
//    //        //    SetStatus("Server error");
//    //        //    yield break;
//    //        //}
//    //        // 2) show texture
//    //        overlay.SetTexture(_tex);

//    //        // make it crisp in world-space canvas
//    //        if (_tex != null)
//    //        {
//    //            _tex.filterMode = FilterMode.Point;  // stop bilinear blur
//    //            _tex.anisoLevel = 0;
//    //        }
//    //        displayImage.SetNativeSize();            // show at native pixel size

//    //        // 3) POST to server
//    //        // 👉 IMPORTANT: use PNG, not JPG
//    //        byte[] bytes = _tex.EncodeToPNG();
//    //        Debug.Log($"[OCR/Client] Sending {_tex.width}x{_tex.height}, bytes={bytes.Length}");

//    //        string b64 = System.Convert.ToBase64String(bytes);
//    //        string payload = "{\"image_base64\":\"" + b64 + "\"}";

//    //        var req = new UnityWebRequest(serverUrl, "POST");
//    //        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
//    //        req.downloadHandler = new DownloadHandlerBuffer();
//    //        req.SetRequestHeader("Content-Type", "application/json");

//    //        yield return req.SendWebRequest();


//    //        // 4) parse OCR
//    //        _resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
//    //        if (_resp == null || _resp.words == null) { Debug.LogError("[Redbox] Bad JSON"); SetStatus("Bad JSON"); yield break; }

//    //        // 5) initialize at current step
//    //        ApplyStepKeywords();
//    //    }
//    IEnumerator Co_RunOcrOnce()
//    {
//        // 1) pick texture (GuideRunContext screenshot preferred)
//        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
//            _tex = GuideRunContext.I.screenshot;
//        else
//        {
//            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
//            byte[] imgBytes;
//#if UNITY_ANDROID && !UNITY_EDITOR
//        using (var www = UnityWebRequest.Get(path))
//        {
//            yield return www.SendWebRequest();
//            if (www.result != UnityWebRequest.Result.Success) { Debug.LogError(www.error); yield break; }
//            imgBytes = www.downloadHandler.data;
//        }
//#else
//            if (!File.Exists(path)) { Debug.LogError("Fallback not found: " + path); yield break; }
//            imgBytes = File.ReadAllBytes(path);
//#endif
//            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); // no mipmaps
//            _tex.LoadImage(imgBytes);
//        }

//        // 2) show texture (overlay handles crispness + sizing)
//        overlay.SetTexture(_tex);

//        // 3) POST to server (PNG, not JPG)
//        byte[] bytes = _tex.EncodeToPNG();
//        Debug.Log($"[OCR/Client] Sending {_tex.width}x{_tex.height}, bytes={bytes.Length}");
//        string b64 = System.Convert.ToBase64String(bytes);
//        string payload = "{\"image_base64\":\"" + b64 + "\"}";

//        var req = new UnityWebRequest(serverUrl, "POST");
//        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
//        req.downloadHandler = new DownloadHandlerBuffer();
//        req.SetRequestHeader("Content-Type", "application/json");

//        yield return req.SendWebRequest();

//        if (req.result != UnityWebRequest.Result.Success)
//        {
//            Debug.LogError("[Redbox] Server error: " + req.error);
//            SetStatus("Server error");
//            yield break;
//        }

//        // 4) parse OCR
//        _resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
//        Debug.Log($"[OCR/Resp] img={_resp.width}x{_resp.height}, words={_resp.words?.Count ?? 0}");
//        if (_resp == null || _resp.words == null)
//        {
//            Debug.LogError("[Redbox] Bad JSON");
//            SetStatus("Bad JSON");
//            yield break;
//        }

//        // 5) initialize at current step (prunes + builds matches, then shows first box)
//        ApplyStepKeywords();
//    }


//    // --- STEP CONTROL ---
//    static string MD5Hex(byte[] data)
//    {
//        using (var md5 = System.Security.Cryptography.MD5.Create())
//        {
//            var hash = md5.ComputeHash(data);
//            var sb = new System.Text.StringBuilder(hash.Length * 2);
//            foreach (var b in hash) sb.Append(b.ToString("x2"));
//            return sb.ToString();
//        }
//    }

//    public void NextStep()
//    {
//        if (!HasSteps()) { SetStepText("No steps"); return; }
//        _curStep = Mathf.Min(_curStep + 1, _guide.steps.Length - 1);
//        ApplyStepKeywords();
//    }

//    public void PrevStep()
//    {
//        if (!HasSteps()) { SetStepText("No steps"); return; }
//        _curStep = Mathf.Max(_curStep - 1, 0);
//        ApplyStepKeywords();
//    }

//    bool HasSteps()
//    {
//        return _guide != null && _guide.steps != null && _guide.steps.Length > 0;
//    }

//    // --- put these in your runner ---

//    // Normalize for fuzzy compare (handles l/1/i, punctuation, spacing)
//    static string Clean(string s)
//    {
//        if (string.IsNullOrEmpty(s)) return "";
//        s = s.ToLowerInvariant().Trim();
//        var sb = new System.Text.StringBuilder(s.Length);
//        foreach (var c in s) if (char.IsLetterOrDigit(c) || c == '+' || c == ' ') sb.Append(c);
//        s = sb.ToString().Replace('1', 'i').Replace('l', 'i');
//        return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
//    }

//    public static event System.Action<string> OnKeywordMatched;

//    static bool FuzzyContains(string haystack, string needle)
//    {
//        string H = Clean(haystack), N = Clean(needle);
//        if (string.IsNullOrEmpty(H) || string.IsNullOrEmpty(N)) return false;
//        if (H.Contains(N)) return true;
//        foreach (var part in H.Split(' '))
//        {
//            var p = part.Trim();
//            if (p.Length == 0) continue;
//            // tiny edit distance
//            int n = p.Length, m = N.Length;
//            if (Mathf.Abs(n - m) > 1) continue;
//            var d = new int[n + 1, m + 1];
//            for (int i = 0; i <= n; i++) d[i, 0] = i;
//            for (int j = 0; j <= m; j++) d[0, j] = j;
//            for (int i = 1; i <= n; i++)
//                for (int j = 1; j <= m; j++)
//                {
//                    int cost = (p[i - 1] == N[j - 1]) ? 0 : 1;
//                    d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
//                }
//            int dist = d[n, m];
//            if ((Mathf.Max(n, m) <= 5 && dist <= 1) || (1f - dist / (float)Mathf.Max(n, m) >= 0.82f)) return true;
//        }
//        return false;
//    }

//    // Build cleaned OCR lexicon for quick checks
//    HashSet<string> BuildOcrLexicon(OcrRedboxResponse resp)
//    {
//        return new HashSet<string>(
//            resp.words.Select(w => Clean(w.text ?? "")).Where(t => t.Length >= 2)
//        );
//    }

//    // Keep only keywords that appear in OCR (fuzzy/contains). If none survive, leave empty.
//    //string[] PruneKeywordsToOCR(string[] keywords, OcrRedboxResponse resp)
//    //{
//    //    if (keywords == null || resp?.words == null) return System.Array.Empty<string>();
//    //    var kept = new List<string>();
//    //    foreach (var kw in keywords)
//    //    {
//    //        if (string.IsNullOrWhiteSpace(kw)) continue;
//    //        bool hit = resp.words.Any(w => FuzzyContains(w.text ?? "", kw));
//    //        if (hit) kept.Add(kw);
//    //    }
//    //    // de-dup & cap length
//    //    return kept.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
//    //}
//    string[] PruneKeywordsToOCR(string[] keywords, OcrRedboxResponse resp)
//    {
//        if (keywords == null || resp?.words == null) return System.Array.Empty<string>();
//        var kept = new List<string>();
//        var words = resp.words;

//        foreach (var kw in keywords)
//        {
//            if (string.IsNullOrWhiteSpace(kw)) continue;
//            var tokens = kw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

//            bool hit = false;

//            // 1) phrase match (consecutive OCR words)
//            if (tokens.Length > 1 && TryFindPhraseSpan(words, tokens, out _, out _))
//            {
//                hit = true;
//            }
//            else
//            {
//                // 2) exact-ish token match
//                foreach (var w in words)
//                {
//                    var t = w.text ?? "";
//                    if (FuzzyEq(t, kw)) { hit = true; break; }

//                    // 3) LAST RESORT: allow "contains" only for longer keywords
//                    if (kw.Length >= 5 && FuzzyContains(t, kw)) { hit = true; break; }
//                }
//            }

//            if (hit) kept.Add(kw);
//        }

//        return kept.Distinct(System.StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
//    }


//    //void ApplyStepKeywords()
//    //{
//    //    overlay.ClearBoxes();
//    //    // Update header text (title)
//    //    if (HasSteps())
//    //    {
//    //        var s = _guide.steps[_curStep];
//    //        string title = s.title;                     // <<< adjust if your field is named differently
//    //        SetStepText($"Step {_curStep + 1}/{_guide.steps.Length} — {title}");
//    //    }
//    //    else
//    //    {
//    //        SetStepText("Step 0/0 — (no guide)");
//    //    }

//    //    // Build match list for this step’s keywords
//    //    RebuildMatchesForCurrentStep();

//    //    if (_matchedBoxes.Count == 0)
//    //    {
//    //        overlay.ClearBoxes();
//    //        SetStatus("No keyword matches");
//    //        _curMatch = -1;
//    //    }
//    //    else
//    //    {
//    //        _curMatch = -1;
//    //        NextKeyword();   // show first keyword hit
//    //    }
//    //}
//    //void ApplyStepKeywords()
//    //{
//    //    // ✅ Always clear previous boxes when step changes
//    //    overlay.ClearBoxes();

//    //    // Update header text (title)
//    //    if (HasSteps())
//    //    {
//    //        var s = _guide.steps[_curStep];
//    //        string title = s.title;
//    //        SetStepText($"Step {_curStep + 1}/{_guide.steps.Length} — {title}");
//    //    }
//    //    else
//    //    {
//    //        SetStepText("Step 0/0 — (no guide)");
//    //    }

//    //    // Rebuild matches for this step’s keywords
//    //    RebuildMatchesForCurrentStep();
//    //    var stepKws = (_guide?.steps?[_curStep].keywords) ?? System.Array.Empty<string>();
//    //    Debug.Log($"[Guide/Step] Now at step #{_curStep + 1} rawKW=({string.Join(", ", stepKws)}) " +
//    //              $"matchedBoxes={_matchedBoxes.Count}");


//    //    if (_matchedBoxes.Count == 0)
//    //    {
//    //        SetStatus("No keyword matches");
//    //        _curMatch = -1;
//    //    }
//    //    else
//    //    {
//    //        _curMatch = -1;
//    //        NextKeyword();   // will draw + clear internally again (double-safe)
//    //    }
//    //}

//    void ApplyStepKeywords()
//    {
//        // wipe any previous boxes immediately
//        overlay.ClearBoxes();

//        // bump epoch so late draws from older steps are ignored (if you guard them)
//        _drawEpoch++;

//        // header
//        if (HasSteps())
//        {
//            var s = _guide.steps[_curStep];
//            SetStepText($"Step {_curStep + 1}/{_guide.steps.Length} — {s.title}");
//        }
//        else
//        {
//            SetStepText("Step 0/0 — (no guide)");
//        }

//        // rebuild matches for THIS step
//        RebuildMatchesForCurrentStep();

//        // draw or clear
//        if (_matchedBoxes.Count == 0)
//        {
//            _curMatch = -1;
//            SetStatus("No keyword matches");
//            // overlay already cleared
//        }
//        else
//        {
//            _curMatch = -1;
//            NextKeyword();   // will draw first match
//        }
//    }


//    void RebuildMatchesForCurrentStep()
//    {
//        _matchedBoxes.Clear();
//        if (_resp == null || _resp.words == null) return;

//        // 1) get raw keywords for this step
//        string[] kwList = null;
//        if (HasSteps()) kwList = _guide.steps[_curStep].keywords;
//        if (kwList == null) kwList = System.Array.Empty<string>();

//        // 2) prune keywords to only those present in OCR
//        kwList = PruneKeywordsToOCR(kwList, _resp);

//        Debug.Log("[OCR Words] " + string.Join(" | ", _resp.words.Select(w => w.text)));
//        Debug.Log("[Guide/Pruned] Step " + (_curStep + 1) + " = " + (kwList.Length == 0 ? "(none)" : string.Join(", ", kwList)));

//        // 3) for each keyword, try phrase → fallback tokens
//        foreach (var kw in kwList)
//        {
//            if (string.IsNullOrWhiteSpace(kw)) continue;

//            var tokens = kw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
//            int s, e;

//            // Full phrase: consecutive OCR words
//            if (tokens.Length > 1 && TryFindPhraseSpan(_resp.words, tokens, out s, out e))
//            {
//                _matchedBoxes.Add(MergeSpan(_resp.words, s, e, kw));
//                continue;
//            }

//            // Fallback: try each token individually
//            foreach (var t in tokens)
//            {
//                for (int i = 0; i < _resp.words.Count; i++)
//                {
//                    var w = _resp.words[i];
//                    if (w.conf < confMin) continue;
//                    // avoid looking "stuck": if we have >1 boxes and one equals the last matched, drop it
//                    if (_matchedBoxes.Count > 1 && !string.IsNullOrEmpty(_lastMatched))
//                    {
//                        _matchedBoxes = _matchedBoxes
//                            .Where(b => !FuzzyEq(b.text ?? "", _lastMatched))
//                            .ToList();
//                    }

//                    if (FuzzyEq(w.text ?? "", t) || FuzzyContains(w.text ?? "", t))
//                    {
//                        _matchedBoxes.Add(w);
//                        break; // take first match of this token
//                    }
//                }
//            }
//        }
//    }



//    // --- KEYWORD CYCLING ---

//    //public void NextKeyword()
//    //{
//    //    if (_matchedBoxes.Count == 0)
//    //    {
//    //        overlay.ClearBoxes();
//    //        SetStatus("No matches");
//    //        return;
//    //    }

//    //    _curMatch = (_curMatch + 1) % _matchedBoxes.Count;

//    //    var box = _matchedBoxes[_curMatch];
//    //    overlay.DrawOneWord(box, _resp.width, _resp.height, GREEN);
//    //    SetStatus($"Match {_curMatch + 1}/{_matchedBoxes.Count}: \"{box.text}\"");
//    //}
//    public void NextKeyword()
//    {
//        if (_matchedBoxes.Count == 0)
//        {
//            overlay.ClearBoxes();
//            SetStatus("No matches");
//            return;
//        }

//        _curMatch = (_curMatch + 1) % _matchedBoxes.Count;

//        var box = _matchedBoxes[_curMatch];
//        overlay.DrawOneWord(box, _resp.width, _resp.height, GREEN);

//        _lastMatched = box.text;                 // remember
//        OnKeywordMatched?.Invoke(box.text);      // tell the UI

//        SetStatus($"Match {_curMatch + 1}/{_matchedBoxes.Count}: \"{box.text}\"");
//    }



//    // --- UI helpers ---

//    void SetStatus(string s)
//    {
//        if (statusText) statusText.text = s;
//        Debug.Log("[Redbox] " + s);
//    }

//    void SetStepText(string s)
//    {
//        if (stepText) stepText.text = s;
//        Debug.Log("[Redbox] " + s);
//    }
//}


using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using GuideModel = global::AIGuide;
using GuideStep = global::AIGuideStep;
using System;

public class AR_RedboxRunner : MonoBehaviour
{
    [Header("Refs")]
    public OcrRedboxOverlayController overlay;
    public Button nextKeywordButton;   // onClick → NextKeyword()
    public Button nextStepButton;      // (optional) onClick → NextStep()
    public Button prevStepButton;      // (optional) onClick → PrevStep()
    public Text statusText;            // shows “Match 1/3: <word>”
    public Text stepText;              // shows “Step 2/5 — <title>” (optional)

    [Header("Server")]
    public string serverUrl = "http://192.168.1.23:5000/ocr"; // set to your Flask URL

    [Header("Fallback image")]
    public string fallbackStreamingAsset = "test_screenshot.jpg";

    [Header("Run")]
    public bool runOnStart = false;
    public float confMin = 0f;

    [Header("Display Mode")]
    public bool showAllMatchesForStep = true; // ← draw all by default


    // --- OCR STATE ---
    Texture2D _tex;
    OcrRedboxResponse _resp;

    // --- MATCH STATE (for keywords in current step) ---
    List<OcrRedboxWord> _matchedBoxes = new List<OcrRedboxWord>();
    int _curMatch = -1;

    // --- GUIDE STATE ---
    AIGuide _guide;       // from GuideRunContext.I
    int _curStep = 0;

    static readonly Color RED = new Color(1f, 1f, 0f, 1f);
    [SerializeField] bool useContextScreenshotOnly = true; // keep ON for phone builds


    void Start()
    {
        // hook buttons
        if (nextKeywordButton) nextKeywordButton.onClick.AddListener(NextKeyword);
        if (nextStepButton) nextStepButton.onClick.AddListener(NextStep);
        if (prevStepButton) prevStepButton.onClick.AddListener(PrevStep);

        // capture guide (if available)
        if (GuideRunContext.I != null)
            _guide = GuideRunContext.I.guide;

        if (runOnStart) StartCoroutine(Co_RunOcrOnce());

        if (GuideRunContext.I?.guide?.steps != null)
        {
            var steps = GuideRunContext.I.guide.steps;
            for (int i = 0; i < steps.Length; i++)
            {
                var kws = steps[i].keywords;
                var list = (kws == null || kws.Length == 0) ? "(none)" : string.Join(", ", kws);
                Debug.Log($"[Guide] Step {i + 1} \"{steps[i].title}\": keywords = {list}");
            }
        }

    }

    static string Norm(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Trim().ToLowerInvariant();
        // normalize i/l/1 confusion
        s = s.Replace('ł', 'l'); // just in case
        return s;
    }

    // allow tiny OCR noise; also treat i/l/1 as close
    static bool FuzzyEq(string a, string b)
    {
        a = Norm(a); b = Norm(b);
        if (a == b) return true;

        string ai = a.Replace('1', 'i').Replace('l', 'i');
        string bi = b.Replace('1', 'i').Replace('l', 'i');
        if (ai == bi) return true;

        // small Levenshtein (<=1 edit for short tokens)
        int n = ai.Length, m = bi.Length;
        if (n == 0 || m == 0) return false;
        if (Mathf.Abs(n - m) > 1) return false;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
            for (int j = 1; j <= m; j++)
            {
                int cost = ai[i - 1] == bi[j - 1] ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        int dist = d[n, m];
        return (Mathf.Max(n, m) <= 5 && dist <= 1) || (1f - dist / (float)Mathf.Max(n, m) >= 0.82f);
    }

    // Jump to a step and (optionally) show the first matched keyword box
    public void GotoStep(int index, bool showFirstMatch)
    {
        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0) return;
        _curStep = Mathf.Clamp(index, 0, _guide.steps.Length - 1);
        ApplyStepKeywords();                  // rebuild matches for this step
        if (showFirstMatch) NextKeyword();    // draw the first green box (if any)
    }

    public void OnPlacedAndReady()
    {
        StartCoroutine(Co_WaitAndRun());
    }

    // AR_RedboxRunner.cs  (replace Co_WaitAndRun with this safer version)
    IEnumerator Co_WaitAndRun()
    {
        // Wait up to ~2s for GuideRunContext screenshot
        float start = Time.realtimeSinceStartup;
        while (GuideRunContext.I?.screenshot == null && Time.realtimeSinceStartup - start < 2f)
            yield return null;

        // Use context screenshot, or fallback from StreamingAssets
        if (GuideRunContext.I?.screenshot != null)
            _tex = GuideRunContext.I.screenshot;
        else
        {
            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
#if UNITY_ANDROID && !UNITY_EDITOR
        var www = UnityEngine.Networking.UnityWebRequest.Get(path);
        yield return www.SendWebRequest();
        if (www.result == UnityWebRequest.Result.Success)
        {
            var bytes = www.downloadHandler.data;
            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _tex.LoadImage(bytes);
        }
#else
            if (File.Exists(path))
            {
                var bytes = File.ReadAllBytes(path);
                _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                _tex.LoadImage(bytes);
            }
#endif
        }

        // Show the image immediately so RawImage isn’t white
        if (_tex != null) overlay.SetTexture(_tex);

        // Wait for overlay to have real size, then OCR
        yield return new WaitForEndOfFrame();
        yield return new WaitUntil(() => overlay.overlayRoot.rect.width > 50f &&
                                         overlay.overlayRoot.rect.height > 50f);

        yield return Co_RunOcrOnce();
    }


    // Optional getter if you want UI to read titles from runner instead of context
    public (int cur, int total, string title, string instruction) GetStepInfo()
    {
        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0)
            return (0, 0, "", "");
        var s = _guide.steps[_curStep];
        return (_curStep + 1, _guide.steps.Length, s.title, s.instruction);
    }

    // try to find consecutive OCR words matching tokens[0..k-1]
    bool TryFindPhraseSpan(List<OcrRedboxWord> words, string[] tokens, out int start, out int end)
    {
        start = end = -1;
        if (tokens.Length == 0 || words == null || words.Count == 0) return false;

        for (int i = 0; i < words.Count; i++)
        {
            if (!FuzzyEq(words[i].text, tokens[0])) continue;

            int j = 1;
            int idx = i + 1;
            while (j < tokens.Length && idx < words.Count && FuzzyEq(words[idx].text, tokens[j]))
            {
                j++; idx++;
            }
            if (j == tokens.Length)
            {
                start = i;
                end = idx - 1;
                return true;
            }
        }
        return false;
    }

    // merge bounding boxes from words[start..end]
    OcrRedboxWord MergeSpan(List<OcrRedboxWord> words, int start, int end, string label)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue, conf = 1f;
        for (int i = start; i <= end; i++)
        {
            var w = words[i];
            minX = Mathf.Min(minX, w.x);
            minY = Mathf.Min(minY, w.y);
            maxX = Mathf.Max(maxX, w.x + w.w);
            maxY = Mathf.Max(maxY, w.y + w.h);
            conf = Mathf.Min(conf, w.conf);
        }
        return new OcrRedboxWord
        {
            text = label,
            x = minX,
            y = minY,
            w = maxX - minX,
            h = maxY - minY,
            conf = conf
        };
    }


    [ContextMenu("Run OCR Now")]
    public void RunNow() => StartCoroutine(Co_RunOcrOnce());

    //    IEnumerator Co_RunOcrOnce()
    //    {
    //        // 1) pick texture (GuideRunContext screenshot preferred)
    //        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
    //            _tex = GuideRunContext.I.screenshot;
    //        else
    //        {
    //            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
    //            byte[] imgBytes;
    //#if UNITY_ANDROID && !UNITY_EDITOR
    //            using (var www = UnityWebRequest.Get(path))
    //            {
    //                yield return www.SendWebRequest();
    //                if (www.result != UnityWebRequest.Result.Success) { Debug.LogError(www.error); yield break; }
    //                imgBytes = www.downloadHandler.data;
    //            }
    //#else
    //            if (!File.Exists(path)) { Debug.LogError("Fallback not found: " + path); yield break; }
    //            imgBytes = File.ReadAllBytes(path);
    //#endif
    //            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    //            _tex.LoadImage(imgBytes);
    //        }

    //        //// 2) show texture
    //        //overlay.SetTexture(_tex);

    //        //// 3) POST to server
    //        //byte[] bytes = _tex.EncodeToJPG(60);
    //        //string b64 = System.Convert.ToBase64String(bytes);
    //        //string payload = "{\"image_base64\":\"" + b64 + "\"}";

    //        //var req = new UnityWebRequest(serverUrl, "POST");
    //        //req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
    //        //req.downloadHandler = new DownloadHandlerBuffer();
    //        //req.SetRequestHeader("Content-Type", "application/json");

    //        //yield return req.SendWebRequest();
    //        //if (req.result != UnityWebRequest.Result.Success)
    //        //{
    //        //    Debug.LogError("[Redbox] Server error: " + req.error);
    //        //    SetStatus("Server error");
    //        //    yield break;
    //        //}
    //        // 2) show texture
    //        overlay.SetTexture(_tex);

    //        // make it crisp in world-space canvas
    //        if (_tex != null)
    //        {
    //            _tex.filterMode = FilterMode.Point;  // stop bilinear blur
    //            _tex.anisoLevel = 0;
    //        }
    //        displayImage.SetNativeSize();            // show at native pixel size

    //        // 3) POST to server
    //        // 👉 IMPORTANT: use PNG, not JPG
    //        byte[] bytes = _tex.EncodeToPNG();
    //        Debug.Log($"[OCR/Client] Sending {_tex.width}x{_tex.height}, bytes={bytes.Length}");

    //        string b64 = System.Convert.ToBase64String(bytes);
    //        string payload = "{\"image_base64\":\"" + b64 + "\"}";

    //        var req = new UnityWebRequest(serverUrl, "POST");
    //        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
    //        req.downloadHandler = new DownloadHandlerBuffer();
    //        req.SetRequestHeader("Content-Type", "application/json");

    //        yield return req.SendWebRequest();


    //        // 4) parse OCR
    //        _resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
    //        if (_resp == null || _resp.words == null) { Debug.LogError("[Redbox] Bad JSON"); SetStatus("Bad JSON"); yield break; }

    //        // 5) initialize at current step
    //        ApplyStepKeywords();
    //    }
    IEnumerator Co_RunOcrOnce()
    {
        // 1) pick texture (GuideRunContext screenshot preferred)
        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
            _tex = GuideRunContext.I.screenshot;
        else
        {
            string path = Path.Combine(Application.streamingAssetsPath, fallbackStreamingAsset);
            byte[] imgBytes;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) { Debug.LogError(www.error); yield break; }
            imgBytes = www.downloadHandler.data;
        }
#else
            if (!File.Exists(path)) { Debug.LogError("Fallback not found: " + path); yield break; }
            imgBytes = File.ReadAllBytes(path);
#endif
            _tex = new Texture2D(2, 2, TextureFormat.RGBA32, false); // no mipmaps
            _tex.LoadImage(imgBytes);
        }

        // 2) show texture (overlay handles crispness + sizing)
        overlay.SetTexture(_tex);

        // 3) POST to server (PNG, not JPG)
        byte[] bytes = _tex.EncodeToPNG();
        Debug.Log($"[OCR/Client] Sending {_tex.width}x{_tex.height}, bytes={bytes.Length}");
        string b64 = System.Convert.ToBase64String(bytes);
        string payload = "{\"image_base64\":\"" + b64 + "\"}";

        var req = new UnityWebRequest(serverUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Redbox] Server error: " + req.error);
            SetStatus("Server error");
            yield break;
        }

        // 4) parse OCR
        _resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
        if (_resp == null || _resp.words == null)
        {
            Debug.LogError("[Redbox] Bad JSON");
            SetStatus("Bad JSON");
            yield break;
        }

        // 5) initialize at current step (prunes + builds matches, then shows first box)
        ApplyStepKeywords();
    }


    // --- STEP CONTROL ---
    static string MD5Hex(byte[] data)
    {
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(data);
            var sb = new System.Text.StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    public void NextStep()
    {
        if (!HasSteps()) { SetStepText("No steps"); return; }
        _curStep = Mathf.Min(_curStep + 1, _guide.steps.Length - 1);
        ApplyStepKeywords();
    }

    public void PrevStep()
    {
        if (!HasSteps()) { SetStepText("No steps"); return; }
        _curStep = Mathf.Max(_curStep - 1, 0);
        ApplyStepKeywords();
    }

    bool HasSteps()
    {
        return _guide != null && _guide.steps != null && _guide.steps.Length > 0;
    }

    // --- put these in your runner ---

    // Normalize for fuzzy compare (handles l/1/i, punctuation, spacing)
    static string Clean(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.ToLowerInvariant().Trim();
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (var c in s) if (char.IsLetterOrDigit(c) || c == '+' || c == ' ') sb.Append(c);
        s = sb.ToString().Replace('1', 'i').Replace('l', 'i');
        return System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ").Trim();
    }

    static bool FuzzyContains(string haystack, string needle)
    {
        string H = Clean(haystack), N = Clean(needle);
        if (string.IsNullOrEmpty(H) || string.IsNullOrEmpty(N)) return false;
        if (H.Contains(N)) return true;
        foreach (var part in H.Split(' '))
        {
            var p = part.Trim();
            if (p.Length == 0) continue;
            // tiny edit distance
            int n = p.Length, m = N.Length;
            if (Mathf.Abs(n - m) > 1) continue;
            var d = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++) d[i, 0] = i;
            for (int j = 0; j <= m; j++) d[0, j] = j;
            for (int i = 1; i <= n; i++)
                for (int j = 1; j <= m; j++)
                {
                    int cost = (p[i - 1] == N[j - 1]) ? 0 : 1;
                    d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            int dist = d[n, m];
            if ((Mathf.Max(n, m) <= 5 && dist <= 1) || (1f - dist / (float)Mathf.Max(n, m) >= 0.82f)) return true;
        }
        return false;
    }

    // Build cleaned OCR lexicon for quick checks
    HashSet<string> BuildOcrLexicon(OcrRedboxResponse resp)
    {
        return new HashSet<string>(
            resp.words.Select(w => Clean(w.text ?? "")).Where(t => t.Length >= 2)
        );
    }

    // Keep only keywords that appear in OCR (fuzzy/contains). If none survive, leave empty.
    string[] PruneKeywordsToOCR(string[] keywords, OcrRedboxResponse resp)
    {
        if (keywords == null || resp?.words == null) return System.Array.Empty<string>();
        var kept = new List<string>();
        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;
            bool hit = resp.words.Any(w => FuzzyContains(w.text ?? "", kw));
            if (hit) kept.Add(kw);
        }
        // de-dup & cap length
        return kept.Distinct(StringComparer.OrdinalIgnoreCase).Take(3).ToArray();
    }

    void ApplyStepKeywords()
    {
        overlay.ClearBoxes();

        // Update header text (title)
        if (HasSteps())
        {
            var s = _guide.steps[_curStep];
            string title = s.title;                     // <<< adjust if your field is named differently
            SetStepText($"Step {_curStep + 1}/{_guide.steps.Length} — {title}");
        }
        else
        {
            SetStepText("Step 0/0 — (no guide)");
        }

        // Build match list for this step’s keywords
        RebuildMatchesForCurrentStep();

        // draw or clear
        if (_matchedBoxes.Count == 0)
        {
            _curMatch = -1;
            overlay.ClearBoxes();
            SetStatus("No keyword matches");
        }
        else
        {
            if (showAllMatchesForStep)
            {
                // ✅ Draw every relevant box for this step at once
                overlay.DrawWords(_matchedBoxes, _resp.width, _resp.height, confMin);
                SetStatus($"{_matchedBoxes.Count} matches shown");
            }
            else
            {
                // keep old behavior: cycle one-by-one
                _curMatch = -1;
                NextKeyword();
            }
        }

    }

    void RebuildMatchesForCurrentStep()
    {
        _matchedBoxes.Clear();
        if (_resp == null || _resp.words == null) return;

        // 1) get raw keywords for this step
        string[] kwList = null;
        if (HasSteps()) kwList = _guide.steps[_curStep].keywords;
        if (kwList == null) kwList = System.Array.Empty<string>();

        // 2) prune keywords to only those present in OCR
        kwList = PruneKeywordsToOCR(kwList, _resp);

        Debug.Log("[OCR Words] " + string.Join(" | ", _resp.words.Select(w => w.text)));
        Debug.Log("[Guide/Pruned] Step " + (_curStep + 1) + " = " + (kwList.Length == 0 ? "(none)" : string.Join(", ", kwList)));

        // 3) for each keyword, try phrase → fallback tokens
        foreach (var kw in kwList)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;

            var tokens = kw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int s, e;

            // Full phrase: consecutive OCR words
            if (tokens.Length > 1 && TryFindPhraseSpan(_resp.words, tokens, out s, out e))
            {
                _matchedBoxes.Add(MergeSpan(_resp.words, s, e, kw));
                continue;
            }

            // Fallback: try each token individually
            foreach (var t in tokens)
            {
                for (int i = 0; i < _resp.words.Count; i++)
                {
                    var w = _resp.words[i];
                    if (w.conf < confMin) continue;
                    if (FuzzyEq(w.text ?? "", t) || FuzzyContains(w.text ?? "", t))
                    {
                        _matchedBoxes.Add(w);
                        break; // take first match of this token
                    }
                }
            }
        }
    }



    // --- KEYWORD CYCLING ---

    public void NextKeyword()
    {
        if (_matchedBoxes.Count == 0)
        {
            overlay.ClearBoxes();
            SetStatus("No matches");
            return;
        }

        _curMatch = (_curMatch + 1) % _matchedBoxes.Count;

        var box = _matchedBoxes[_curMatch];
        overlay.DrawOneWord(box, _resp.width, _resp.height, RED);
        SetStatus($"Match {_curMatch + 1}/{_matchedBoxes.Count}: \"{box.text}\"");
    }


    // --- UI helpers ---

    void SetStatus(string s)
    {
        if (statusText) statusText.text = s;
        Debug.Log("[Redbox] " + s);
    }

    void SetStepText(string s)
    {
        if (stepText) stepText.text = s;
        Debug.Log("[Redbox] " + s);
    }
}
