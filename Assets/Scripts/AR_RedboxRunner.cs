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
    // TTS will be muted until OCR completes once.
    private bool _ttsReadyToSpeak = false;

    [Header("Run")]
    public bool runOnStart = false;
    public float confMin = 0f;
    private bool _ocrRanOnce = false;   // guard: run OCR only once per scene


    [Header("Display Mode")]
    public bool showAllMatchesForStep = true; // ← draw all by default
    [SerializeField] private RectTransform imageFrame; // the 1269×641 area

    [SerializeField] private AudioSource audioSource;

    [ContextMenu("Dump TTS dir")]
    void DebugDumpAudioDir()
    {
        var dir = PlayerPrefs.GetString("tts_audio_dir", null);
        if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
        {
            Debug.Log("[TTS] No dir to dump");
            return;
        }
        var files = Directory.GetFiles(dir).OrderBy(p => p).ToArray();
        Debug.Log("[TTS] Dir: " + dir + "\n" + string.Join("\n", files.Select(p => {
            long len = 0; try { len = new FileInfo(p).Length; } catch { }
            return $"{Path.GetFileName(p)} ({len} bytes)";
        })));
    }



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
    private int _lastSpokenStep = -1;
    private Coroutine _audioCo;
    private int _speakVersion = 0;


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

    //private void SpeakCurrentStep()
    //{
    //    if (_guide?.steps == null || _guide.steps.Length == 0) return;

    //    int stepIndex0Based = Mathf.Clamp(_curStep, 0, _guide.steps.Length - 1);
    //    int stepIndex1Based = stepIndex0Based + 1;          // file names are 1-based

    //    // De-dup: only skip if we *just* played this exact 0-based step
    //    if (_lastSpokenStep == stepIndex0Based)
    //    {
    //        Debug.Log($"[TTS] Already played step {stepIndex1Based}, skipping");
    //        return;
    //    }

    //    // Stop any currently playing voice
    //    if (audioSource != null && audioSource.isPlaying)
    //    {
    //        Debug.Log("[TTS] Stopping current audio");
    //        audioSource.Stop();
    //    }

    //    // Cancel any in-flight load for an older step
    //    if (_audioCo != null)
    //    {
    //        Debug.Log("[TTS] Canceling previous audio load");
    //        StopCoroutine(_audioCo);
    //        _audioCo = null;
    //    }

    //    int version = ++_speakVersion; // guards against stale loads finishing late
    //    Debug.Log($"[TTS] Starting audio for step {stepIndex1Based} (v{version})");

    //    PlayStepAudio(stepIndex1Based, version, stepIndex0Based); // pass both indices
    //}

    //private void PlayStepAudio(int stepIndex1Based, int versionToken, int stepIndex0Based)
    //{
    //    string dir = PlayerPrefs.GetString("tts_audio_dir", null);
    //    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
    //    {
    //        Debug.LogError($"[TTS] Audio directory invalid: {dir}");
    //        return;
    //    }

    //    // Try both “step_02.mp3” and “step_2.mp3”, and WAV as fallback
    //    string d2 = $"step_{stepIndex1Based:D2}";
    //    string d1 = $"step_{stepIndex1Based}";
    //    string[] candidates =
    //    {
    //    Path.Combine(dir, d2 + ".mp3"),
    //    Path.Combine(dir, d2 + ".wav"),
    //    Path.Combine(dir, d1 + ".mp3"),
    //    Path.Combine(dir, d1 + ".wav"),
    //};

    //    Debug.Log($"[TTS] Looking for step {stepIndex1Based} files in {dir}");
    //    foreach (var c in candidates)
    //        Debug.Log($"[TTS]   {Path.GetFileName(c)}: {(File.Exists(c) ? "EXISTS" : "NOT FOUND")}");

    //    string path = candidates.FirstOrDefault(File.Exists);
    //    if (path == null)
    //    {
    //        Debug.LogError($"[TTS] No audio file found for step {stepIndex1Based} in {dir}");
    //        return;
    //    }

    //    // One active loader only
    //    if (_audioCo != null) { StopCoroutine(_audioCo); _audioCo = null; }
    //    _audioCo = StartCoroutine(Co_PlayLocalClip_Fixed(path, stepIndex1Based, versionToken, stepIndex0Based));
    //}





    //void PlayStepAudio(int stepIndex1Based)
    //{
    //    string dir = PlayerPrefs.GetString("tts_audio_dir", null);
    //    if (string.IsNullOrEmpty(dir)) { Debug.Log("[TTS] No audio dir"); return; }

    //    string path = Path.Combine(dir, $"step_{stepIndex1Based:D2}.wav");
    //    if (!File.Exists(path)) { Debug.Log("[TTS] Not found: " + path); return; }

    //    StartCoroutine(Co_PlayLocalClip(path));
    //}

    IEnumerator Co_PlayLocalWav(string path)
    {
        using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning("[TTS] Load failed: " + req.error);
                yield break;
            }
            var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }

    void ApplyTextureAndFit(Texture2D tex)
    {
        if (!tex || overlay == null || overlay.displayImage == null || imageFrame == null) return;

        // Crisp text
        tex.filterMode = FilterMode.Point;  // or Bilinear
        tex.wrapMode = TextureWrapMode.Clamp;

        var img = overlay.displayImage;        // RawImage (parent of Overlay)
        var imgRT = img.rectTransform;
        var frame = imageFrame.rect.size;        // e.g., 1269 × 641

        // Uniform scale so the image fits inside frame (no stretch)
        float iw = tex.width, ih = tex.height;
        float s = Mathf.Min(frame.x / iw, frame.y / ih) * 0.95f; // 0.95f leaves a little margin
        Vector2 disp = new Vector2(iw * s, ih * s);

        // Place RawImage as a child of ImageFrame, left-aligned, vertically centered
        imgRT.SetParent(imageFrame, worldPositionStays: false);
        imgRT.anchorMin = imgRT.anchorMax = new Vector2(0f, 0.5f); // left-middle
        imgRT.pivot = new Vector2(0f, 0.5f);
        imgRT.sizeDelta = disp;
        imgRT.anchoredPosition = Vector2.zero;          // hugs left edge of the frame

        // Make Overlay exactly match the RawImage rect
        var ov = overlay.overlayRoot;                   // child of RawImage
        ov.anchorMin = Vector2.zero; ov.anchorMax = Vector2.one;
        ov.offsetMin = Vector2.zero; ov.offsetMax = Vector2.zero;
        ov.pivot = new Vector2(0f, 0f);            // bottom-left pivot (matches common box math)

        // Assign texture last
        img.texture = tex;

        Debug.Log($"[Fit] frame={frame}, img={iw}x{ih}, display={disp}");
    }
    // Fit the image in its parent without distortion and mirror the overlay rect.
    void FitImageWithoutStretch(Texture2D tex, RawImage img, RectTransform overlayRoot)
    {
        if (!tex || img == null || overlayRoot == null) return;

        var imgRT = img.rectTransform;
        var parent = imgRT.parent as RectTransform;
        var frame = parent.rect.size;          // available area (your purple panel)

        float iw = tex.width, ih = tex.height;
        float s = Mathf.Min(frame.x / iw, frame.y / ih); // uniform scale (fit)
        Vector2 size = new Vector2(iw * s, ih * s);

        // center in parent (anchors & pivot should be middle-center for both)
        imgRT.anchorMin = imgRT.anchorMax = new Vector2(0.5f, 0.5f);
        imgRT.pivot = new Vector2(0.5f, 0.5f);
        imgRT.sizeDelta = size;
        imgRT.anchoredPosition = Vector2.zero;

        overlayRoot.anchorMin = overlayRoot.anchorMax = new Vector2(0.5f, 0.5f);
        overlayRoot.pivot = new Vector2(0.5f, 0.5f);
        overlayRoot.sizeDelta = size;
        overlayRoot.anchoredPosition = Vector2.zero;

        // crisp sampling
        tex.filterMode = FilterMode.Point; // or Bilinear
        tex.wrapMode = TextureWrapMode.Clamp;

        Debug.Log($"[Fit] frame={frame}, img={iw}x{ih}, display={size}");
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

    void FitImageLeft(Texture2D tex, RawImage img, RectTransform overlayRoot, float leftPadding = 16f)
    {
        if (!tex || img == null || overlayRoot == null) return;

        var imgRT = img.rectTransform;
        var parent = imgRT.parent as RectTransform;
        var frame = parent.rect.size;                 // the available area (your left display panel)

        float iw = tex.width, ih = tex.height;
        float s = Mathf.Min(frame.x / iw, frame.y / ih);   // uniform scale (no stretch)
        Vector2 size = new Vector2(iw * s, ih * s);

        // RawImage: left-middle anchored & pivot, sized to keep aspect, placed at leftPadding
        imgRT.anchorMin = imgRT.anchorMax = new Vector2(0f, 0.5f);
        imgRT.pivot = new Vector2(0f, 0.5f);
        imgRT.sizeDelta = size;
        imgRT.anchoredPosition = new Vector2(leftPadding, 0f);

        // Overlay root: exact same transform so red boxes match perfectly
        overlayRoot.anchorMin = overlayRoot.anchorMax = new Vector2(0f, 0.5f);
        overlayRoot.pivot = new Vector2(0f, 0.5f);
        overlayRoot.sizeDelta = size;
        overlayRoot.anchoredPosition = new Vector2(leftPadding, 0f);

        // Crisp sampling for text UIs
        tex.filterMode = FilterMode.Point; // or Bilinear if you prefer slight smoothing
        tex.wrapMode = TextureWrapMode.Clamp;

        Debug.Log($"[FitLeft] frame={frame}, img={iw}x{ih}, display={size}");
    }

    void FitImageIntoFrameLeft(Texture2D tex, float fill = 0.90f, float leftPad = 0f, float topPad = 0f, float rightPad = 0f, float bottomPad = 0f)
    {
        if (!tex || overlay == null || overlay.displayImage == null || imageFrame == null) return;

        // Crisp text
        tex.filterMode = FilterMode.Point;      // or Bilinear
        tex.wrapMode = TextureWrapMode.Clamp;

        var img = overlay.displayImage;                   // RawImage
        var imgRT = img.rectTransform;
        var ovRT = overlay.overlayRoot;                    // Overlay (child of RawImage)
        var frame = imageFrame.rect.size;                   // e.g., 1269×641

        // Usable size inside the frame after padding
        float usableW = Mathf.Max(0f, frame.x - leftPad - rightPad);
        float usableH = Mathf.Max(0f, frame.y - topPad - bottomPad);

        // Uniform scale (no distortion)
        float s = Mathf.Min(usableW / tex.width, usableH / tex.height) * fill;
        Vector2 disp = new Vector2(tex.width * s, tex.height * s);

        // Place RawImage inside ImageFrame: left-aligned, vertically centered
        imgRT.SetParent(imageFrame, worldPositionStays: false);                         
        imgRT.anchorMin = imgRT.anchorMax = new Vector2(0f, 0.5f); // left-middle
        imgRT.pivot = new Vector2(0f, 0.5f);
        imgRT.sizeDelta = disp;
        imgRT.anchoredPosition = new Vector2(leftPad, (topPad - bottomPad) * 0.5f);

        // Make Overlay exactly match RawImage’s rect
        ovRT.SetParent(imgRT, worldPositionStays: false);
        ovRT.anchorMin = Vector2.zero; ovRT.anchorMax = Vector2.one;
        ovRT.offsetMin = Vector2.zero; ovRT.offsetMax = Vector2.zero;
        ovRT.pivot = new Vector2(0f, 0f);

        // Assign texture last
        img.texture = tex;

        Debug.Log($"[Fit] frame={frame}, usable=({usableW},{usableH}), display={disp}");
    }
    IEnumerator Co_RunOcrOnce()
    {
        if (_ocrRanOnce) yield break;
        _ocrRanOnce = true;

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
        //ApplyTextureAndFit(_tex);
        FitImageIntoFrameLeft(_tex, fill: 0.90f, leftPad: 12f, topPad: 40f, rightPad: 12f, bottomPad: 12f);


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

        //_ttsReadyToSpeak = true;
        //_lastSpokenStep = -1;       // reset dedupe on fresh OCR
        //SpeakCurrentStep();
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

    //public void NextStep()
    //{
    //    //if (!HasSteps()) { SetStepText("No steps"); return; }
    //    //_curStep = Mathf.Min(_curStep + 1, _guide.steps.Length - 1);
    //    //ApplyStepKeywords();
    //    //SpeakCurrentStep();
    //    if (!HasSteps()) { SetStepText("No steps"); return; }
    //    int before = _curStep;
    //    _curStep = Mathf.Min(_curStep + 1, _guide.steps.Length - 1);
    //    ApplyStepKeywords();
    //    Debug.Log($"[Nav] Next: {before} -> {_curStep}  ready={_ttsReadyToSpeak}");
    //    SpeakCurrentStep();
    //}


    //void PlayStepAudio(int stepIndex1Based)
    //{
    //    // Backwards compatibility: route to versioned using current token
    //    PlayStepAudio(stepIndex1Based, _speakVersion);
    //}

    //void PlayStepAudio(int stepIndex1Based, int versionToken)
    //{
    //    string dir = PlayerPrefs.GetString("tts_audio_dir", null);
    //    if (string.IsNullOrEmpty(dir)) { Debug.Log("[TTS] No audio dir"); return; }

    //    // Try both 2-digit and 1-digit patterns, mp3 first then wav
    //    string d2 = $"step_{stepIndex1Based:D2}";
    //    string d1 = $"step_{stepIndex1Based}";
    //    var candidates = new List<string> {
    //    Path.Combine(dir, d2 + ".mp3"),
    //    Path.Combine(dir, d2 + ".wav"),
    //    Path.Combine(dir, d1 + ".mp3"),
    //    Path.Combine(dir, d1 + ".wav"),
    //};

    //    string path = candidates.FirstOrDefault(File.Exists);
    //    if (path == null)
    //    {
    //        Debug.Log($"[TTS] Not found: {string.Join(" | ", candidates.Select(Path.GetFileName))}");
    //        return;
    //    }

    //    long len = 0; try { len = new FileInfo(path).Length; } catch { }
    //    Debug.Log($"[TTS] Playing step {stepIndex1Based} -> {Path.GetFileName(path)} ({len} bytes) v{versionToken}");

    //    // Cancel any previous load before starting a new one
    //    if (_audioCo != null) { StopCoroutine(_audioCo); _audioCo = null; }

    //    _audioCo = StartCoroutine(Co_PlayLocalClip_Versioned(path, stepIndex1Based, versionToken));
    //}

    private System.Collections.IEnumerator Co_PlayLocalClip_Fixed(string path, int stepIndex1Based, int versionToken, int stepIndex0Based)
    {
        Debug.Log($"[TTS] Loading audio: step1Based={stepIndex1Based}, step0Based={stepIndex0Based}, v{versionToken}");

        bool isMp3 = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
        var audioType = isMp3 ? AudioType.MPEG : AudioType.WAV;

        using (var req = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + path, audioType))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
        if (req.isHttpError || req.isNetworkError)
#endif
            {
                Debug.LogError($"[TTS] Load failed: {req.error}");
                yield break;
            }

            // Guard against race: if user already moved to another step, ignore this clip.
            if (versionToken != _speakVersion)
            {
                Debug.Log($"[TTS] Stale request ignored (was v{versionToken}, current v{_speakVersion})");
                yield break;
            }

            var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogError("[TTS] Clip is null");
                yield break;
            }

            if (audioSource == null)
                audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D
            audioSource.volume = 1f;

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            // **Important**: remember the spoken step using 0-based index
            _lastSpokenStep = stepIndex0Based;

            Debug.Log($"[TTS] SUCCESS: Playing step {stepIndex1Based}, set _lastSpokenStep={_lastSpokenStep}");
        }

        _audioCo = null;
    }


    public void NextStep()
    {
        if (!HasSteps()) { SetStepText("No steps"); return; }

        int beforeStep = _curStep;
        _curStep = Mathf.Min(_curStep + 1, _guide.steps.Length - 1);

        Debug.Log($"[NAV] NextStep: {beforeStep} -> {_curStep} (guide has {_guide.steps.Length} steps)");

        ApplyStepKeywords();
        //SpeakCurrentStep();
    }

    public void PrevStep()
    {
        if (!HasSteps()) { SetStepText("No steps"); return; }

        int beforeStep = _curStep;
        _curStep = Mathf.Max(_curStep - 1, 0);

        Debug.Log($"[NAV] PrevStep: {beforeStep} -> {_curStep} (guide has {_guide.steps.Length} steps)");

        ApplyStepKeywords();
        //SpeakCurrentStep();
    }
    IEnumerator Co_PlayLocalClip(string path, int stepIndex1Based)
    {
        if (!LooksLikeAudioFile(path))
        {
            Debug.LogWarning("[TTS] File is not recognizable audio: " + path);
            yield break;
        }

        string url = "file://" + path;
        AudioType type = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.WAV;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isHttpError || req.isNetworkError)
