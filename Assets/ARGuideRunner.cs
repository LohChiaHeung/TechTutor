////using System.Collections;
////using System.Collections.Generic;
////using System.Linq;
////using UnityEngine;
////using UnityEngine.UI;
////using TMPro;

////public class ARGuideRunner : MonoBehaviour
////{
////    [Header("Scene refs (world-space)")]
////    public RectTransform boardOverlayRT;   // <-- replaces screenshotRT
////    public RectTransform calloutLayer;     // same as before (on ARGuideCanvas)
////    public OcrOverlay overlay;             // same component as 2D
////    public CalloutOverlay callout;         // same component as 2D
////    public EasyOcrClient ocrClient;        // same OCR client

////    [Header("Options")]
////    [Range(0.6f, 1f)] public float matchThreshold = 0.82f;
////    public float marginAbove = 0.06f;      // meters in world, but we place via canvas so keep similar visual gap
////    public float pad = 8f;                 // px inside canvas
////    public float maxPanelWidthCap = 300f;  // px

////    [Header("Nav (optional)")]
////    public Button prevButton;
////    public Button nextButton;
////    public TextMeshProUGUI titleTextTop;   // optional: show current step title somewhere

////    // runtime
////    private AIGuide guide;
////    private OcrResponse ocr;
////    private int index = -1;
////    private RectTransform currentPanel;
////    private Texture2D screenshot;

////    // Call once after the board is placed and texture is set
////    public void Run(Texture2D tex, AIGuide g)
////    {
////        screenshot = tex;
////        guide = g;

////        // early disable nav
////        UpdateNavButtons();

////        if (prevButton) prevButton.onClick.AddListener(PrevStep);
////        if (nextButton) nextButton.onClick.AddListener(NextStep);

////        if (screenshot == null || guide == null || guide.steps == null || guide.steps.Length == 0)
////        {
////            Debug.LogError("[ARGuideRunner] Missing screenshot/guide/steps.");
////            return;
////        }

////        StartCoroutine(ocrClient.Run(screenshot, resp =>
////        {
////            ocr = resp;
////            Debug.Log($"[ARGuideRunner] OCR ready: {resp.words.Length} words");
////            ShowStep(0);
////            UpdateNavButtons();
////        },
////        err => Debug.LogError(err)));
////    }

////    public void NextStep()
////    {
////        if (guide == null || guide.steps == null || guide.steps.Length == 0) return;
////        if (index >= guide.steps.Length - 1) return;
////        ShowStep(index + 1);
////    }

////    public void PrevStep()
////    {
////        if (guide == null || guide.steps == null || guide.steps.Length == 0) return;
////        if (index <= 0) return;
////        ShowStep(index - 1);
////    }

////    void UpdateNavButtons()
////    {
////        bool hasGuide = guide != null && guide.steps != null && guide.steps.Length > 0;
////        bool canPrev = hasGuide && index > 0;
////        bool canNext = hasGuide && index >= 0 && index < guide.steps.Length - 1;

////        if (prevButton) prevButton.interactable = canPrev;
////        if (nextButton) nextButton.interactable = canNext;
////    }

////    void ShowStep(int i)
////    {
////        if (guide?.steps == null || guide.steps.Length == 0) return;
////        if (i < 0 || i >= guide.steps.Length) return;
////        index = i;

////        overlay.Clear();
////        callout.Clear();

////        var step = guide.steps[i];
////        var phrases = new List<string>();
////        if (step.keywords != null) phrases.AddRange(step.keywords);
////        if (step.alts != null) phrases.AddRange(step.alts);

////        Rect? rectPx = null;
////        string matched = null;

////        if (ocr != null && phrases.Count > 0)
////        {
////            KeywordMatcher.Found? found = null;
////            foreach (var p in phrases.Where(s => !string.IsNullOrWhiteSpace(s)))
////            {
////                found = KeywordMatcher.FindBest(p, ocr, matchThreshold);
////                if (found != null) break;
////            }
////            if (found != null) { rectPx = found.Value.rectPx; matched = found.Value.matchedText; }
////        }

