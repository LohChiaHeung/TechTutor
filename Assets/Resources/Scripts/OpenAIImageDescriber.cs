using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class OpenAIImageDescriber : MonoBehaviour
{
    [Header("UI: Main")]
    public Button captureButton;
    public TextMeshProUGUI guideText;

    [Header("UI: More Info Panel")]
    public Button moreInfoButton;
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoContent;
    public RawImage infoImage;                   // <- NEW: shows the captured image
    public Button closeButton;

    [Header("UI: History")]
    public Button historyButton;                 // <- NEW: open history
    public GameObject historyPanel;              // <- NEW: parent panel
    public Transform historyContent;             // <- NEW: content of a ScrollView (VerticalLayout)
    public GameObject historyItemPrefab;         // <- NEW: prefab with RawImage + 3 TMPs + “View” button
    public Button closeHistoryButton;

    [Header("UI: Capture Effects")]
    public Image screenFlash;                    // <- NEW: full-screen Image (white), alpha = 0 initially
    public AudioSource shutterAudio;             // <- OPTIONAL: assign a shutter sound
    public TextMeshProUGUI statusText;           // <- NEW: e.g. “Analyzing… Please wait”
    public CanvasGroup workingSpinner;           // <- OPTIONAL: a spinner (alpha hidden initially)

    [Header("Voice / TTS")]
    public TTSManager tts;            // Assign in Inspector
    public Button infoVoiceButton;    // Button in Info Panel to replay voice

    [Header("OpenAI API")]
    [TextArea]
    public string openAIApiKey = "";             // Your GPT-4o API key

    [Header("Capture & Compression")]
    [Range(256, 1920)] public int maxUploadWidth = 960;  // downscale long side to this width
    [Range(20, 95)] public int jpegQuality = 60;         // lower = smaller size

    // Runtime
    private string lastFullReply = "";
    private Texture2D lastPreviewTexture = null;         // what we show in info panel
    private List<HistoryEntry> history = new List<HistoryEntry>();
    private string historyJsonPath;

    [Header("Camera Capture")]
    public Camera captureCamera; // assign your Main Camera in Inspector

    // Response model
    [Serializable] public class OpenAIWrapper { public OpenAIResponse wrapper; }
    [Serializable] public class OpenAIResponse { public Choice[] choices; }
    [Serializable] public class Choice { public AIMessage message; }
    [Serializable] public class AIMessage { public string content; }

    // History
    [Serializable] public class HistoryList { public List<HistoryEntry> items = new List<HistoryEntry>(); }
    [Serializable]
    public class HistoryEntry
    {
        public string summary;   // “This is a keyboard.”
        public string title;     // “Keyboard”
        public string info;      // the paragraph(s)
        public string imagePath; // saved PNG path
        public string timeIso;   // timestamp
        public string summaryTtsPath; // local wav/mp3 for summary voice
        public string infoTtsPath;    // local wav/mp3 for info voice
    }

    private void Awake()
    {
        historyJsonPath = Path.Combine(Application.persistentDataPath, "ai_component_history.json");
        LoadHistoryFromDisk();
        infoPanel.SetActive(false);
        if (historyPanel != null) historyPanel.SetActive(false);
        SetWorking(false, "");
        if (screenFlash) screenFlash.color = new Color(1, 1, 1, 0);
        //history = new List<HistoryEntry>(); // reset in-memory
        LoadHistoryFromDisk();
    }

    private void Start()
    {
        captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSendToOpenAI()));
        moreInfoButton.onClick.AddListener(ShowInfoPanel);
        closeButton.onClick.AddListener(() => {
            StopLocalVoice();
            infoPanel.SetActive(false);
             });

        if (infoVoiceButton != null)
        {
            infoVoiceButton.onClick.RemoveAllListeners(); // ensure single source of truth
            infoVoiceButton.onClick.AddListener(SpeakInfoFull);
        }


        if (historyButton != null)
            historyButton.onClick.AddListener(OpenHistory);

        if (closeHistoryButton != null)
            closeHistoryButton.onClick.AddListener(() => historyPanel.SetActive(false));

        //ClearHistoryAndDeleteFile();
        // Seed demo rows so you can see scrolling
        //AddHistory("This is a keyboard.", "Keyboard", "A keyboard is an input device...", "");
        //AddHistory("This is a mouse.", "Mouse", "A mouse is a pointing device...", "");
    }

    void FitRawImageToBox(RawImage img, Texture tex, Vector2 box) // box = (300, 250)
    {
        if (!img || tex == null) return;

        float texW = tex.width, texH = tex.height;
        float boxW = box.x, boxH = box.y;

        float texAspect = texW / texH;
        float boxAspect = boxW / boxH;

        float outW, outH;
        if (texAspect > boxAspect)
        {
            // too wide: fit width, reduce height
            outW = boxW;
            outH = boxW / texAspect;
        }
        else
        {
            // too tall: fit height, reduce width
            outH = boxH;
            outW = boxH * texAspect;
        }

        var rt = img.rectTransform;
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, outW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, outH);

        img.texture = tex;
    }

    Texture2D CaptureFromCamera(Camera cam, int targetWidth)
    {
        int w = targetWidth;
        int h = Mathf.RoundToInt(targetWidth * (cam.pixelHeight / (float)cam.pixelWidth));

        RenderTexture rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
        var prevRT = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = prevRT;
        rt.Release();
        Destroy(rt);

        return tex;
    }

    public void ClearHistoryAndDeleteFile()
    {
        history.Clear();
        SaveHistoryToDisk();
        var path = System.IO.Path.Combine(Application.persistentDataPath, "ai_component_history.json");
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        Debug.Log("History cleared and file deleted: " + path);
    }
    // ===================== MAIN CAPTURE + SEND =====================
    IEnumerator CaptureAndSendToOpenAI()
    {
        DisableMainButtons(true);
        SetWorking(true, "Analyzing… Please wait…");

        // Small “shutter” feeling
        yield return StartCoroutine(DoCaptureFlash());

        yield return new WaitForEndOfFrame();

        // 1) Capture full screen
        Texture2D full = CaptureFromCamera(captureCamera, maxUploadWidth);


        // 2) Downscale & compress (also generate preview copy)
        Texture2D resized = Downscale(full, maxUploadWidth);
        byte[] jpegBytes = resized.EncodeToJPG(jpegQuality);
        // Keep a preview texture to show in info panel (use the resized one)
        if (lastPreviewTexture) Destroy(lastPreviewTexture);
        lastPreviewTexture = resized; // keep for UI
        Destroy(full);                // free big texture

        // 3) Build request
        string base64Image = Convert.ToBase64String(jpegBytes);
        string prompt =
            "You are helping to build an AR educational tutorial titled 'Identify Computer Components'.\\n" +
            "Your job is to describe what computer component is shown in the image.\\n" +
            "Examples of components include: mouse, keyboard, laptop, monitor, speaker, etc.\\n" +
            "Always reply in this exact format:\\n" +
            "This is a _____.\\n" +
            "[TITLE]: _____\\n" +
            "[INFO]: _____\\n" +
            "Replace the blanks with the name of the component.\\n" +
            "Make sure the INFO is written in a beginner-friendly way using 1 to 2 short paragraphs.\\n" +
            "Do not describe the image (e.g., 'This image shows...'). Instead, provide educational content about the component.\\n" +
            "The TITLE must be the same as the component name.";

        string json =
            "{\"model\": \"gpt-4o\", " +
            "\"messages\": [{" +
                "\"role\": \"user\", " +
                "\"content\": [" +
                    "{\"type\": \"text\", \"text\": \"" + prompt + "\"}," +
                    "{\"type\": \"image_url\", \"image_url\": {\"url\": \"data:image/jpeg;base64," + base64Image + "\"}}" +
                "]" +
            "}] }";

        // 4) Send
        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string reply = ExtractOpenAIResponse(request.downloadHandler.text);
            lastFullReply = reply;

            string summary = ExtractSummaryOnly(reply);
            guideText.text = summary;

            // Extract parts for history & info
            ExtractTitleAndInfo(reply, out string title, out string content);

            // Save preview image to disk so it persists in history
            string savedPath = SavePreviewTextureToDisk(lastPreviewTexture);
            var entry = AddHistory(summary, title, content, savedPath);
            // Do NOT persist here (it plays). Let SpeakSummaryOnce() create+save+play.
            SpeakSummaryOnce();
        }
        else
        {
            guideText.text = "❌ Failed to get response.";
            Debug.LogError("OpenAI API Error: " + request.error + "\n" + request.downloadHandler.text);
        }

        SetWorking(false, "");
        DisableMainButtons(false);
    }

    HistoryEntry GetEntryForLastReply()
    {
        ExtractTitleAndInfo(lastFullReply, out string t, out string c);
        string s = ExtractSummaryOnly(lastFullReply);
        foreach (var e in history)
            if (e.title == t && e.info == c && e.summary == s) return e;
        return history.Count > 0 ? history[0] : null;
    }

    //void SpeakInfoFull()  // plays: "This is a ___." then "<Title>. <Content>"
    //{
    //    if (!tts) return;

    //    var entry = GetEntryForLastReply();
    //    ExtractTitleAndInfo(lastFullReply, out string title, out string content);
    //    string summaryPhrase = ExtractSummaryOnly(lastFullReply);
    //    string infoPhrase = (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(content)) ? $"{title}. {content}" : title;

    //    // Ensure summary file exists (usually already from capture)
    //    if (entry != null && string.IsNullOrEmpty(entry.summaryTtsPath))
    //        PersistVoicesFor(entry, isInfo: false, summaryPhrase);

    //    // Ensure info file exists (generate on demand the first time user asks)
    //    if (entry != null && string.IsNullOrEmpty(entry.infoTtsPath))
    //        PersistVoicesFor(entry, isInfo: true, infoPhrase);  // this will synth+play the FIRST one we request (see below)

    //    // Decide order:
    //    StartCoroutine(PlaySummaryThenInfo(entry, summaryPhrase, infoPhrase));
    //}

    void SpeakInfoFull() // plays "<Title>. <Content>" ONLY (no "This is a …")
    {
        if (!tts) return;

        var entry = GetEntryForLastReply();
        ExtractTitleAndInfo(lastFullReply, out string title, out string content);

        // Compose Title + Content (fallback to title if content empty)
        string infoPhrase = (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(content))
                            ? $"{title}. {content}"
                            : title;

        // Run the single-step sequencer; will reuse local file or synth+save if missing
        StartCoroutine(PlayInfoOnly(entry, infoPhrase));
    }

    IEnumerator PlayInfoOnly(HistoryEntry entry, string infoPhrase)
    {
        // stop any current speech to avoid overlaps
        var src = tts ? tts.GetComponentInChildren<AudioSource>() : null;
        if (src && src.isPlaying) src.Stop();

        // If we already have a file, play it
        if (entry != null && !string.IsNullOrEmpty(entry.infoTtsPath) && File.Exists(entry.infoTtsPath))
        {
            PlayLocalVoice(entry.infoTtsPath);
            yield break;
        }

        // Otherwise synthesize once, capture to .wav, save path into history, then play
        tts.SynthesizeAndPlay(infoPhrase);
        yield return StartCoroutine(CaptureCurrentTTSClipToFile(infoPhrase, p =>
        {
            if (!string.IsNullOrEmpty(p) && entry != null)
            {
                entry.infoTtsPath = p;
                SaveHistoryToDisk();
                Debug.Log("[TTS] Saved info → " + p);
            }
        }));

        // Play the freshly saved file (defensive)
        if (entry != null && !string.IsNullOrEmpty(entry.infoTtsPath) && File.Exists(entry.infoTtsPath))
            PlayLocalVoice(entry.infoTtsPath);
    }


    IEnumerator PlaySummaryThenInfo(HistoryEntry entry, string summaryPhrase, string infoPhrase)
    {
        var src0 = tts ? tts.GetComponentInChildren<AudioSource>() : null;
        if (src0 && src0.isPlaying) src0.Stop(); // prevent overlap if user taps repeatedly

        // Resolve paths (may still be generating; we’ll retry briefly)
        string summaryPath = entry?.summaryTtsPath;
        string infoPath = entry?.infoTtsPath;

        float giveUp = Time.time + 12f;
        // Wait a short while for files to appear if they’re being created right now
        while (Time.time < giveUp && (string.IsNullOrEmpty(summaryPath) || string.IsNullOrEmpty(infoPath)))
        {
            summaryPath = entry?.summaryTtsPath;
            infoPath = entry?.infoTtsPath;
            yield return null;
        }

        // Play summary if available, else synth+capture now
        if (!string.IsNullOrEmpty(summaryPath) && System.IO.File.Exists(summaryPath))
        {
            PlayLocalVoice(summaryPath);
            yield return WaitForCurrentClipToEnd();
        }
        else
        {
            tts.SynthesizeAndPlay(summaryPhrase);
            yield return StartCoroutine(CaptureCurrentTTSClipToFile(summaryPhrase, p => {
                if (!string.IsNullOrEmpty(p) && entry != null) { entry.summaryTtsPath = p; SaveHistoryToDisk(); }
            }));
            yield return WaitForCurrentClipToEnd();
        }

        // Play info if available, else synth+capture now
        if (!string.IsNullOrEmpty(infoPath) && System.IO.File.Exists(infoPath))
        {
            PlayLocalVoice(infoPath);
            yield return WaitForCurrentClipToEnd();
        }
        else
        {
            tts.SynthesizeAndPlay(infoPhrase);
            yield return StartCoroutine(CaptureCurrentTTSClipToFile(infoPhrase, p => {
                if (!string.IsNullOrEmpty(p) && entry != null) { entry.infoTtsPath = p; SaveHistoryToDisk(); }
            }));
            yield return WaitForCurrentClipToEnd();
        }
    }

    IEnumerator WaitForCurrentClipToEnd()
    {
        var src = tts ? tts.GetComponentInChildren<AudioSource>() : null;
        if (!src) yield break;
        // tiny delay to allow Play() to start
        yield return null;
        while (src.isPlaying) yield return null;
    }


    // ===================== INFO PANEL =====================
    void ShowInfoPanel()
    {
        infoPanel.SetActive(true);

        ExtractTitleAndInfo(lastFullReply, out string title, out string content);
        infoTitle.text = string.IsNullOrEmpty(title) ? "Unknown" : title;
        infoContent.text = string.IsNullOrEmpty(content) ? "No information available." : content;

        if (infoImage == null) return;

        if (lastPreviewTexture != null)
        {
            infoImage.texture = lastPreviewTexture;

            // Fit INSIDE 305 x 250, preserving aspect
            const float targetW = 400f;
            const float targetH = 400f;

            float texW = lastPreviewTexture.width;
            float texH = lastPreviewTexture.height;
            float texAspect = texW / texH;
            float boxAspect = targetW / targetH;

            float finalW, finalH;
            if (texAspect > boxAspect)
            {
                // wider than box → match width
                finalW = targetW;
                finalH = targetW / texAspect;
            }
            else
            {
                // taller than box → match height
                finalH = targetH;
                finalW = targetH * texAspect;
            }

            var rt = infoImage.rectTransform;
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, finalW);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, finalH);
        
        }
        else
        {
            infoImage.texture = null;
        }
        SpeakInfoFull();
    }


    // ===================== HISTORY =====================
    void OpenHistory()
    {
        if (historyPanel == null) return;
        if (historyContent == null || historyItemPrefab == null)
        {
            Debug.LogError("History UI not wired: assign historyContent and historyItemPrefab in the Inspector.");
            return;
        }

        historyPanel.SetActive(true);

        // Ensure this modal is on top
        historyPanel.transform.SetAsLastSibling();

        // Clear existing UI items
        foreach (Transform child in historyContent)
            Destroy(child.gameObject);

        // Rebuild list
        foreach (var item in history)
        {
            GameObject go = Instantiate(historyItemPrefab, historyContent);
            var view = go.GetComponent<HistoryItemView>();
            if (view == null) continue;

            // Texts
            view.titleText.text = item.title;
            view.summaryText.text = item.info;
            view.summaryText.maxVisibleLines = 3;

            // time text (robust parse only)
            if (view.timeText != null)
            {
                if (System.DateTime.TryParse(item.timeIso, out var dt))
                    view.timeText.text = dt.ToString("yyyy-MM-dd HH:mm");
                else
                    view.timeText.text = "—";
            }

            // Thumbnail
            if (File.Exists(item.imagePath))
            {
                var pngData = File.ReadAllBytes(item.imagePath);
                Texture2D t = new Texture2D(2, 2);
                t.LoadImage(pngData);
                view.thumbnail.texture = t;
            }

            // “View” button: hide history → show info (on top)
            if (view.viewButton != null)
            {
                view.viewButton.onClick.AddListener(() =>
                {
                    lastFullReply = item.summary + "\n[TITLE]: " + item.title + "\n[INFO]: " + item.info;

                    if (lastPreviewTexture) Destroy(lastPreviewTexture);
                    if (System.IO.File.Exists(item.imagePath))
                    {
                        var img = new Texture2D(2, 2);
                        img.LoadImage(System.IO.File.ReadAllBytes(item.imagePath));
                        lastPreviewTexture = img;
                    }

                    // Hide list so info isn’t covered
                    if (historyPanel) historyPanel.SetActive(false);

                    // Show info on top
                    ShowInfoPanel();
                    if (infoPanel) infoPanel.transform.SetAsLastSibling();

                });
            }

            Canvas.ForceUpdateCanvases();
            var rt = historyContent as RectTransform;
            if (rt) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            var scroll = historyContent.GetComponentInParent<ScrollRect>();
            if (scroll)
            {
                // snap to top (or bottom, your choice)
                scroll.verticalNormalizedPosition = 1f;
                scroll.vertical = true;
            }
        }
    }

    void SpeakSummaryOnce()
    {
        if (!tts) return;
        var phrase = ExtractSummaryOnly(lastFullReply);
            if (string.IsNullOrEmpty(phrase)) return;
           // Look up the most recent entry (top of list)
        var entry = history.Count > 0 ? history[0] : null;
            // Try to play existing file
           if (entry != null && !string.IsNullOrEmpty(entry.summaryTtsPath) && System.IO.File.Exists(entry.summaryTtsPath))
                {
            Debug.Log("[TTS] Reuse summary file: " + System.IO.Path.GetFileName(entry.summaryTtsPath));
            PlayLocalVoice(entry.summaryTtsPath);
               }
           else
                {
            Debug.Log("[TTS] Create summary voice (first time)");
            tts.SynthesizeAndPlay(phrase);
            StartCoroutine(CaptureCurrentTTSClipToFile(phrase, savedPath =>
              {
                           if (!string.IsNullOrEmpty(savedPath) && entry != null)
                               {
                    entry.summaryTtsPath = savedPath;
                    SaveHistoryToDisk();
                    Debug.Log("[TTS] Saved summary → " + savedPath);
                                }
              }));
           }
    }

    // Where we will save files
    string TtsDir => System.IO.Path.Combine(Application.persistentDataPath, "tts");
    string MakeTtsPath(string text)
    {
        if (!System.IO.Directory.Exists(TtsDir)) System.IO.Directory.CreateDirectory(TtsDir);
        // simple stable name
        string safe = System.Text.RegularExpressions.Regex.Replace(text ?? "voice", "[^a-zA-Z0-9]+", "_");
        if (safe.Length > 40) safe = safe.Substring(0, 40);
        return System.IO.Path.Combine(TtsDir, safe + "_" + DateTime.UtcNow.ToString("HHmmssfff") + ".wav");
    }

    /// Persist the *currently playing* TTSManager AudioSource’s clip to disk.
    /// We don’t touch TTSManager; we just read the AudioClip samples and write a WAV.
    IEnumerator CaptureCurrentTTSClipToFile(string sourceText, Action<string> onSaved)
    {
        var src = tts ? tts.GetComponentInChildren<AudioSource>() : null;
        if (!src) { Debug.LogWarning("[TTS] No AudioSource under TTSManager."); onSaved?.Invoke(null); yield break; }

        AudioClip before = src.clip;
        float giveUpAt = Time.time + 20f;
        AudioClip clip = null;

        while (Time.time < giveUpAt)
        {
            if (src.clip != null && src.clip != before)
            {
                clip = src.clip;
                if (clip.loadState == AudioDataLoadState.Loaded || src.isPlaying) break;
            }
            else if (src.isPlaying && src.clip != null)
            {
                clip = src.clip;
                break;
            }
            yield return null;
        }

        if (clip == null) { Debug.LogWarning("[TTS] AudioSource didn’t start in time; cannot capture."); onSaved?.Invoke(null); yield break; }
        yield return null;

        float[] samples = new float[clip.samples * clip.channels];
        try { clip.GetData(samples, 0); }
        catch (Exception e) { Debug.LogWarning("[TTS] GetData failed: " + e.Message); onSaved?.Invoke(null); yield break; }

        string path = MakeTtsPath(sourceText);
        try { WriteWav(path, samples, clip.frequency, clip.channels); Debug.Log("[TTS] Wrote WAV: " + path); onSaved?.Invoke(path); }
        catch (Exception e) { Debug.LogError("[TTS] Write WAV failed: " + e.Message); onSaved?.Invoke(null); }
    }


    /// Simple WAV writer (PCM 16-bit)
    void WriteWav(string path, float[] samples, int sampleRate, int channels)
    {
        using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create, System.IO.FileAccess.Write))
        using (var bw = new System.IO.BinaryWriter(fs))
        {
            int sampleCount = samples.Length;
            int byteRate = sampleRate * channels * 2; // 16-bit
            int subchunk2Size = sampleCount * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + subchunk2Size);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16); // PCM
            bw.Write((short)1); // AudioFormat PCM
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)(channels * 2));
            bw.Write((short)16); // bits per sample
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(subchunk2Size);

            // samples float [-1,1] -> int16
            for (int i = 0; i < sampleCount; i++)
            {
                short s = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), short.MinValue, short.MaxValue);
                bw.Write(s);
            }
        }
    }

    void PersistVoicesFor(HistoryEntry entry, bool isInfo, string phrase)
    {
        if (!tts || string.IsNullOrWhiteSpace(phrase)) return;

        var existing = isInfo ? entry.infoTtsPath : entry.summaryTtsPath;
        if (!string.IsNullOrEmpty(existing) && File.Exists(existing))
        {
            Debug.Log("[TTS] Reuse existing file: " + Path.GetFileName(existing));
            return;
        }

        Debug.Log("[TTS] Create " + (isInfo ? "info" : "summary") + " voice (first time)");
        tts.SynthesizeAndPlay(phrase);   // use your current TTS pipeline (no TTSManager changes)

        StartCoroutine(CaptureCurrentTTSClipToFile(phrase, savedPath =>
        {
            if (!string.IsNullOrEmpty(savedPath))
            {
                if (isInfo) entry.infoTtsPath = savedPath;
                else entry.summaryTtsPath = savedPath;

                SaveHistoryToDisk();
                Debug.Log("[TTS] Saved " + (isInfo ? "info" : "summary") + " → " + savedPath);
            }
        }));
    }




    /// Play local voice file via our own temporary AudioSource (or reuse TTS’s source)
    void PlayLocalVoice(string path)
    {
        StartCoroutine(PlayLocalVoice_Co(path));
    }
    IEnumerator PlayLocalVoice_Co(string path)
    {
        if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
        {
            Debug.LogWarning("[TTS] Local file missing: " + path);
            yield break;
        }
        using (var req = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.WAV))
        {
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning("[TTS] Load local clip failed: " + req.error);
                yield break;
            }
            var clip = DownloadHandlerAudioClip.GetContent(req);
            var src = tts ? tts.GetComponentInChildren<AudioSource>() : null;
            if (!src) src = gameObject.AddComponent<AudioSource>();
            src.Stop();
            src.loop = false;
            src.clip = clip;
            src.Play();
            Debug.Log($"[TTS] Playing local: {System.IO.Path.GetFileName(path)}  dur≈{clip.length:0.0}s");
        }
    }
    void StopLocalVoice()
    {
        var src = tts ? tts.GetComponentInChildren<AudioSource>() : null;
        if (src && src.isPlaying)
        {
            src.Stop();
            Debug.Log("[TTS] Stopped.");
        }
    }

    public void SpeakCurrentInfo()
{
        SpeakInfoFull();
    }

    HistoryEntry AddHistory(string summary, string title, string info, string imagePath)
    {
        var entry = new HistoryEntry
        {
            summary = summary,
            title = title,
            info = info,
            imagePath = imagePath,
            timeIso = DateTime.UtcNow.ToString("o")
        };
        history.Insert(0, entry); // latest first
        SaveHistoryToDisk();
        return entry;
    }

    void SaveHistoryToDisk()
    {
        var wrap = new HistoryList { items = history };
        string json = JsonUtility.ToJson(wrap, prettyPrint: true);
        File.WriteAllText(historyJsonPath, json);
    }

    void LoadHistoryFromDisk()
    {
        if (!File.Exists(historyJsonPath)) return;
        string json = File.ReadAllText(historyJsonPath);
        var wrap = JsonUtility.FromJson<HistoryList>(json);
        if (wrap != null && wrap.items != null) history = wrap.items;
    }

    string SavePreviewTextureToDisk(Texture2D tex)
    {
        try
        {
            if (tex == null) return "";
            string name = "cap_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff") + ".png";
            string path = Path.Combine(Application.persistentDataPath, name);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            return path;
        }
        catch (Exception e)
        {
            Debug.LogWarning("Failed saving preview image: " + e.Message);
            return "";
        }
    }

    // ===================== HELPERS =====================
    string ExtractOpenAIResponse(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<OpenAIWrapper>("{\"wrapper\":" + json + "}");
            return wrapper.wrapper.choices[0].message.content;
        }
        catch
        {
            return "(⚠️ Could not parse GPT-4o reply)";
        }
    }

    string ExtractSummaryOnly(string fullReply)
    {
        if (string.IsNullOrEmpty(fullReply)) return "No object detected.";
        string[] lines = fullReply.Split('\n');
        return lines.Length > 0 ? lines[0] : fullReply;
    }

    void ExtractTitleAndInfo(string reply, out string title, out string content)
    {
        title = "Unknown";
        content = "No information available.";
        if (string.IsNullOrEmpty(reply)) return;

        string[] lines = reply.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("[TITLE]:"))
                title = line.Replace("[TITLE]:", "").Trim();
            else if (line.StartsWith("[INFO]:"))
                content = line.Replace("[INFO]:", "").Trim();
        }
    }

    Texture2D Downscale(Texture2D src, int maxW)
    {
        int w = src.width;
        int h = src.height;
        if (w <= maxW) return Duplicate(src); // already small enough

        float scale = (float)maxW / w;
        int newW = maxW;
        int newH = Mathf.RoundToInt(h * scale);

        RenderTexture rt = RenderTexture.GetTemporary(newW, newH, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D dst = new Texture2D(newW, newH, TextureFormat.RGB24, false);
        dst.ReadPixels(new Rect(0, 0, newW, newH), 0, 0);
        dst.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }

    Texture2D Duplicate(Texture2D src)
    {
        Texture2D dup = new Texture2D(src.width, src.height, src.format, false);
        dup.SetPixels(src.GetPixels());
        dup.Apply();
        return dup;
    }

    IEnumerator DoCaptureFlash()
    {
        if (shutterAudio) shutterAudio.Play();

        if (screenFlash == null) yield break;

        // fade in
        float t = 0f;
        while (t < 0.08f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0f, 0.6f, t / 0.08f);
            screenFlash.color = new Color(1, 1, 1, a);
            yield return null;
        }
        // fade out
        t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(0.6f, 0f, t / 0.2f);
            screenFlash.color = new Color(1, 1, 1, a);
            yield return null;
        }
        screenFlash.color = new Color(1, 1, 1, 0);
    }

    void SetWorking(bool isWorking, string message)
    {
        if (statusText) statusText.text = message;
        if (workingSpinner)
        {
            workingSpinner.alpha = isWorking ? 1f : 0f;
            workingSpinner.blocksRaycasts = isWorking;
            workingSpinner.interactable = isWorking;
        }
    }

    void DisableMainButtons(bool disabled)
    {
        if (captureButton) captureButton.interactable = !disabled;
        if (moreInfoButton) moreInfoButton.interactable = !disabled;
        if (historyButton) historyButton.interactable = !disabled;
    }
}
