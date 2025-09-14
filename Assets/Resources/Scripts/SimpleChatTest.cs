using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.IO;
using UnityEngine.SceneManagement;
using static UnityEngine.XR.ARSubsystems.XRCpuImage;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using Unity.VisualScripting.Antlr3.Runtime;
using NativeGalleryNamespace;
using NativeCameraNamespace;
using System;
using System.Linq;
using GuideModel = global::AIGuide;

public class TechTutorAskUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TMP_InputField userInput;
    public TMP_Text responseText;
    public ScrollRect chatScrollRect;
    public ScrollRect inputScroll;
    public Button arModeButton;
    public Button captureImageButton;

    public Button uploadImageButton;
    private Texture2D selectedImage = null;

    public RawImage previewImage;
    public GameObject previewImageObject; // Parent container for image + remove button
    public Button removeImageButton;

    public GameObject fullScreenImagePanel;
    public RawImage fullScreenImage;
    public Button closeFullScreenButton;

    private GuideModel lastGuide;

    [Header("OpenAI API")]
    [TextArea(5, 10)]
    public string openAIKey = "sk-...";
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    private string lastBotReply = "";

    [Header("Dev / Auto-AR")]
    public bool autoStartAROnPlay = true;              // toggle to auto-start AR
    [TextArea(3, 10)]
    public string devFixedReply =
    "Step 1: Open the folder\n" +
    "→ Click the 'Documents' folder in the window\n\n" +
    "Step 2: Type with the keyboard\n" +
    "→ Enter your file name in the name box\n\n" +
    "Step 3: Press Enter\n" +
    "→ Confirm to proceed";

    // OPTIONAL: auto-load a test image from Resources for preview
    public bool loadTestImageFromResources = true;

    // ✅ History tracking
    private List<HistoryEntry> historyList = new();
    private const string HistoryKey = "tutor_history";

    [Serializable]
    public class ARGuideCache
    {
        public string imagePath;  // absolute path to saved PNG
        public string guideJson;  // serialized AIGuide
    }

    string GetARCacheFilePath()
    {
        return System.IO.Path.Combine(Application.persistentDataPath, "ar_guide_cache.json");
    }

    void SaveARGuideCache(GuideModel guide, string imagePath)
    {
        if (guide == null || guide.steps == null || guide.steps.Length == 0) return;
        if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath)) return;

        try
        {
            var cache = new ARGuideCache
            {
                imagePath = imagePath,
                guideJson = JsonUtility.ToJson(guide)
            };
            string json = JsonUtility.ToJson(cache);
            System.IO.File.WriteAllText(GetARCacheFilePath(), json);
            Debug.Log("[ARCache] Saved cache: " + GetARCacheFilePath());
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ARCache] Save failed: " + e.Message);
        }
    }

    bool LoadARGuideCache(out GuideModel guide, out string imagePath)
    {
        guide = null;
        imagePath = null;
        try
        {
            string file = GetARCacheFilePath();
            if (!System.IO.File.Exists(file)) return false;

            string json = System.IO.File.ReadAllText(file);
            var cache = JsonUtility.FromJson<ARGuideCache>(json);
            if (cache == null || string.IsNullOrEmpty(cache.guideJson) || string.IsNullOrEmpty(cache.imagePath))
                return false;

            if (!System.IO.File.Exists(cache.imagePath)) return false;

            guide = JsonUtility.FromJson<AIGuide>(cache.guideJson);
            imagePath = cache.imagePath;
            return (guide != null && guide.steps != null && guide.steps.Length > 0);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[ARCache] Load failed: " + e.Message);
            return false;
        }
    }

    void Start()
    {
        LoadHistory();


        // Auto-load last reply into memory
        if (PlayerPrefs.HasKey("last_reply"))
        {
            lastBotReply = PlayerPrefs.GetString("last_reply");

            // Enable AR Mode button immediately
            arModeButton.gameObject.SetActive(true);

            Debug.Log("Loaded lastBotReply from PlayerPrefs:");
            Debug.Log(lastBotReply);
        }

    
        uploadImageButton.onClick.AddListener(OpenImagePicker);
        captureImageButton.onClick.AddListener(CaptureImageFromCamera);

        //ChatHistoryStore.Instance.ClearAll();

        if (ChatHistoryStore.Instance != null && ChatHistoryStore.Instance.Data.items.Count == 0)
        {
            ChatHistoryStore.Instance.Add("What is a GPU?", "A graphics processing unit used for parallel graphics/math tasks.");
            ChatHistoryStore.Instance.Add("How to take a screenshot?", "Press Windows+Shift+S, select the area, release to copy.");
        }

        // --- DEV: quick image for testing (optional) ---
        if (loadTestImageFromResources)
        {
            var tex = Resources.Load<Texture2D>("AR_AI_Tutorial"); // Assets/Resources/AR_AI_Tutorial.jpg
            if (tex != null)
            {
                selectedImage = tex;
                if (previewImage) previewImage.texture = tex;
                if (previewImageObject) previewImageObject.SetActive(true);
            }
        }

        // --- DEV: auto start AR mode on play ---
        if (autoStartAROnPlay)
        {
            // force-enable AR button path and feed a fixed reply so ConvertToTutorialSpec has steps
            lastBotReply = string.IsNullOrWhiteSpace(lastBotReply) ? devFixedReply : lastBotReply;
            arModeButton.gameObject.SetActive(true);

            // wait one frame so UI/holders are ready, then jump
            StartCoroutine(_AutoStartARNextFrame());
        }

        IEnumerator _AutoStartARNextFrame()
        {
            yield return null; // let the scene finish awake/start wiring
            OnARModeClicked();
        }



    }

    string SaveTextureToPng(Texture2D tex, string fileNameNoExt)
    {
        try
        {
            if (tex == null) return null;
            var bytes = tex.EncodeToPNG();
            string dir = Path.Combine(Application.persistentDataPath, "ChatThumbs");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, fileNameNoExt + ".png");
            File.WriteAllBytes(path, bytes);
            return path;
        }
        catch { return null; }
    }
    public void OnSendClicked()
    {
        string trimmedInput = userInput.text.Trim();

        if (!string.IsNullOrEmpty(trimmedInput))
        {
            string userMessage = trimmedInput;
            userInput.text = "";
            StartCoroutine(SendMessageToOpenAI(userMessage));
        }
        else
        {
            responseText.text += "\n\n[!] Please enter your question.";
            Debug.LogWarning("User tried to send an empty message.");
        }
    }
    string SanitizeForTMP(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return s.Replace("✅", "[OK]")
                .Replace("❌", "[X]")
                .Replace("⚠️", "[!]");
    }

    // Remove disclaimers you don't want to show in chat
    string StripDisclaimers(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var lines = s.Split('\n');
        System.Text.StringBuilder b = new System.Text.StringBuilder(s.Length);
        foreach (var line in lines)
        {
            var l = line.Trim();
            if (l.StartsWith("I'm unable to view", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (l.StartsWith("I’m unable to view", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (l.StartsWith("I cannot view", System.StringComparison.OrdinalIgnoreCase)) continue;
            b.AppendLine(line);
        }
        return b.ToString().TrimEnd();
    }

    //    string systemPrompt =
    //"You are TechTutor, an AI assistant that explains computer tasks step-by-step.\n\n" +
    //"Always respond in this STRICT plain-text format ONLY (no markdown, no headings, no code fences):\n" +
    //"Step 1: <short action>\n" +
    //"→ <very brief explanation>\n\n" +
    //"Step 2: <short action>\n" +
    //"→ <very brief explanation>\n\n" +
    //"- Include 4–6 steps if needed\n" +
    //"- Use simple words\n" +
    //"- Do NOT add greetings, summaries, tips, or anything else\n" +
    //"- Do NOT use ### or any markdown\n" +
    //"- Do NOT wrap your answer in JSON or code fences\n";
    IEnumerator SendMessageToOpenAI(string userMessage)
    {
        string json = "";
        string base64Image = "";
        bool includeImage = selectedImage != null;

        string systemPrompt =
"You are TechTutor, an AI assistant that explains computer tasks step-by-step.\n\n" +
"Always respond in this STRICT plain-text format ONLY (no markdown, no code fences):\n" +
"Step 1: <short action>\n" +
"→ <very brief explanation>\n" +
"Keywords: <1-3 exact UI tokens from the image, comma-separated>\n\n" +
"Step 2: <short action>\n" +
"→ <very brief explanation>\n" +
"Keywords: <1-3 exact UI tokens from the image, comma-separated>\n\n" +
"- Include 4–6 steps if needed\n" +
"- Use simple words\n" +
"- Keywords MUST be short, verbatim labels visible in the IMAGE (menu text, button labels, tab names, shortcuts like Ctrl+C)\n" +
"- Do NOT invent tokens; if none are visible for a step, write: Keywords: (none)\n" +
"- Do NOT add greetings, summaries, tips, or anything else\n" +
"- Do NOT use ### or any markdown\n" +
"- Do NOT wrap your answer in JSON or code fences\n";
       

        string model = includeImage ? "gpt-4o" : "gpt-4o-mini";

        if (includeImage)
        {
            base64Image = EncodeImageToBase64(selectedImage);
            json = @"{
          ""model"": """ + model + @""",
          ""temperature"": 0.2,
          ""messages"": [
            { ""role"": ""system"", ""content"": """ + EscapeJson(systemPrompt) + @""" },
            {
              ""role"": ""user"",
              ""content"": [
                { ""type"": ""text"", ""text"": """ + EscapeJson(userMessage) + @""" },
                { ""type"": ""image_url"", ""image_url"": { ""url"": ""data:image/jpeg;base64," + base64Image + @""" } }
              ]
            }
          ],
          ""max_tokens"": 900
        }";
        }
        else
        {
            json = @"{
          ""model"": """ + model + @""",
          ""temperature"": 0.2,
          ""messages"": [
            { ""role"": ""system"", ""content"": """ + EscapeJson(systemPrompt) + @""" },
            { ""role"": ""user"", ""content"": """ + EscapeJson(userMessage) + @""" }
          ],
          ""max_tokens"": 900
        }";
        }

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIKey);

        // Show the user message immediately
        responseText.text += $"\n\nYou: {userMessage}\n\nTechTutor:\n";
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            string reply = ExtractReply(jsonResponse);

            // 🧹 sanitize before displaying
            reply = StripDisclaimers(reply);
            reply = SanitizeForTMP(reply);

            lastBotReply = reply;

            // still build guide so AR can run
            lastGuide = BuildGuideFromPlainSteps(reply);

            string displayReply = StripKeywordLines(reply);
            StartCoroutine(TypeText(responseText.text, displayReply));

            if (lastGuide?.steps != null)
            {
                for (int si = 0; si < lastGuide.steps.Length; si++)
                {
                    var s = lastGuide.steps[si];
                    var kw = (s.keywords == null || s.keywords.Length == 0) ? "(none)" : string.Join(", ", s.keywords);
                    Debug.Log($"[Guide/OpenAI] Step {si + 1} \"{s.title}\": keywords = {kw}");
                }
            }

            // Type out ONLY the assistant reply (since we already added "You:" above)
            // StartCoroutine(TypeText(responseText.text, reply));

            // Save to history
            string imagePathOrNull = SaveQuestionImageIfAny();
            ChatHistoryStore.Instance.Add(userMessage, displayReply, imagePathOrNull);

            // Save AR cache for offline AR testing
            // Save guide + image for AR Mode reuse (no more tokens)
            if (lastGuide != null && lastGuide.steps != null && lastGuide.steps.Length > 0)
            {
                ARCache.SaveGuide(lastGuide);
                if (selectedImage != null)
                {
                    ARCache.SavePngOrJpg(selectedImage);
                }
                //PlayerPrefs.SetString("last_question", userMessage);
                //PlayerPrefs.SetString("last_reply", reply);   // RAW with Keywords for AR parsing
                //PlayerPrefs.Save();
                else
                {
                    // if you store the path elsewhere, copy it to ARCache.ImagePath here (optional)
                }
            }


            PlayerPrefs.SetString("last_question", userMessage);
            PlayerPrefs.SetString("last_reply", reply);
            PlayerPrefs.Save();
        }
        else
        {
            responseText.text += "Error: " + request.error;
            Debug.LogError("OpenAI Error: " + request.downloadHandler.text);
        }

        Canvas.ForceUpdateCanvases();
        RebuildAndScrollToBottom();
    }

    void EnsureGuideRunContext()
    {
        if (GuideRunContext.I == null)
        {
            var go = new GameObject("GuideRunContext");
            go.AddComponent<GuideRunContext>();   // has DontDestroyOnLoad() inside
        }
    }

    // Turn the plain "Step 1 / → ..." text into an AIGuide for AR_ImageTest
    //GuideModel BuildGuideFromPlainSteps(string text)
    //{
    //    if (string.IsNullOrWhiteSpace(text)) return null;

    //    var lines = text.Replace("\r", "").Split('\n');
    //    var steps = new List<AIGuideStep>();
    //    AIGuideStep current = null;

    //    foreach (var raw in lines)
    //    {
    //        var line = raw.Trim();
    //        if (line.StartsWith("Step "))
    //        {
    //            if (current != null)
    //            {
    //                // fill keywords + action_type before pushing
    //                current.keywords = ExtractKeywordsFromStep(current.title, current.instruction);
    //                current.action_type = InferActionType(current.title, current.instruction);
    //                steps.Add(current);
    //            }

    //            current = new StepItem
    //            {
    //                title = line,                 // e.g., "Step 1: Click Start"
    //                instruction = "",
    //                keywords = Array.Empty<string>(),
    //                alts = Array.Empty<string>(),
    //                action_type = "",
    //                notes = ""
    //            };
    //        }
    //        else if (line.StartsWith("→"))
    //        {
    //            if (current != null)
    //            {
    //                current.instruction = line.Length > 1 ? line.Substring(1).Trim() : "";
    //            }
    //        }
    //    }

    //    if (current != null)
    //    {
    //        current.keywords = ExtractKeywordsFromStep(current.title, current.instruction);
    //        current.action_type = InferActionType(current.title, current.instruction);
    //        steps.Add(current);
    //    }

    //    if (steps.Count == 0) return null;
    //    return new GuideModel { steps = steps.ToArray() };
    //}

    AIGuide BuildGuideFromPlainSteps(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        var lines = text.Replace("\r", "").Split('\n');
        var steps = new List<AIGuideStep>();
        AIGuideStep current = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            if (line.StartsWith("Step ", StringComparison.OrdinalIgnoreCase))
            {
                // flush previous step
                if (current != null)
                {
                    if (current.keywords == null || current.keywords.Length == 0)
                        current.keywords = ExtractKeywordsFromStep(current.title, current.instruction); // fallback
                    steps.Add(current);
                }

                current = new AIGuideStep
                {
                    title = line,
                    instruction = "",
                    keywords = Array.Empty<string>()
                };

                // next line may be explanation (→ …)
                if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("→"))
                {
                    current.instruction = lines[i + 1].Trim().TrimStart('→', ' ').Trim();
                    i++;
                }

                // optional: next line may be Keywords: …
                if (i + 1 < lines.Length && lines[i + 1].TrimStart().StartsWith("Keywords:", StringComparison.OrdinalIgnoreCase))
                {
                    string raw = lines[i + 1].Substring(lines[i + 1].IndexOf(':') + 1).Trim();
                    if (!string.IsNullOrEmpty(raw) && !raw.Equals("(none)", StringComparison.OrdinalIgnoreCase))
                    {
                        current.keywords = raw.Split(',')
                                               .Select(s => s.Trim())
                                               .Where(s => s.Length > 0)
                                               .ToArray();
                    }
                    i++;
                }
            }
        }

        // flush last step
        if (current != null)
        {
            if (current.keywords == null || current.keywords.Length == 0)
                current.keywords = ExtractKeywordsFromStep(current.title, current.instruction); // fallback
            steps.Add(current);
        }

        if (steps.Count == 0) return null;
        return new AIGuide { steps = steps.ToArray() };
    }





    static readonly HashSet<string> Stop = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "the","a","an","to","and","or","of","in","on","at","for","with","by","from","into","your",
    "this","that","it","is","are","as","be","then","now","you","me","my","we","i",
    "button","icon","menu","panel","window","app","application","screen","page","tab",
    "file","folder","document","documents","text","box","field","name","type","click"
};

    static readonly string[] VerbPatterns = new[]
    {
    "click","press","select","choose","open","tap","type","enter","go to","navigate to",
    "search for","hit","use","pick","enable","disable","check","uncheck","switch","expand","collapse"
};

    string StripKeywordLines(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var lines = s.Replace("\r", "").Split('\n');
        var kept = new System.Text.StringBuilder(s.Length);

        foreach (var raw in lines)
        {
            var line = raw.TrimStart();
            // hide both "Keyword:" and "Keywords:", any casing
            if (line.StartsWith("Keywords:", StringComparison.OrdinalIgnoreCase)) continue;
            if (line.StartsWith("Keyword:", StringComparison.OrdinalIgnoreCase)) continue;

            kept.AppendLine(raw);
        }
        return kept.ToString().TrimEnd();
    }

    string[] ExtractKeywordsFromStep(string title, string instruction)
    {
        var found = new List<string>();

        // 1) quoted phrases "..." or '...'
        ExtractQuotedPhrases(title, found);
        ExtractQuotedPhrases(instruction, found);

        // 2) menu paths like File > New > Project
        ExtractMenuPaths(title, found);
        ExtractMenuPaths(instruction, found);

        // 3) phrases after key verbs (Click X, Open Settings, Type "hello")
        ExtractAfterVerbs(title, found);
        ExtractAfterVerbs(instruction, found);

        // 4) Title-cased or UI-looking tokens (Start, Settings, English, Ctrl+C)
        ExtractUiLookingTokens(title, found);
        ExtractUiLookingTokens(instruction, found);

        // 5) cleanup: trim, dedup, filter stopwords, short tokens, punctuation
        var cleaned = new List<string>();
        foreach (var s in found)
        {
            var t = s.Trim();
            if (t.Length < 2) continue;
            if (Stop.Contains(t)) continue;

            // remove trailing punctuation
            t = TrimPunct(t);
            if (t.Length < 2) continue;
            if (!cleaned.Contains(t, StringComparer.OrdinalIgnoreCase))
                cleaned.Add(t);
        }

        // keep it small (OCR works better with focused set)
        const int MAX = 6;
        if (cleaned.Count > MAX) cleaned = cleaned.GetRange(0, MAX);

        return cleaned.ToArray();
    }

    void ExtractQuotedPhrases(string src, List<string> outList)
    {
        if (string.IsNullOrEmpty(src)) return;
        // matches "English Training" or 'English Training'
        var m1 = System.Text.RegularExpressions.Regex.Matches(src, "\"([^\"]+)\"");
        foreach (System.Text.RegularExpressions.Match m in m1) outList.Add(m.Groups[1].Value);

        var m2 = System.Text.RegularExpressions.Regex.Matches(src, "'([^']+)'");
        foreach (System.Text.RegularExpressions.Match m in m2) outList.Add(m.Groups[1].Value);
    }

    void ExtractMenuPaths(string src, List<string> outList)
    {
        if (string.IsNullOrEmpty(src)) return;
        // split by > or →
        var parts = src.Split(new[] { '>', '→' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in parts)
        {
            var t = p.Trim();
            if (t.Length >= 2) outList.Add(t);
        }
    }

    void ExtractAfterVerbs(string src, List<string> outList)
    {
        if (string.IsNullOrEmpty(src)) return;

        // Build verb regex once
        string verbs = string.Join("|", VerbPatterns.Select(v => System.Text.RegularExpressions.Regex.Escape(v)));
        var rx = new System.Text.RegularExpressions.Regex(@"\b(" + verbs + @")\b\s+([^\.\,\n\r\(\)]{1,60})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (System.Text.RegularExpressions.Match m in rx.Matches(src))
        {
            var phrase = m.Groups[2].Value.Trim();
            // stop on "the window"/generic endings
            phrase = phrase.TrimEnd('.', ',', ')', '(', ':', ';');
            if (!string.IsNullOrEmpty(phrase)) outList.Add(phrase);
        }
    }

    void ExtractUiLookingTokens(string src, List<string> outList)
    {
        if (string.IsNullOrEmpty(src)) return;
        // tokens likely to appear as on-screen text: TitleCase words, ALLCAPS, has digits or +/-/&/ /
        var rx = new System.Text.RegularExpressions.Regex(@"\b([A-Z][a-zA-Z0-9\+\-_/]{1,30}|[A-Z0-9\+\-_/]{2,30})\b");
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(src))
        {
            var t = m.Value.Trim();
            if (!string.IsNullOrEmpty(t)) outList.Add(t);
        }

        // also split on colon "Step 1: Open Settings" -> "Open Settings"
        int colon = src.IndexOf(':');
        if (colon >= 0 && colon + 1 < src.Length)
        {
            var after = src.Substring(colon + 1).Trim();
            if (after.Length > 1) outList.Add(after);
        }
    }

    string TrimPunct(string s)
    {
        int i = 0, j = s.Length - 1;
        while (i <= j && char.IsPunctuation(s[i])) i++;
        while (j >= i && char.IsPunctuation(s[j])) j--;
        if (j >= i) return s.Substring(i, j - i + 1);
        return "";
    }

    string InferActionType(string title, string instruction)
    {
        string src = (title + " " + instruction).ToLowerInvariant();
        if (src.Contains("click") || src.Contains("tap")) return "click";
        if (src.Contains("open")) return "open";
        if (src.Contains("type") || src.Contains("enter")) return "type";
        if (src.Contains("select") || src.Contains("choose") || src.Contains("pick")) return "select";
        if (src.Contains("menu") || src.Contains("go to") || src.Contains("navigate")) return "menu";
        if (src.Contains("scroll")) return "scroll";
        if (src.Contains("observe") || src.Contains("check the")) return "observe";
        return "";
    }


    string SanitizeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        // Strip code fences if present
        if (s.StartsWith("```"))
        {
            int start = s.IndexOf('{');
            int end = s.LastIndexOf('}');
            if (start >= 0 && end > start) s = s.Substring(start, end - start + 1);
        }

        // Keep only the first {...} block
        int i0 = s.IndexOf('{');
        int i1 = s.LastIndexOf('}');
        if (i0 >= 0 && i1 > i0) s = s.Substring(i0, i1 - i0 + 1);

        return s.Trim();
    }

    // Saves selectedImage (Texture2D) to disk and returns its absolute path, or null
    string SaveQuestionImageIfAny()
    {
        if (selectedImage == null) return null;

        try
        {
            // Ensure folder exists
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "chat_images");
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            // Unique filename
            string file = System.IO.Path.Combine(dir, $"qa_{System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png");

            // 🔧 Make a readable copy so EncodeToPNG never fails
            int w = selectedImage.width;
            int h = selectedImage.height;

            RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(selectedImage, rt);
            RenderTexture.active = rt;

            Texture2D readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            readable.Apply();

            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            byte[] bytes = readable.EncodeToPNG();
            UnityEngine.Object.Destroy(readable); // cleanup

            // Save to file
            System.IO.File.WriteAllBytes(file, bytes);

            return file;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Chat] Failed to save image: {e.Message}");
            return null;
        }
    }

    string EscapeJson(string input)
    {
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }

    //string EncodeImageToBase64(Texture2D sourceTexture, int targetSize = 512)
    //{
    //    if (sourceTexture == null) return "";

    //    // Scale to target size if needed
    //    int width = sourceTexture.width;
    //    int height = sourceTexture.height;

    //    float scale = 1f;
    //    if (Mathf.Max(width, height) > targetSize)
    //        scale = targetSize / (float)Mathf.Max(width, height);

    //    int newWidth = Mathf.RoundToInt(width * scale);
    //    int newHeight = Mathf.RoundToInt(height * scale);

    //    // Create a temporary RenderTexture and blit
    //    RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
    //    var prev = RenderTexture.active;
    //    Graphics.Blit(sourceTexture, rt);
    //    RenderTexture.active = rt;

    //    // Read into a new readable Texture2D
    //    Texture2D readableTex = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
    //    readableTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
    //    readableTex.Apply();

    //    RenderTexture.active = prev;
    //    RenderTexture.ReleaseTemporary(rt);

    //    // Encode to JPG
    //    byte[] imageBytes = readableTex.EncodeToJPG(60); // 60% quality
    //    UnityEngine.Object.Destroy(readableTex);         // cleanup

    //    return System.Convert.ToBase64String(imageBytes);
    //}
    string EncodeImageToBase64(Texture2D sourceTexture, int targetSize = 1024) // was 512
    {
        if (sourceTexture == null) return "";

        int width = sourceTexture.width, height = sourceTexture.height;
        float scale = 1f;
        int maxDim = Mathf.Max(width, height);
        if (maxDim > targetSize) scale = targetSize / (float)maxDim;

        int newWidth = Mathf.RoundToInt(width * scale);
        int newHeight = Mathf.RoundToInt(height * scale);

        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        var prev = RenderTexture.active;
        Graphics.Blit(sourceTexture, rt);
        RenderTexture.active = rt;

        var readableTex = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        readableTex.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        readableTex.Apply();

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        byte[] imageBytes = readableTex.EncodeToJPG(85); // was 60
        UnityEngine.Object.Destroy(readableTex);

        return Convert.ToBase64String(imageBytes);
    }


    //IEnumerator TypeText(string baseText, string newText, float delay = 0.02f)
    //{
    //    string current = baseText;

    //    for (int i = 0; i < newText.Length; i++)
    //    {
    //        current += newText[i];
    //        responseText.text = current;

    //        Canvas.ForceUpdateCanvases();
    //       //chatScrollRect.verticalNormalizedPosition = 0f;

    //        yield return new WaitForSeconds(delay);
    //    }
    //}
    IEnumerator TypeText(string baseText, string newText, float delay = 0.02f)
    {
        string current = baseText;
        bool autoScroll = IsNearBottom();

        for (int i = 0; i < newText.Length; i++)
        {
            current += newText[i];
            responseText.text = current;

            if (autoScroll) RebuildAndScrollToBottom();
            yield return new WaitForSeconds(delay);
        }
    }

    public void ShowFullScreenImage()
    {
        if (selectedImage != null)
        {
            fullScreenImage.texture = selectedImage;
            fullScreenImagePanel.SetActive(true);
        }
    }

    public void CloseFullScreenImage()
    {
        fullScreenImagePanel.SetActive(false);
    }


    void ShowPreview(Texture2D tex)
    {
        selectedImage = tex;
        previewImage.texture = tex;
        previewImageObject.SetActive(true);
    }

    string ExtractReply(string json)
    {
        try
        {
            OpenAIResponse parsed = JsonUtility.FromJson<OpenAIWrapper>("{\"wrapper\":" + json + "}").wrapper;
            return parsed.choices[0].message.content;
        }
        catch
        {
            return "(Could not parse reply)";
        }
    }

    void SaveImageToStorage(Texture2D image, string fileName = "saved_image.png")
    {
        if (image == null)
        {
            Debug.LogWarning("❌ No image to save.");
            return;
        }

        // Convert Texture2D to PNG byte array
        byte[] pngData = image.EncodeToPNG();
        if (pngData == null)
        {
            Debug.LogWarning("❌ Failed to encode image to PNG.");
            return;
        }

        // Path to save the image
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        // Save to disk
        System.IO.File.WriteAllBytes(filePath, pngData);
        Debug.Log("✅ Image saved to: " + filePath);
    }

    public void CancelSelectedImage()
    {
        selectedImage = null;
        previewImage.texture = null;
        previewImage.gameObject.SetActive(false);
        Debug.Log("🗑️ Image selection canceled.");
    }

    //    void CaptureImageFromCamera()
    //    {
    //#if UNITY_ANDROID || UNITY_IOS
    //        NativeCamera.TakePicture((path) =>
    //        {
    //            if (!string.IsNullOrEmpty(path))
    //            {
    //                Texture2D texture = NativeCamera.LoadImageAtPath(path, 512, false);
    //                if (texture != null)
    //                {
    //                    ShowPreview(texture); // 👈 Show in UI

    //                    // Save to persistent storage
    //                    string savedPath = System.IO.Path.Combine(Application.persistentDataPath, "last_image.png");
    //                    System.IO.File.WriteAllBytes(savedPath, texture.EncodeToPNG());
    //                    Debug.Log("✅ Image captured and saved: " + savedPath);
    //                }
    //                else
    //                {
    //                    Debug.LogWarning("⚠️ Failed to load captured image.");
    //                }
    //            }
    //            else
    //            {
    //                Debug.LogWarning("❌ Camera capture canceled.");
    //            }
    //        }, 2048);
    //#else
    //    Debug.LogWarning("⚠️ Camera capture only supported on Android/iOS.");
    //#endif
    //    }

    void CaptureImageFromCamera()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeCamera.TakePicture((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("❌ Camera capture canceled.");
                return;
            }

            Texture2D texture = NativeCamera.LoadImageAtPath(path, 2048, false);
            if (texture != null)
            {
                selectedImage = texture;

                // ✅ Show image preview
                previewImage.texture = selectedImage;
                previewImageObject.SetActive(true);
                previewImage.gameObject.SetActive(true);

                // ✅ Save to persistent path
                string savedPath = System.IO.Path.Combine(Application.persistentDataPath, "last_image.png");
                System.IO.File.WriteAllBytes(savedPath, selectedImage.EncodeToPNG());
                Debug.Log("✅ Image captured & saved: " + savedPath);

                PlayerPrefs.SetString("last_image_path", savedPath);
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogWarning("⚠️ Failed to load captured image.");
            }
        },
        maxSize: 2048,
        preferredCamera: NativeCamera.PreferredCamera.Rear);