////        // highlight box (world-space canvas)
////        if (rectPx.HasValue)
////        {
////            overlay.RenderSingleBox(
////                boardOverlayRT,
////                new Vector2(ocr.width, ocr.height),
////                rectPx.Value,
////                new Color(0f, 1f, 0f, 0.55f)
////            );
////        }

////        // spawn callout
////        string title = string.IsNullOrEmpty(step.title) ? $"Step {i + 1}" : step.title;
////        if (titleTextTop) titleTextTop.text = title; // optional header
////        currentPanel = callout.ShowCalloutSmart(title, "", Vector2.zero, 260f, 120f);

////        // place above-center of the board area (same layout reasoning)
////        PlaceAboveCentered(currentPanel);

////        UpdateNavButtons();
////        Debug.Log($"[ARGuideRunner] Step {i + 1}/{guide.steps.Length} | match={(matched ?? "NONE")}");
////    }

////    void PlaceAboveCentered(RectTransform rt)
////    {
////        // board bounds in overlay local
////        Vector3[] sc = new Vector3[4]; boardOverlayRT.GetWorldCorners(sc);
////        Vector2 sBL = (Vector2)calloutLayer.InverseTransformPoint(sc[0]);
////        Vector2 sTR = (Vector2)calloutLayer.InverseTransformPoint(sc[2]);
////        float L = sBL.x, R = sTR.x, T = sTR.y;

////        // container bounds
////        var containerRT = (RectTransform)calloutLayer.parent;
////        Vector3[] cc = new Vector3[4]; containerRT.GetWorldCorners(cc);
////        Vector2 cBL = (Vector2)calloutLayer.InverseTransformPoint(cc[0]);
////        Vector2 cTR = (Vector2)calloutLayer.InverseTransformPoint(cc[2]);
////        float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

////        // width cap & layout
////        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
////        le.preferredWidth = Mathf.Min(maxPanelWidthCap, (R - L) - 2f * pad, (CR - CL) - 2f * pad);
////        le.flexibleWidth = 0;
////        var csf = rt.GetComponent<ContentSizeFitter>();
////        if (csf)
////        {
////            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
////            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
////        }
////        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
////        float w = rt.rect.width, h = rt.rect.height;

////        // bottom-center pivot, centered X over board, just above top
////        rt.pivot = new Vector2(0.5f, 0f);
////        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
////        float x = 0.5f * (L + R);
////        if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
////        if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;
////        rt.anchoredPosition = new Vector2(x, T + marginAbove * 1000f); // convert ~meters feel to px (canvas scale ~0.001)

////        // final clamp to container
////        Rect placed = new Rect(rt.anchoredPosition.x - rt.pivot.x * w, rt.anchoredPosition.y - rt.pivot.y * h, w, h);
////        Rect container = new Rect(CL, CB, CR - CL, CT - CB);
////        float dx = 0f, dy = 0f;
////        if (placed.xMin < container.xMin + pad) dx = (container.xMin + pad) - placed.xMin;
////        if (placed.xMax > container.xMax - pad) dx = (container.xMax - pad) - placed.xMax;
////        if (placed.yMin < container.yMin + pad) dy = (container.yMin + pad) - placed.yMin;
////        if (placed.yMax > container.yMax - pad) dy = (container.yMax - pad) - placed.yMax;
////        rt.anchoredPosition += new Vector2(dx, dy);
////    }
////}

//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System;

//public class ARGuideRunner : MonoBehaviour
//{
//    [Header("Scene refs (world-space)")]
//    public RectTransform boardOverlayRT;   // should be the "Overlay" RectTransform (stretched to canvas)
//    public RectTransform calloutLayer;     // should be the "CalloutLayer" RectTransform (stretched to canvas)
//    public OcrOverlay overlay;             // your OCR boxes renderer
//    public CalloutOverlay callout;         // your callout manager
//    public EasyOcrClient ocrClient;

