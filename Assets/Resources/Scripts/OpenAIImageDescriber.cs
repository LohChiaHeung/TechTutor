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
        closeButton.onClick.AddListener(() => infoPanel.SetActive(false));

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
            AddHistory(content, title, content, savedPath);

            // Optional: auto-open info panel after success
            // ShowInfoPanel();
        }
        else
        {
            guideText.text = "❌ Failed to get response.";
            Debug.LogError("OpenAI API Error: " + request.error + "\n" + request.downloadHandler.text);
        }

        SetWorking(false, "");
        DisableMainButtons(false);
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
            const float targetW = 305f;
            const float targetH = 250f;

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
            view.summaryText.text = item.summary;
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
        }
    }


    void AddHistory(string summary, string title, string info, string imagePath)
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