#endif
            {
                Debug.LogWarning("[TTS] Load failed: " + req.error + " (" + path + ")");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogWarning("[TTS] Loaded but clip null: " + path);
                yield break;
            }

            if (audioSource == null) audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            // ✅ mark success now (0-based)
            _lastSpokenStep = stepIndex1Based - 1;
            Debug.Log($"[TTS] Marked spoken step {_lastSpokenStep + 1}");
        }
    }



    IEnumerator Co_PlayLocalClip(string path)
    {
        if (!LooksLikeAudioFile(path))
        {
            Debug.LogWarning("[TTS] File is not recognizable audio: " + path);
            yield break;
        }

        string url = "file://" + path;
        AudioType type = path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.WAV;

        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_3_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isHttpError || req.isNetworkError)
#endif
            {
                Debug.LogWarning("[TTS] Load failed: " + req.error + " (" + path + ")");
                yield break;
            }

            var clip = DownloadHandlerAudioClip.GetContent(req);
            if (clip == null)
            {
                Debug.LogWarning("[TTS] Loaded but clip null: " + path);
                yield break;
            }

            if (audioSource == null) audioSource = gameObject.GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D

            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
    bool LooksLikeAudioFile(string path)
    {
        try
        {
            var head = File.ReadAllBytes(path);
            if (head.Length < 12) return false;

            // WAV: "RIFF....WAVE"
            if (head[0] == 'R' && head[1] == 'I' && head[2] == 'F' && head[3] == 'F' &&
                head[8] == 'W' && head[9] == 'A' && head[10] == 'V' && head[11] == 'E') return true;

            // MP3: ID3 or frame sync
            if ((head[0] == 'I' && head[1] == 'D' && head[2] == '3') ||
                (head[0] == 0xFF && (head[1] == 0xFB || head[1] == 0xF3 || head[1] == 0xF2))) return true;

            // JSON/HTML starts with '{' or '<'
            return false;
        }
        catch { return false; }
    }

    // tiny header check: RIFF/WAVE or ID3/MP3 or 0xFF 0xFB (mp3 frame)


    //public void PrevStep()
    //{
    //    //if (!HasSteps()) { SetStepText("No steps"); return; }
    //    //_curStep = Mathf.Max(_curStep - 1, 0);
    //    //ApplyStepKeywords();
    //    //SpeakCurrentStep();
    //     if (!HasSteps()) { SetStepText("No steps"); return; }
    //int before = _curStep;
    //_curStep = Mathf.Max(_curStep - 1, 0);
    //ApplyStepKeywords();
    //Debug.Log($"[Nav] Prev: {before} -> {_curStep}  ready={_ttsReadyToSpeak}");
    //SpeakCurrentStep();
    //}

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
            //PlayStepAudio(_curStep + 1);
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

    // Add this context menu item to reset audio state if things get confused
    [ContextMenu("Reset Audio State")]
    void ResetAudioState()
    {
        Debug.Log("[RESET] Resetting audio state");

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("[RESET] Stopped audio");
        }

        if (_audioCo != null)
        {
            StopCoroutine(_audioCo);
            _audioCo = null;
            Debug.Log("[RESET] Canceled audio coroutine");
        }

        _lastSpokenStep = -1; // Reset to invalid value
        _speakVersion++; // Invalidate any pending loads

        Debug.Log($"[RESET] Reset complete: _lastSpokenStep={_lastSpokenStep}, _speakVersion={_speakVersion}");
    }

    // Add debug context menu to show current state
    [ContextMenu("Debug Audio State")]
    void DebugAudioState()
    {
        Debug.Log("=== AUDIO STATE DEBUG ===");
        Debug.Log($"_curStep: {_curStep}");
        Debug.Log($"_lastSpokenStep: {_lastSpokenStep}");
        Debug.Log($"_speakVersion: {_speakVersion}");
        Debug.Log($"_ttsReadyToSpeak: {_ttsReadyToSpeak}");
        Debug.Log($"audioSource: {(audioSource != null ? "exists" : "null")}");
        if (audioSource != null)
        {
            Debug.Log($"audioSource.isPlaying: {audioSource.isPlaying}");
            Debug.Log($"audioSource.clip: {(audioSource.clip != null ? audioSource.clip.name : "null")}");
        }
        Debug.Log($"_audioCo: {(_audioCo != null ? "running" : "null")}");
        Debug.Log($"Guide steps: {(_guide?.steps?.Length ?? 0)}");
        Debug.Log("========================");
    }
}