//    [Header("Options")]
//    [Range(0.6f, 1f)] public float matchThreshold = 0.82f;
//    public float marginAbove = 0.06f;      // meters
//    public float pad = 8f;                 // pixels in canvas space
//    public float maxPanelWidthCap = 300f;  // pixels

//    [Header("Nav (optional)")]
//    public Button prevButton;
//    public Button nextButton;
//    public TextMeshProUGUI titleTextTop;

//    // runtime
//    AIGuide guide;
//    OcrResponse ocr;
//    int index = -1;
//    RectTransform currentPanel;
//    Texture2D screenshot;

//    public void Run(Texture2D tex, AIGuide g)
//    {
//        screenshot = tex;
//        guide = g;

//        UpdateNavButtons();

//        if (prevButton) prevButton.onClick.AddListener(PrevStep);
//        if (nextButton) nextButton.onClick.AddListener(NextStep);

//        if (screenshot == null || guide?.steps == null || guide.steps.Length == 0)
//        {
//            Debug.LogError("[ARGuideRunner] Missing screenshot/guide/steps.");
//            return;
//        }

//        StartCoroutine(ocrClient.Run(screenshot, resp =>
//        {
//            ocr = resp;
//            Debug.Log($"[ARGuideRunner] OCR ready: {resp.words.Length} words");
//            var sample = string.Join(", ", ocr.words.Take(20).Select(w => w.text));
//            Debug.Log($"[ARGuideRunner] OCR sample: {sample}");
//            ShowStep(0);
//            UpdateNavButtons();
//        },
//        err => Debug.LogError(err)));
//    }

//    public void NextStep()
//    {
//        if (guide?.steps == null || guide.steps.Length == 0) return;
//        if (index >= guide.steps.Length - 1) return;
//        ShowStep(index + 1);
//    }

//    public void PrevStep()
//    {
//        if (guide?.steps == null || guide.steps.Length == 0) return;
//        if (index <= 0) return;
//        ShowStep(index - 1);
//    }

//    bool TryFindLiteralRect(OcrResponse ocr, IEnumerable<string> candidates, out Rect rect)
//    {
//        rect = default;
//        if (ocr?.words == null) return false;

//        // Exact (case-insensitive)
//        foreach (var c in candidates)
//        {
//            if (string.IsNullOrWhiteSpace(c)) continue;
//            for (int i = 0; i < ocr.words.Length; i++)
//            {
//                var w = ocr.words[i];
//                if (string.Equals(w.text, c, StringComparison.OrdinalIgnoreCase))
//                {
//                    rect = new Rect(w.x, w.y, w.w, w.h);
//                    return true;
//                }
//            }
//        }

//        // Soft contains (helps with minor OCR noise like "Scenes," or "Scenes/")
//        foreach (var c in candidates)
//        {
//            if (string.IsNullOrWhiteSpace(c)) continue;
//            for (int i = 0; i < ocr.words.Length; i++)
//            {
//                var w = ocr.words[i];
//                if ((w?.text ?? "").IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0)
//                {
//                    rect = new Rect(w.x, w.y, w.w, w.h);
//                    return true;
//                }
//            }
//        }

//        return false;
//    }

//    void UpdateNavButtons()
//    {
//        bool hasGuide = guide != null && guide.steps != null && guide.steps.Length > 0;
//        bool canPrev = hasGuide && index > 0;
//        bool canNext = hasGuide && index >= 0 && index < guide.steps.Length - 1;

//        if (prevButton) prevButton.interactable = canPrev;
//        if (nextButton) nextButton.interactable = canNext;
//    }

//    void ShowStep(int i)
//    {
//        if (guide?.steps == null || guide.steps.Length == 0) return;
//        if (i < 0 || i >= guide.steps.Length) return;
//        index = i;

//        if (overlay) overlay.Clear();
//        if (callout) callout.Clear();

//        var step = guide.steps[i];
//        var phrases = new List<string>();
//        if (step.keywords != null) phrases.AddRange(step.keywords);
//        if (step.alts != null) phrases.AddRange(step.alts);

//        Rect? rectPx = null;
//        string matched = null;