#else
    Debug.LogWarning("⚠️ Camera capture only supported on Android/iOS.");
#endif
    }

    //public void OnARModeClicked()
    //{
    //    // Prefer current session data
    //    Texture2D shot = selectedImage;
    //    AIGuide guide = lastGuide;

    //    // Fallback to cached if missing
    //    if (guide == null || guide.steps == null || guide.steps.Length == 0 || shot == null)
    //    {
    //        if (LoadARGuideCache(out var cachedGuide, out var cachedImagePath))
    //        {
    //            // load the cached PNG into Texture2D
    //            try
    //            {
    //                byte[] bytes = System.IO.File.ReadAllBytes(cachedImagePath);
    //                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    //                tex.LoadImage(bytes);
    //                shot = tex;
    //                guide = cachedGuide;
    //                Debug.Log("[ARCache] Using cached AR guide and image.");
    //            }
    //            catch (Exception e)
    //            {
    //                Debug.LogWarning("[ARCache] Could not load cached image: " + e.Message);
    //            }
    //        }
    //        else
    //        {
    //            Debug.LogWarning("[ARCache] No cache available.");
    //        }
    //    }

    //    if (guide == null || guide.steps == null || guide.steps.Length == 0 || shot == null)
    //    {
    //        // show a simple message in your UI instead of emojis
    //        responseText.text += "\n\n[AR] No steps/image available. Ask a question first.";
    //        return;
    //    }

    //    GuideRunContext.I.screenshot = shot;
    //    GuideRunContext.I.guide = guide;
    //    if (TutorialSpecHolder.I != null)
    //    {
    //        // build from the last plain-text reply you already saved
    //        var spec = ConvertToTutorialSpec(lastBotReply);
    //        TutorialSpecHolder.I.spec = spec;
    //    }

    //    if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
    //    {
    //        try
    //        {
    //            var png = GuideRunContext.I.screenshot.EncodeToPNG();
    //            var path = Path.Combine(Application.persistentDataPath, "last_screenshot.png");
    //            File.WriteAllBytes(path, png);
    //            PlayerPrefs.SetString("last_image_path", path);
    //            PlayerPrefs.Save();
    //            Debug.Log("[AR] Saved last image to: " + path);
    //        }
    //        catch (System.Exception e)
    //        {
    //            Debug.LogWarning("[AR] Failed to save last image: " + e.Message);
    //        }
    //    }
    //    //SceneManager.LoadScene("AR_ImageTest");
    //    SceneManager.LoadScene("AR_PanelTutorial");
    //    //SceneManager.LoadScene("AR_ImageBoardScene");
    //}
    //public void OnARModeClicked()
    //{
    //    // 1) make sure the singleton exists
    //    EnsureGuideRunContext();

    //    // 2) prefer current session (fallback to cache if needed)
    //    Texture2D shot = selectedImage;
    //    AIGuide guide = lastGuide;
    //    if ((shot == null || guide == null || guide.steps == null || guide.steps.Length == 0)
    //        && LoadARGuideCache(out var cachedGuide, out var cachedImagePath))
    //    {
    //        try
    //        {
    //            byte[] bytes = System.IO.File.ReadAllBytes(cachedImagePath);
    //            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
    //            tex.LoadImage(bytes);
    //            shot = tex;
    //            guide = cachedGuide;
    //        }
    //        catch { /* ignore */ }
    //    }

    //    if (shot == null) { Debug.LogWarning("[AR] No image available."); return; }
    //    if (guide == null || guide.steps == null || guide.steps.Length == 0)
    //    {
    //        // Optional: build a simple guide from last plain-text reply
    //        guide = BuildGuideFromPlainSteps(lastBotReply);
    //    }

    //    // 3) stash into the cross-scene context
    //    GuideRunContext.I.screenshot = shot;
    //    GuideRunContext.I.guide = guide;

    //    // 4) go to your new AR scene that uses AR_RedboxRunner
    //    UnityEngine.SceneManagement.SceneManager.LoadScene("TT2_OcrDemo_Test");
    //}

    public void OnARModeClicked()
    {
        // Ensure singleton exists
        if (GuideRunContext.I == null)
        {
            var go = new GameObject("GuideRunContext");
            go.AddComponent<GuideRunContext>();
        }

        // Prefer the saved (cached) guide + image to avoid extra API calls
        GuideModel g;
        Texture2D tex;
        if (ARCache.LoadGuide(out g) && ARCache.LoadImage(out tex))
        {
            GuideRunContext.I.guide = g;
            GuideRunContext.I.screenshot = tex;
            Debug.Log("[AR] Loaded cached guide + image (no API)");
        }
        else
        {
            // Fallback to current session in case cache is missing
            GuideRunContext.I.guide = lastGuide;
            GuideRunContext.I.screenshot = selectedImage;
            Debug.Log("[AR] Using current session guide + image");
        }

        // Load your red-box OCR scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("TT2_OcrDemo_Test");
    }




    //public void OnARModeClicked()
    //{
    //    Debug.Log("✅ AR Mode button clicked");

    //    if (GuideRunContext.I == null)
    //    {
    //        Debug.LogError("GuideRunContext not found in scene!");
    //        return;
    //    }
    //    if (selectedImage == null)
    //    {
    //        Debug.LogError("❌ No uploaded/captured image. Please add one before AR Mode.");
    //        return;
    //    }
    //    if (lastGuide == null || lastGuide.steps == null || lastGuide.steps.Length == 0)
    //    {
    //        Debug.LogError("❌ No parsed AI steps. Ask a question and wait for JSON steps first.");
    //        return;
    //    }

    //    GuideRunContext.I.screenshot = selectedImage;
    //    GuideRunContext.I.guide = lastGuide;

    //    UnityEngine.SceneManagement.SceneManager.LoadScene("AR_ImageTest");
    //}



    //    void OpenImagePicker()
    //    {
    //#if UNITY_ANDROID || UNITY_IOS
    //        NativeGallery.GetImageFromGallery((path) =>
    //        {
    //            if (!string.IsNullOrEmpty(path))
    //            {
    //                Texture2D texture = NativeGallery.LoadImageAtPath(path, 512, false);
    //                if (texture != null)
    //                {
    //                    ShowPreview(texture); // 👈 Show in UI

    //                    // Save to persistent storage
    //                    string savedPath = System.IO.Path.Combine(Application.persistentDataPath, "last_image.png");
    //                    System.IO.File.WriteAllBytes(savedPath, texture.EncodeToPNG());
    //                    Debug.Log("✅ Image selected and saved: " + savedPath);
    //                }
    //                else
    //                {
    //                    Debug.LogWarning("⚠️ Failed to load image.");
    //                }
    //            }
    //            else
    //            {
    //                Debug.LogWarning("❌ No image selected.");
    //            }
    //        }, "Select an image for TechTutor to analyze.");
    //#else
    //    Debug.LogWarning("⚠️ Image picker only supported on Android/iOS.");
    //#endif
    //    }

    void OpenImagePicker()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery((path) =>
        {
            if (path != null)
            {
                Texture2D texture = NativeGallery.LoadImageAtPath(path, 2048, false);
                if (texture != null)
                {
                    selectedImage = texture;

                    // ✅ Show image preview
                    previewImage.texture = selectedImage;
                    previewImage.gameObject.SetActive(true);
                    previewImageObject.SetActive(true);

                    // ✅ Save to persistent path
                    string savedPath = System.IO.Path.Combine(Application.persistentDataPath, "last_image.png");
                    System.IO.File.WriteAllBytes(savedPath, selectedImage.EncodeToPNG());
                    Debug.Log("✅ Image selected & saved: " + savedPath);

                    PlayerPrefs.SetString("last_image_path", savedPath);
                    PlayerPrefs.Save();
                    Debug.Log("✅ Image selected & saved: " + savedPath);
                }
                else
                {
                    Debug.LogWarning("⚠️ Failed to load image.");
                }
            }
            else
            {
                Debug.LogWarning("❌ No image selected.");
            }
        }, "Select an image for TechTutor to analyze.");
