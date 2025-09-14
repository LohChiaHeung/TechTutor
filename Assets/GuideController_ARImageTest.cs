////using System;
////using System.Collections;
////using System.Collections.Generic;
////using System.Linq;
////using System.Net.Sockets;
////using UnityEngine;
////using UnityEngine.UI;

////public class GuideController_ARImageTest : MonoBehaviour
////{
////    [Header("Scene refs")]
////    public RawImage screenshotImage;        // assign: the RawImage that shows the screenshot
////    public RectTransform screenshotRT;      // assign: screenshotImage.rectTransform
////    public RectTransform calloutLayer;      // assign: overlay container (stretch/stretch)
////    public OcrOverlay overlay;         // assign: your overlay renderer
////    public CalloutOverlay callout;          // assign: your callout manager
////    public EasyOcrClient ocrClient;             // assign: your OCR client
////    private Rect? lastRectPx = null;
////    private string lastMatchedText = null;

////    [Header("Options")]
////    [Range(0.6f, 1f)] public float matchThreshold = 0.82f;
////    public float marginAbove = 20f;
////    public float pad = 8f;
////    public float maxPanelWidthCap = 300f;
////    [Header("UI Buttons")]
////    public Button prevButton;
////    public Button nextButton;


////    // runtime
////    private AIGuide guide;
////    private OcrResponse ocr;
////    private int index = -1;
////    private RectTransform currentPanel;

////    void Start()
////    {
////        // 1) get screenshot + steps from the holder
////        if (GuideRunContext.I == null || GuideRunContext.I.guide == null || GuideRunContext.I.screenshot == null)
////        {
////            Debug.LogError("[AR] No guide/screenshot in GuideRunContext. Open this scene from ar_ai_tutorial after asking AI.");
////            return;
////        }

////        guide = GuideRunContext.I.guide;
////        var tex = GuideRunContext.I.screenshot;

////        // show the image
////        screenshotImage.texture = tex;
////        EnsureOverlayOnTop();
////        UpdateNavButtons();

////        // 2) run OCR once
////        StartCoroutine(ocrClient.Run(tex, resp =>
////        {
////            ocr = resp;
////            Debug.Log($"[AR_ImageTest] OCR ready: {resp.words.Length} words");
////            ShowStep(0);
////            UpdateNavButtons();
////        },
////        err => Debug.LogError(err)));
////    }

////    // 1) SAFETY GUARDS: ignore clicks at bounds
////    public void NextStep()
////    {
////        if (guide == null || guide.steps == null || guide.steps.Length == 0) return;
////        if (index >= guide.steps.Length - 1) return;   // <-- guard
////        ShowStep(index + 1);
////    }

////    public void PrevStep()
////    {
////        if (guide == null || guide.steps == null || guide.steps.Length == 0) return;
////        if (index <= 0) return;                        // <-- guard
////        ShowStep(index - 1);
////    }


////    // 2) UPDATE NAV BUTTONS (handles index = -1 on first load)
////    void UpdateNavButtons()
////    {
////        bool hasGuide = guide != null && guide.steps != null && guide.steps.Length > 0;

////        // Enable Next if we haven't shown the first step yet (index == -1),
////        // or if there are more steps ahead.
////        bool canPrev = hasGuide && index > 0;
////        bool canNext = hasGuide && index < guide.steps.Length - 1; // works even when index == -1

////        if (prevButton) prevButton.interactable = canPrev;
////        if (nextButton) nextButton.interactable = canNext;
////    }


////    Rect PixelRectToOverlay(Rect pxBL)
////    {
////        // screenshot bounds in overlay local
////        Vector3[] sc = new Vector3[4]; screenshotRT.GetWorldCorners(sc);
////        Vector2 sBL = (Vector2)calloutLayer.InverseTransformPoint(sc[0]);
////        Vector2 sTR = (Vector2)calloutLayer.InverseTransformPoint(sc[2]);
////        float L = sBL.x, B = sBL.y, R = sTR.x, T = sTR.y;

////        float iw = ocr.width, ih = ocr.height;     // OCR image size (px)
////        float sw = R - L, sh = T - B;