//        // 1) Literal/contains first (fast & robust for “Scenes”)
//        if (ocr != null && phrases.Count > 0)
//        {
//            if (TryFindLiteralRect(ocr, phrases, out var r))
//            {
//                rectPx = r;
//                matched = "(literal)";
//            }
//        }

//        // 2) Fuzzy fallback if literal didn’t hit
//        if (rectPx == null && ocr != null && phrases.Count > 0)
//        {
//            KeywordMatcher.Found? found = null;
//            foreach (var p in phrases.Where(s => !string.IsNullOrWhiteSpace(s)))
//            {
//                found = KeywordMatcher.FindBest(p, ocr, matchThreshold);
//                if (found != null) break;
//            }
//            if (found != null) { rectPx = found.Value.rectPx; matched = found.Value.matchedText; }
//        }

//        // highlight box (world-space canvas)
//        if (rectPx.HasValue && overlay != null && boardOverlayRT != null)
//        {
//            overlay.RenderSingleBox(
//                boardOverlayRT,
//                new Vector2(ocr.width, ocr.height),
//                rectPx.Value,
//                new Color(0f, 1f, 0f, 0.55f)
//            );
//        }

//        // spawn callout
//        string title = string.IsNullOrEmpty(step.title) ? $"Step {i + 1}" : step.title;
//        if (titleTextTop) titleTextTop.text = title;
//        if (callout != null)
//        {
//            currentPanel = callout.ShowCalloutSmart(title, "", Vector2.zero, 260f, 120f);
//            PlaceAboveCentered(currentPanel);
//        }

//        UpdateNavButtons();
//        Debug.Log($"[ARGuideRunner] Step {i + 1}/{guide.steps.Length} | match={(matched ?? "NONE")}");
//    }

//    void PlaceAboveCentered(RectTransform rt)
//    {
//        if (rt == null || boardOverlayRT == null || calloutLayer == null) return;

//        // board bounds in overlay local
//        Vector3[] sc = new Vector3[4]; boardOverlayRT.GetWorldCorners(sc);
//        Vector2 sBL = (Vector2)calloutLayer.InverseTransformPoint(sc[0]);
//        Vector2 sTR = (Vector2)calloutLayer.InverseTransformPoint(sc[2]);
//        float L = sBL.x, R = sTR.x, T = sTR.y;

//        // container bounds
//        var containerRT = (RectTransform)calloutLayer.parent;
//        Vector3[] cc = new Vector3[4]; containerRT.GetWorldCorners(cc);
//        Vector2 cBL = (Vector2)calloutLayer.InverseTransformPoint(cc[0]);
//        Vector2 cTR = (Vector2)calloutLayer.InverseTransformPoint(cc[2]);
//        float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

//        // width cap & layout
//        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
//        le.preferredWidth = Mathf.Min(maxPanelWidthCap, (R - L) - 2f * pad, (CR - CL) - 2f * pad);
//        le.flexibleWidth = 0;
//        var csf = rt.GetComponent<ContentSizeFitter>();
//        if (csf) { csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
//        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
//        float w = rt.rect.width, h = rt.rect.height;

//        // meters → pixels from canvas scale
//        float pxPerMeter = 1000f; // default for scale 0.001
//        var parentCanvas = calloutLayer.GetComponentInParent<Canvas>();
//        if (parentCanvas)
//        {
//            var crt = (RectTransform)parentCanvas.transform;
//            if (crt) pxPerMeter = 1f / Mathf.Max(0.00001f, crt.localScale.x);
//        }

//        // bottom-center pivot, centered X over board, just above top
//        rt.pivot = new Vector2(0.5f, 0f);
//        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//        float x = 0.5f * (L + R);
//        if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
//        if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;
//        rt.anchoredPosition = new Vector2(x, T + marginAbove * pxPerMeter);