#else
    Debug.LogWarning("⚠️ Image picker only supported on Android/iOS.");
#endif
    }

    bool IsNearBottom(float threshold = 0.1f)
    {
        if (!chatScrollRect || !chatScrollRect.content) return true;
        return chatScrollRect.verticalNormalizedPosition <= threshold; // 0 = bottom
    }

    void RebuildAndScrollToBottom()
    {
        var content = chatScrollRect.content;
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f; // bottom
        Canvas.ForceUpdateCanvases();
    }

    public void OnRemoveImageClicked()
    {
        selectedImage = null;
        previewImage.texture = null;
        previewImageObject.SetActive(false);
    }

    void Update()
    {
        if (userInput.isFocused && Application.isMobilePlatform)
        {
            Canvas.ForceUpdateCanvases();
            inputScroll.verticalNormalizedPosition = 0f;
        }
    }

    void SaveHistory()
    {
        HistoryWrapper wrapper = new HistoryWrapper { entries = historyList };
        string json = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(HistoryKey, json);
        PlayerPrefs.Save();
    }

    void LoadHistory()
    {
        if (PlayerPrefs.HasKey(HistoryKey))
        {
            string json = PlayerPrefs.GetString(HistoryKey);
            HistoryWrapper wrapper = JsonUtility.FromJson<HistoryWrapper>(json);
            if (wrapper != null && wrapper.entries != null)
            {
                historyList = wrapper.entries;
            }
        }
    }

    TutorialSpec ConvertToTutorialSpec(string botReply)
    {
        string[] lines = botReply.Split('\n');
        List<TutorialStep> steps = new();
        TutorialStep currentStep = null;
        int stepNum = 1;

        foreach (string line in lines)
        {
            string trimmed = line.Trim();

            if (trimmed.StartsWith("Step "))
            {
                if (currentStep != null)
                    steps.Add(currentStep);

                currentStep = new TutorialStep
                {
                    id = stepNum++,
                    title = trimmed,
                    description = "",
                    target = ExtractTarget(trimmed)
                };
            }
            else if (trimmed.StartsWith("→") && currentStep != null)
            {
                currentStep.description = trimmed;
            }
        }

        if (currentStep != null)
            steps.Add(currentStep);

        return new TutorialSpec
        {
            title = "AI Tutorial",
            steps = steps
        };
    }

    string ExtractTarget(string text)
    {
        text = text.ToLower();
        if (text.Contains("keyboard")) return "keyboard";
        if (text.Contains("mouse")) return "mouse";
        if (text.Contains("monitor")) return "monitor";
        if (text.Contains("laptop")) return "laptop";
        return null;
    }



    // ========== Models ==========

    [System.Serializable]
    public class HistoryEntry
    {
        public string reply;
    }

    [System.Serializable]
    public class HistoryWrapper
    {
        public List<HistoryEntry> entries = new List<HistoryEntry>();
    }

    [System.Serializable]
    public class OpenAIWrapper
    {
        public OpenAIResponse wrapper;
    }

    [System.Serializable]
    public class OpenAIResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class TutorialSpec
    {
        public string title;
        public List<TutorialStep> steps;
    }

    [System.Serializable]
    public class TutorialStep
    {
        public int id;
        public string title;       // e.g., Step 1: Click Start Menu
        public string description; // e.g., → This opens the list of programs
        public string target;      // e.g., "keyboard" or null
    }

}