////        // aspect-fit (letterbox aware)
////        float scale = Mathf.Min(sw / iw, sh / ih);
////        float dw = iw * scale, dh = ih * scale;
////        float ox = L + (sw - dw) * 0.5f;           // left of displayed image
////        float oy = B + (sh - dh) * 0.5f;           // bottom of displayed image

////        float x = ox + (pxBL.xMin / iw) * dw;
////        float y = oy + (pxBL.yMin / ih) * dh;      // <- already bottom-left based
////        float w = (pxBL.width / iw) * dw;
////        float h = (pxBL.height / ih) * dh;

////        return new Rect(x, y, w, h);
////    }


////    void PlaceBesideTarget(RectTransform rt, Rect target)
////    {
////        // layout width cap
////        var le = rt.GetComponent<LayoutElement>() ?? rt.gameObject.AddComponent<LayoutElement>();
////        le.preferredWidth = Mathf.Min(maxPanelWidthCap, Mathf.Max(220f, target.width * 1.5f));
////        le.flexibleWidth = 0;
////        var csf = rt.GetComponent<ContentSizeFitter>();
////        if (csf) { csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
////        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
////        float w = rt.rect.width, h = rt.rect.height;

////        float CL = -calloutLayer.rect.width * 0.5f;
////        float CR = calloutLayer.rect.width * 0.5f;
////        float CB = -calloutLayer.rect.height * 0.5f;
////        float CT = calloutLayer.rect.height * 0.5f;

////        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

////        // try RIGHT
////        rt.pivot = new Vector2(0f, 0.5f);
////        Vector2 pos = new Vector2(target.xMax + pad, target.center.y);

////        // overflow? try LEFT
////        if (pos.x + w > CR - pad)
////        {
////            rt.pivot = new Vector2(1f, 0.5f);
////            pos = new Vector2(target.xMin - pad, target.center.y);
////        }

////        // clamp
////        float minX = CL + pad + w * rt.pivot.x;
////        float maxX = CR - pad - w * (1f - rt.pivot.x);
////        float minY = CB + pad + h * 0.5f;
////        float maxY = CT - pad - h * 0.5f;

////        pos.x = Mathf.Clamp(pos.x, minX, maxX);
////        pos.y = Mathf.Clamp(pos.y, minY, maxY);

////        rt.anchoredPosition = pos;
////    }

////    void ShowStep(int i)
////    {
////        if (guide?.steps == null || guide.steps.Length == 0) return;
////        if (i < 0 || i >= guide.steps.Length) return;
////        index = i;

////        overlay.Clear();
////        callout.Clear();

////        var step = guide.steps[i];

////        // --- build phrases robustly (fallbacks) ---
////        var phrases = new List<string>();
////        if (step.keywords != null) phrases.AddRange(step.keywords);
////        if (step.alts != null) phrases.AddRange(step.alts);
////        // helpful fallbacks if keywords missing:
////        if (!string.IsNullOrWhiteSpace(step.title)) phrases.Add(step.title);
////        if (!string.IsNullOrWhiteSpace(step.instruction)) phrases.Add(step.instruction);

////        // normalize
////        phrases = phrases.Where(s => !string.IsNullOrWhiteSpace(s))
////                         .Select(s => s.Trim())
////                         .Distinct(StringComparer.OrdinalIgnoreCase)
////                         .ToList();

////        // --- find best OCR box ---
////        Rect? rectPx = null; string matched = null;
////        if (ocr != null && phrases.Count > 0)
////        {
////            KeywordMatcher.Found? found = null;
////            foreach (var p in phrases)
////            {
////                found = KeywordMatcher.FindBest(p, ocr, matchThreshold);
////                if (found != null) break;
////            }
////            if (found != null) { rectPx = found.Value.rectPx; matched = found.Value.matchedText; }
////        }

////        // --- draw green box; if none found, reuse last ---
////        Rect? drawRectPx = rectPx.HasValue ? rectPx : lastRectPx;
////        if (drawRectPx.HasValue)
////        {
////            // 2a) Convert OCR rect (top-left) -> bottom-left
////            var drawBL = OcrTopLeftToBottomLeft(drawRectPx.Value, ocr.width, ocr.height);