//        // final clamp to container
//        Rect placed = new Rect(rt.anchoredPosition.x - rt.pivot.x * w, rt.anchoredPosition.y - rt.pivot.y * h, w, h);
//        Rect container = new Rect(CL, CB, CR - CL, CT - CB);
//        float dx = 0f, dy = 0f;
//        if (placed.xMin < container.xMin + pad) dx = (container.xMin + pad) - placed.xMin;
//        if (placed.xMax > container.xMax - pad) dx = (container.xMax - pad) - placed.xMax;
//        if (placed.yMin < container.yMin + pad) dy = (container.yMin + pad) - placed.yMin;
//        if (placed.yMax > container.yMax - pad) dy = (container.yMax - pad) - placed.yMax;
//        rt.anchoredPosition += new Vector2(dx, dy);
//    }
//}


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Text;
using System.Text.RegularExpressions;

public class ARGuideRunner : MonoBehaviour
{
    [Header("Scene refs (world-space)")]
    public RectTransform boardOverlayRT;   // should be stretched to your AR board canvas
    public RectTransform calloutLayer;     // stretched to same canvas
    public OcrOverlay overlay;
    public CalloutOverlay callout;
    public EasyOcrClient ocrClient;

    [Header("Options")]
    [Range(0.6f, 1f)] public float matchThreshold = 0.78f; // slightly lower to help borderline matches
    public float marginAbove = 0.06f;      // meters
    public float pad = 8f;                 // px
    public float maxPanelWidthCap = 300f;  // px
    public bool debugVerbose = true;

    [Header("Nav (optional)")]
    public Button prevButton;
    public Button nextButton;
    public TextMeshProUGUI titleTextTop;

    [SerializeField] string testingKeyword = "Scenes";   // testing keyword(s)
    bool _ocrRequested = false;
    const float MATCH_THRESHOLD = 0.65f; // was 0.78/0.82


    // runtime
    AIGuide guide;
    OcrResponse ocr;
    int index = -1;
    RectTransform currentPanel;
    Texture2D screenshot;

    void Start()
    {
        // Basic binding checks (these catch many “panel invisible” issues)
        if (debugVerbose)
        {
            Debug.Log($"[ARGuideRunner] Bindings | boardOverlayRT={(boardOverlayRT ? boardOverlayRT.name : "NULL")} | calloutLayer={(calloutLayer ? calloutLayer.name : "NULL")} | overlay={(overlay ? "OK" : "NULL")} | callout={(callout ? "OK" : "NULL")} | ocrClient={(ocrClient ? "OK" : "NULL")}");
            var c = calloutLayer ? calloutLayer.GetComponentInParent<Canvas>() : null;
            if (c)
            {
                var crt = (RectTransform)c.transform;
                Debug.Log($"[ARGuideRunner] Canvas | worldCamera={(c.worldCamera ? c.worldCamera.name : "NULL")} | sortingOrder={c.sortingOrder} | scale={crt.localScale}");
            }
        }
    }

    public void Run(Texture2D tex, AIGuide g)
    {
        screenshot = tex;
        guide = g;

        UpdateNavButtons();

        if (prevButton) prevButton.onClick.AddListener(PrevStep);
        if (nextButton) nextButton.onClick.AddListener(NextStep);

        if (screenshot == null || guide?.steps == null || guide.steps.Length == 0)
        {
            Debug.LogError("[ARGuideRunner] Missing screenshot/guide/steps.");
            return;
        }

        if (_ocrRequested) return;           // <-- guard
        _ocrRequested = true;

        Debug.Log($"[AR/OCR] sending {screenshot.width}x{screenshot.height}");
        StartCoroutine(ocrClient.Run(screenshot, resp =>
        {
            ocr = resp;
            var sample = string.Join(", ", ocr.words.Take(20).Select(w => w.text));
            Debug.Log($"[ARGuideRunner] OCR ready: {resp.words.Length} words | image={resp.width}x{resp.height}");
            Debug.Log($"[ARGuideRunner] OCR sample: {sample}");
            ShowStep(0);
            UpdateNavButtons();
        },
        err => Debug.LogError(err)));
    }

    public void NextStep()
    {
        if (guide?.steps == null || guide.steps.Length == 0) return;
        if (index >= guide.steps.Length - 1) return;
        ShowStep(index + 1);
    }