////            // (A) Draw green box using the BL rect
////            overlay.RenderSingleBox(
////                screenshotRT,
////                new Vector2(ocr.width, ocr.height),
////                drawBL,
////                new Color(0f, 1f, 0f, 0.55f)
////            );

////            // (B) Place panel beside the SAME rect (mapped into overlay space)
////            var rectOv = PixelRectToOverlay(drawBL);   // <-- pass BL rect here
////            PlaceBesideTarget(currentPanel, rectOv);
////        }
////        else
////        {
////            // first step with nothing? center
////            if (i == 0) PlaceAboveCentered(currentPanel);
////        }

////        //// place panel near the word if we have a rect, otherwise keep stable
////        //if (drawRectPx.HasValue)
////        //{
////        //    var rectOv = PixelRectToOverlay(drawRectPx.Value);
////        //    PlaceBesideTarget(currentPanel, rectOv);
////        //}
////        //else
////        //{
////        //    // first step with nothing? center; otherwise keep previous position (don’t jump)
////        //    if (i == 0) PlaceAboveCentered(currentPanel);
////        //}

////        // remember last good match only when we truly found a new one
////        //if (rectPx.HasValue) { lastRectPx = rectPx; lastMatchedText = matched; }

////        UpdateNavButtons();
////        Debug.Log($"[AR_ImageTest] Step {i + 1}/{guide.steps.Length} | match={(matched ?? (lastMatchedText != null ? $"(reuse {lastMatchedText})" : "NONE"))}");
////    }


////    void EnsureOverlayOnTop()
////    {
////        // 1) Put overlay under the same parent so mask/scroll works the same
////        if (calloutLayer.parent != screenshotRT.parent)
////            calloutLayer.SetParent(screenshotRT.parent, worldPositionStays: false);

////        // 2) Make overlay draw AFTER the screenshot
////        int i = screenshotRT.GetSiblingIndex();
////        calloutLayer.SetSiblingIndex(i + 1);

////        // 3) Optional: keep overlay always the last sibling
////        calloutLayer.SetAsLastSibling();

////        // 4) Optional: screenshot shouldn't block clicks
////        var img = screenshotRT.GetComponent<UnityEngine.UI.Graphic>();
////        if (img) img.raycastTarget = false;

////        // Fill the parent like the screenshot
////        calloutLayer.anchorMin = new Vector2(0, 0);
////        calloutLayer.anchorMax = new Vector2(1, 1);
////        calloutLayer.pivot = new Vector2(0.5f, 0.5f);
////        calloutLayer.offsetMin = Vector2.zero;
////        calloutLayer.offsetMax = Vector2.zero;
////    }

////    // Convert an OCR pixel rect (top-left origin) to bottom-left origin
////    Rect OcrTopLeftToBottomLeft(Rect px, float iw, float ih)
////    {
////        // px.yMin is from the TOP; convert so y grows upwards from BOTTOM
////        float yBL = ih - (px.yMin + px.height);
////        return new Rect(px.xMin, yBL, px.width, px.height);
////    }

////    void PlaceAboveCentered(RectTransform rt)
////    {
////        // screenshot bounds in overlay local
////        Vector3[] sc = new Vector3[4]; screenshotRT.GetWorldCorners(sc);
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
////        if (csf) { csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize; csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize; }
////        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
////        float w = rt.rect.width, h = rt.rect.height;

////        // bottom-center pivot, centered X over screenshot, just above T
////        rt.pivot = new Vector2(0.5f, 0f);
////        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
////        float x = 0.5f * (L + R);
////        if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
////        if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;
////        rt.anchoredPosition = new Vector2(x, T + marginAbove);

////        // final clamp
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

//using System.Collections.Generic;
//using UnityEngine.UI;
//using UnityEngine;

//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Sockets;
//using UnityEngine;
//using UnityEngine.UI;

//public class GuideController_ARImageTest : MonoBehaviour
//{
//    [Header("Scene refs")]
//    public RawImage screenshotImage;        // assign: the RawImage that shows the screenshot
//    public RectTransform screenshotRT;      // assign: screenshotImage.rectTransform
//    public RectTransform calloutLayer;      // assign: overlay container (stretch/stretch)
//    public OcrOverlay overlay;         // assign: your overlay renderer
//    public CalloutOverlay callout;          // assign: your callout manager
//    public EasyOcrClient ocrClient;             // assign: your OCR client