    public void PrevStep()
    {
        if (guide?.steps == null || guide.steps.Length == 0) return;
        if (index <= 0) return;
        ShowStep(index - 1);
    }

    // Normalize OCR tokens and candidate phrases (strip punctuation/extra spaces)
    static string NormalizeToken(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        // remove non-letters/digits (keep basic underscores/dots if you need)
        var t = Regex.Replace(s, @"[^\p{L}\p{N}\s\-_\.]", "");
        // collapse whitespace
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return t;
    }

    bool TryFindLiteralRect(OcrResponse ocr, IEnumerable<string> candidates, out Rect rect, out string matchedHow, out string matchedText)
    {
        rect = default;
        matchedHow = null; matchedText = null;
        if (ocr?.words == null) return false;

        // exact match (normalized, case-insensitive)
        foreach (var cRaw in candidates)
        {
            var c = NormalizeToken(cRaw);
            if (string.IsNullOrWhiteSpace(c)) continue;

            for (int i = 0; i < ocr.words.Length; i++)
            {
                var w = ocr.words[i];
                string wt = NormalizeToken(w.text ?? "");
                if (string.Equals(wt, c, StringComparison.OrdinalIgnoreCase))
                {
                    rect = new Rect(w.x, w.y, w.w, w.h);
                    matchedHow = "literal-exact";
                    matchedText = w.text;
                    return true;
                }
            }
        }

        // contains match (helps “Scenes,” “Scenes/”, “New…Scene”)
        foreach (var cRaw in candidates)
        {
            var c = NormalizeToken(cRaw);
            if (string.IsNullOrWhiteSpace(c)) continue;

            for (int i = 0; i < ocr.words.Length; i++)
            {
                var w = ocr.words[i];
                string wt = NormalizeToken(w.text ?? "");
                if (wt.IndexOf(c, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    rect = new Rect(w.x, w.y, w.w, w.h);
                    matchedHow = "literal-contains";
                    matchedText = w.text;
                    return true;
                }
            }
        }

        return false;
    }

    void UpdateNavButtons()
    {
        bool hasGuide = guide != null && guide.steps != null && guide.steps.Length > 0;
        bool canPrev = hasGuide && index > 0;
        bool canNext = hasGuide && index >= 0 && index < guide.steps.Length - 1;

        if (prevButton) prevButton.interactable = canPrev;
        if (nextButton) nextButton.interactable = canNext;
    }

    void ShowStep(int i)
    {
        if (guide?.steps == null || guide.steps.Length == 0) return;
        if (i < 0 || i >= guide.steps.Length) return;
        index = i;

        overlay?.Clear();
        callout?.Clear();

        var step = guide.steps[i];
        var phrases = new List<string>();
        if (!string.IsNullOrWhiteSpace(testingKeyword))
        {
            phrases.Add(testingKeyword);                   // testing override
        }
        else
        {
            if (step.keywords != null) phrases.AddRange(step.keywords);
            if (step.alts != null) phrases.AddRange(step.alts);
        }
        if (debugVerbose)
        {
            Debug.Log($"[ARGuideRunner] Step {i + 1} | candidates= [{string.Join(" | ", phrases)}]");
        }

        Rect? rectPx = null;
        string matchedMode = null;
        string matchedText = null;

        // 1) Literal/contains first (fast & robust)
        if (ocr != null && phrases.Count > 0)
        {
            if (TryFindLiteralRect(ocr, phrases, out var r, out matchedMode, out matchedText))
            {
                rectPx = r;
            }
        }

        // 2) Fuzzy fallback if needed
        if (rectPx == null && ocr != null && phrases.Count > 0)
        {
            KeywordMatcher.Found? found = null;
            foreach (var p in phrases.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                found = KeywordMatcher.FindBest(p, ocr, MATCH_THRESHOLD);
                if (found != null) break;
            }
            if (found != null)
            {
                rectPx = found.Value.rectPx;
                matchedText = found.Value.matchedText;
                matchedMode = "fuzzy";
            }
        }

        // Draw green box if we have a hit (skip red debug spam by default)
        if (rectPx.HasValue && overlay != null && boardOverlayRT != null)
        {
            overlay.RenderSingleBox(
                boardOverlayRT,
                new Vector2(ocr.width, ocr.height),
                rectPx.Value,
                new Color(0f, 1f, 0f, 0.55f)
            );
            Debug.Log($"[ARGuideRunner] MATCH \"{matchedText}\" mode={matchedMode} rect={rectPx.Value}");
        }
        else
        {
            Debug.Log("[ARGuideRunner] No match"); // essential no-match log
        }

        // Callout panel (keep as-is)
        string title = string.IsNullOrEmpty(step.title) ? $"Step {i + 1}" : step.title;
        if (titleTextTop) titleTextTop.text = title;
        if (callout != null)
        {
            currentPanel = callout.ShowCalloutSmart(title, "", Vector2.zero, 260f, 120f);
            PlaceAboveCentered(currentPanel);
        }

        UpdateNavButtons();
    }

    void PlaceAboveCentered(RectTransform rt)
    {
        if (rt == null || boardOverlayRT == null || calloutLayer == null) return;

        // board bounds in overlay local
        Vector3[] sc = new Vector3[4]; boardOverlayRT.GetWorldCorners(sc);
        Vector2 sBL = (Vector2)calloutLayer.InverseTransformPoint(sc[0]);
        Vector2 sTR = (Vector2)calloutLayer.InverseTransformPoint(sc[2]);
        float L = sBL.x, R = sTR.x, T = sTR.y;

        // container bounds
        var containerRT = (RectTransform)calloutLayer.parent;
        Vector3[] cc = new Vector3[4]; containerRT.GetWorldCorners(cc);
        Vector2 cBL = (Vector2)calloutLayer.InverseTransformPoint(cc[0]);
        Vector2 cTR = (Vector2)calloutLayer.InverseTransformPoint(cc[2]);
        float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

        // width cap & layout
        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = Mathf.Min(maxPanelWidthCap, (R - L) - 2f * pad, (CR - CL) - 2f * pad);
        le.flexibleWidth = 0;
        var csf = rt.GetComponent<ContentSizeFitter>();
        if (csf) { csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
        float w = rt.rect.width, h = rt.rect.height;

        // meters → pixels using canvas scale
        float pxPerMeter = 1000f; // if canvas scale is 0.001
        var parentCanvas = calloutLayer.GetComponentInParent<Canvas>();
        if (parentCanvas)
        {
            var crt = (RectTransform)parentCanvas.transform;
            if (crt) pxPerMeter = 1f / Mathf.Max(0.00001f, crt.localScale.x);
        }

        // bottom-center pivot, centered X over board, above the top edge
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        float x = 0.5f * (L + R);
        if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
        if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;
        rt.anchoredPosition = new Vector2(x, T + marginAbove * pxPerMeter);

        // final clamp to container
        Rect placed = new Rect(rt.anchoredPosition.x - rt.pivot.x * w, rt.anchoredPosition.y - rt.pivot.y * h, w, h);
        Rect container = new Rect(CL, CB, CR - CL, CT - CB);
        float dx = 0f, dy = 0f;
        if (placed.xMin < container.xMin + pad) dx = (container.xMin + pad) - placed.xMin;
        if (placed.xMax > container.xMax - pad) dx = (container.xMax - pad) - placed.xMax;
        if (placed.yMin < container.yMin + pad) dy = (container.yMin + pad) - placed.yMin;
        if (placed.yMax > container.yMax - pad) dy = (container.yMax - pad) - placed.yMax;
        rt.anchoredPosition += new Vector2(dx, dy);

        if (debugVerbose)
        {
            Debug.Log($"[ARGuideRunner] Place panel | Board LRT=({L:F1},{R:F1},{T:F1}) | Container=({CL:F1},{CB:F1},{CR:F1},{CT:F1}) | px/m={pxPerMeter:F1} | size=({w:F0}x{h:F0}) | pos={rt.anchoredPosition}");
        }
    }
}