//    [Header("Options")]
//    [Range(0.6f, 1f)] public float matchThreshold = 0.82f;
//    public float marginAbove = 20f;
//    public float pad = 8f;
//    public float maxPanelWidthCap = 300f;

//    // runtime
//    private AIGuide guide;
//    private OcrResponse ocr;
//    private int index = -1;
//    private RectTransform currentPanel;


//    void Start()
//    {
//        // 1) get screenshot + steps from the holder
//        if (GuideRunContext.I == null || GuideRunContext.I.guide == null || GuideRunContext.I.screenshot == null)
//        {
//            Debug.LogError("❌ No guide/screenshot in GuideRunContext. Open this scene from ar_ai_tutorial after asking AI.");
//            return;
//        }

//        guide = GuideRunContext.I.guide;
//        var tex = GuideRunContext.I.screenshot;

//        // show the image
//        screenshotImage.texture = tex;

//        // 2) run OCR once
//        StartCoroutine(ocrClient.Run(tex, resp =>
//        {
//            ocr = resp;
//            Debug.Log($"[AR_ImageTest] OCR ready: {resp.words.Length} words");
//            ShowStep(0);
//        },
//        err => Debug.LogError(err)));
//    }

//    public void NextStep() { if (guide == null) return; ShowStep(Mathf.Clamp(index + 1, 0, guide.steps.Length - 1)); }
//    public void PrevStep() { if (guide == null) return; ShowStep(Mathf.Clamp(index - 1, 0, guide.steps.Length - 1)); }

//    void ShowStep(int i)
//    {
//        if (guide?.steps == null || guide.steps.Length == 0) return;
//        if (i < 0 || i >= guide.steps.Length) return;
//        index = i;

//        overlay.Clear();
//        callout.Clear();

//        var step = guide.steps[i];
//        // 3) choose a phrase to match (keywords then alts)
//        var phrases = new List<string>();
//        if (step.keywords != null) phrases.AddRange(step.keywords);
//        if (step.alts != null) phrases.AddRange(step.alts);

//        // 4) find best OCR box
//        Rect? rectPx = null;
//        string matched = null;

//        if (ocr != null && phrases.Count > 0)
//        {

//            KeywordMatcher.Found? found = null;
//            foreach (var p in phrases.Where(s => !string.IsNullOrWhiteSpace(s)))
//            {
//                found = KeywordMatcher.FindBest(p, ocr, matchThreshold);
//                if (found != null) break;
//            }
//            if (found != null) { rectPx = found.Value.rectPx; matched = found.Value.matchedText; }
//        }

//        // 5) draw highlight if found
//        if (rectPx.HasValue)
//        {
//            overlay.RenderSingleBox(
//                screenshotRT,
//                new Vector2(ocr.width, ocr.height),
//                rectPx.Value,
//                new Color(0f, 1f, 0f, 0.55f)
//            );
//        }

//        // 6) spawn the callout (title + instruction)
//        string title = string.IsNullOrEmpty(step.title) ? $"Step {i + 1}" : step.title;
//        string body = string.IsNullOrEmpty(step.instruction) ? "" : "→ " + step.instruction;
//        currentPanel = callout.ShowCalloutSmart(title, body, Vector2.zero, 260f, 120f);

//        // 7) place panel ALWAYS ABOVE + CENTERED over the screenshot
//        PlaceAboveCentered(currentPanel);

//        Debug.Log($"[AR_ImageTest] Step {i + 1}/{guide.steps.Length} | match={(matched ?? "NONE")}");
//    }

//    void PlaceAboveCentered(RectTransform rt)
//    {
//        // screenshot bounds in overlay local
//        Vector3[] sc = new Vector3[4]; screenshotRT.GetWorldCorners(sc);
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

//        // bottom-center pivot, centered X over screenshot, just above T
//        rt.pivot = new Vector2(0.5f, 0f);
//        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//        float x = 0.5f * (L + R);
//        if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
//        if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;
//        rt.anchoredPosition = new Vector2(x, T + marginAbove);

//        // final clamp
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