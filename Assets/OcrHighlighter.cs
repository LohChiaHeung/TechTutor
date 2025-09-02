using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class OcrHighlighter : MonoBehaviour
{
    [Header("Refs")]
    public EasyOcrClient ocrClient;
    public RawImage screenshotImage;        // ScreenshotImage
    public RectTransform screenshotRT;      // ScreenshotImage RectTransform
    public OcrOverlay overlay;              // has rawImageRT + overlayRoot assigned
    public CalloutOverlay callout;          // simple version (ShowCallout(x,y,w,h))

    [Header("Target")]
    public string targetText = "Data";
    [Range(0f, 1f)] public float minScore = 0.6f;

    //void Start()
    //{
    //    var tex = screenshotImage.texture as Texture2D;
    //    if (tex == null)
    //    {
    //        Debug.LogError("ScreenshotImage has no Texture2D. Assign a texture before running OCR.");
    //        return;
    //    }

    //    StartCoroutine(ocrClient.Run(
    //        tex,
    //        resp =>
    //        {
    //            Debug.Log($"[OCR] words={resp.words.Length}, image={resp.width}x{resp.height}");

    //            // 1) Show ALL OCR boxes (red) so you can see detections
    //            overlay.Clear();
    //            overlay.Render(resp);
    //            // 2) Pick best match against targetText
    //            var best = resp.words
    //                .Select(w => new { w, score = Score(w.text, targetText) })
    //                .OrderByDescending(p => p.score)
    //                .FirstOrDefault();

    //            if (best == null)
    //            {
    //                Debug.LogWarning("[OCR] No words returned by OCR; cannot draw highlight.");
    //                callout.Clear();
    //                return;
    //            }

    //            // ✅ Always draw the green box for the best candidate (debug visibility first)
    //            var rectPx = new Rect(best.w.x, best.w.y, best.w.w, best.w.h);
    //            overlay.RenderSingleBox(
    //                screenshotRT,
    //                new Vector2(resp.width, resp.height),
    //                rectPx,
    //                new Color(0f, 1f, 0f, 0.55f)
    //            );
    //            Debug.Log($"[OCR] best='{best.w.text}' score={best.score:0.00} — drew green box");

    //            // If score is low, SKIP only the panel; keep the green box visible
    //            if (best.score < minScore)
    //            {
    //                Debug.LogWarning($"[OCR] Best score {best.score:0.00} < minScore {minScore:0.00}. Showing green box for debug, skipping callout.");
    //                callout.Clear();
    //                return;
    //            }

    //            // Word rect in ScreenshotImage local space
    //            Rect uiRect = ImageUiMapper.ImageRectPxToLocalRect(
    //                rectPx, screenshotRT, new Vector2(resp.width, resp.height)
    //            );
    //            DrawGreenBoxDirect(uiRect);


    //            // --- compute panel placement OUTSIDE the screenshot (same local space) ---
    //            //Vector2 panelSize = new Vector2(280, 140);
    //            //float margin = 20f;
    //            //Rect screenRect = screenshotRT.rect;

    //            //float wordCenterX = uiRect.center.x;
    //            //float wordCenterY = uiRect.center.y;

    //            //string side;
    //            //if (wordCenterX < screenRect.width * 0.33f) side = "left";
    //            //else if (wordCenterX > screenRect.width * 0.66f) side = "right";
    //            //else if (wordCenterY > screenRect.height * 0.5f) side = "above";
    //            //else side = "below";

    //            //Vector2 panelPos;
    //            //if (side == "left")
    //            //    panelPos = new Vector2(screenRect.xMin - panelSize.x - margin, wordCenterY);
    //            //else if (side == "right")
    //            //    panelPos = new Vector2(screenRect.xMax + margin, wordCenterY);
    //            //else if (side == "above")
    //            //    panelPos = new Vector2(
    //            //        Mathf.Clamp(wordCenterX - panelSize.x * 0.5f, screenRect.xMin, screenRect.xMax - panelSize.x),
    //            //        screenRect.yMax + margin
    //            //    );
    //            //else // below
    //            //    panelPos = new Vector2(
    //            //        Mathf.Clamp(wordCenterX - panelSize.x * 0.5f, screenRect.xMin, screenRect.xMax - panelSize.x),
    //            //        screenRect.yMin - panelSize.y - margin
    //            //    );

    //            // ================== REFS / GEOMETRY ==================
    //            // A. screenshot bounds in CalloutLayer local
    //            Vector3[] sc = new Vector3[4];
    //            screenshotRT.GetWorldCorners(sc);
    //            Vector2 sBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[0]); // bottom-left
    //            Vector2 sTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[2]); // top-right
    //            float L = sBL.x, B = sBL.y, R = sTR.x, T = sTR.y;

    //            // B. container bounds = parent of CalloutLayer (Panel or Canvas)
    //            RectTransform containerRT = (RectTransform)callout.CalloutLayer.parent;
    //            Vector3[] cc = new Vector3[4];
    //            containerRT.GetWorldCorners(cc);
    //            Vector2 cBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[0]);
    //            Vector2 cTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[2]);
    //            float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

    //            // C. word center in CalloutLayer local
    //            Vector2 wordCenterLocal = (Vector2)callout.CalloutLayer.InverseTransformPoint(
    //                screenshotRT.TransformPoint(uiRect.center)
    //            );

    //            // ================== SPAWN & MEASURE (dynamic panel) ==================
    //            float margin = 20f;
    //            float capW = 260f, capH = 120f;

    //            callout.Clear();
    //            var rt = callout.ShowCalloutSmart("Step 1", $"Click {targetText}", Vector2.zero, capW, capH);
    //            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

    //            // Hard-cap width so long text wraps (prevents 700px panels)
    //            float maxW = Mathf.Min(300f, (CR - CL) * 0.45f);  // ~300px or 45% of container width
    //            var le = rt.GetComponent<UnityEngine.UI.LayoutElement>() ?? rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
    //            le.preferredWidth = maxW; le.flexibleWidth = 0;

    //            var csf = rt.GetComponent<UnityEngine.UI.ContentSizeFitter>();
    //            if (csf)
    //            {
    //                csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
    //                csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
    //            }

    //            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    //            float w = rt.rect.width, h = rt.rect.height;

    //            // ================== FOUR OUTSIDE ZONES (inside container) ==================
    //            Rect leftZone = new Rect(CL, B, Mathf.Max(0f, L - margin - CL), Mathf.Max(0f, T - B));
    //            Rect rightZone = new Rect(R + margin, B, Mathf.Max(0f, CR - (R + margin)), Mathf.Max(0f, T - B));
    //            Rect topZone = new Rect(CL, T + margin, Mathf.Max(0f, CR - CL), Mathf.Max(0f, CT - (T + margin)));
    //            Rect bottomZone = new Rect(CL, CB, Mathf.Max(0f, CR - CL), Mathf.Max(0f, (B - margin) - CB));

    //            bool fitLeft = leftZone.width >= w && leftZone.height >= h;
    //            bool fitRight = rightZone.width >= w && rightZone.height >= h;
    //            bool fitTop = topZone.width >= w && topZone.height >= h;
    //            bool fitBottom = bottomZone.width >= w && bottomZone.height >= h;

    //            // ================== PICK NEAREST FITTING ZONE ==================
    //            float distLeft = Mathf.Abs(wordCenterLocal.x - leftZone.xMax);
    //            float distRight = Mathf.Abs(wordCenterLocal.x - rightZone.xMin);
    //            float distTop = Mathf.Abs(wordCenterLocal.y - topZone.yMin);
    //            float distBottom = Mathf.Abs(wordCenterLocal.y - bottomZone.yMax);

    //            (string side, float d) chosen;
    //            var candidates = new System.Collections.Generic.List<(string, float)>();
    //            if (fitLeft) candidates.Add(("left", distLeft));
    //            if (fitRight) candidates.Add(("right", distRight));
    //            if (fitTop) candidates.Add(("above", distTop));
    //            if (fitBottom) candidates.Add(("below", distBottom));
    //            if (candidates.Count == 0) // fallback: pick the largest zone
    //            {
    //                float spanLeft = leftZone.width - w;
    //                float spanRight = rightZone.width - w;
    //                float spanTop = topZone.height - h;
    //                float spanBot = bottomZone.height - h;
    //                float m = Mathf.Max(spanLeft, spanRight, spanTop, spanBot);
    //                chosen = (m == spanLeft) ? ("left", 0) : (m == spanRight) ? ("right", 0) : (m == spanTop) ? ("above", 0) : ("below", 0);
    //            }
    //            else
    //            {
    //                candidates.Sort((a, b) => a.Item2.CompareTo(b.Item2));
    //                chosen = candidates[0];
    //            }

    //            // ================== FINAL POSITION (set pivot per side, then place) ==================
    //            Vector2 finalPos;
    //            Vector2 pivot;

    //            if (chosen.side == "left")
    //            {
    //                // Place with pivot at RIGHT-MIDDLE so the panel grows to the left
    //                pivot = new Vector2(1f, 0.5f);

    //                float y = Mathf.Clamp(wordCenterLocal.y, leftZone.yMin + h * 0.5f, leftZone.yMax - h * 0.5f);
    //                finalPos = new Vector2(leftZone.xMax, y);
    //            }
    //            else if (chosen.side == "right")
    //            {
    //                // Place with pivot at LEFT-MIDDLE so the panel grows to the right
    //                pivot = new Vector2(0f, 0.5f);

    //                float y = Mathf.Clamp(wordCenterLocal.y, rightZone.yMin + h * 0.5f, rightZone.yMax - h * 0.5f);
    //                finalPos = new Vector2(rightZone.xMin, y);
    //            }
    //            else if (chosen.side == "above")
    //            {
    //                // Place with pivot at BOTTOM-CENTER so the panel grows upward
    //                pivot = new Vector2(0.5f, 0f);

    //                float x = Mathf.Clamp(wordCenterLocal.x, topZone.xMin + w * 0.5f, topZone.xMax - w * 0.5f);
    //                finalPos = new Vector2(x, topZone.yMin);
    //            }
    //            else // "below"
    //            {
    //                // Place with pivot at TOP-CENTER so the panel grows downward
    //                pivot = new Vector2(0.5f, 1f);

    //                float x = Mathf.Clamp(wordCenterLocal.x, bottomZone.xMin + w * 0.5f, bottomZone.xMax - w * 0.5f);
    //                finalPos = new Vector2(x, bottomZone.yMax);
    //            }

    //            // Apply anchors & pivot in the overlay
    //            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
    //            rt.pivot = pivot;
    //            rt.anchoredPosition = finalPos;

    //            // ================== FINAL CLAMP inside container (safety padding) ==================
    //            float pad = 8f;

    //            // Build the panel rect in CalloutLayer local after placement
    //            Rect PanelLocalRect(Vector2 pos, Vector2 pv, float W, float H)
    //            {
    //                float xMin = pos.x - pv.x * W;
    //                float yMin = pos.y - pv.y * H;
    //                return new Rect(xMin, yMin, W, H);
    //            }

    //            Rect placed = PanelLocalRect(rt.anchoredPosition, rt.pivot, w, h);
    //            Rect container = new Rect(CL, CB, CR - CL, CT - CB);

    //            // Compute delta needed to keep panel fully inside container
    //            float dx = 0f, dy = 0f;
    //            if (placed.xMin < container.xMin + pad) dx = (container.xMin + pad) - placed.xMin;
    //            if (placed.xMax > container.xMax - pad) dx = (container.xMax - pad) - placed.xMax;
    //            if (placed.yMin < container.yMin + pad) dy = (container.yMin + pad) - placed.yMin;
    //            if (placed.yMax > container.yMax - pad) dy = (container.yMax - pad) - placed.yMax;

    //            // Apply clamp
    //            rt.anchoredPosition += new Vector2(dx, dy);

    //            // Log
    //            Debug.Log($"[PLACE] chosen={chosen.side} | zones fit L/R/T/B=({fitLeft},{fitRight},{fitTop},{fitBottom}) | " +
    //                      $"size=({w:0}x{h:0}) | pos=({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0}) pivot={rt.pivot}");



    //            //               // Spawn the callout panel (simple API)
    //            //               callout.Clear();
    //            //               callout.ShowCalloutSmart(
    //            //    "Step 1",
    //            //    $"Click {targetText}",
    //            //    panelPos,   // absolute pos in CalloutLayer local space
    //            //    260f,       // width
    //            //    120f        // height
    //            //);

    //            //               Debug.Log($"[OCR] side={side} panelPos=({panelPos.x:0},{panelPos.y:0}) wordUI=({uiRect.center.x:0},{uiRect.center.y:0})");
    //            //           },
    //            //           err =>
    //            //           {
    //            //               Debug.LogError(err);
    //            //               overlay.Clear();
    //            //               callout.Clear();
    //            //           }
    //            //       ));
    //        }));
    //}

    void Start()
    {
        var tex = screenshotImage.texture as Texture2D;
        if (tex == null)
        {
            Debug.LogError("ScreenshotImage has no Texture2D. Assign a texture before running OCR.");
            return;
        }

        StartCoroutine(ocrClient.Run(
            tex,
            resp =>
            {
                Debug.Log($"[OCR] words={resp.words.Length}, image={resp.width}x{resp.height}");

                // 1) Show ALL OCR boxes (red) so you can see detections
                overlay.Clear();
                overlay.Render(resp);

                // 2) Pick best match against targetText
                var best = resp.words
                    .Select(w => new { w, score = Score(w.text, targetText) })
                    .OrderByDescending(p => p.score)
                    .FirstOrDefault();

                if (best == null)
                {
                    Debug.LogWarning("[OCR] No words returned by OCR; cannot draw highlight.");
                    callout.Clear();
                    return;
                }

                // ✅ Always draw the green box for the best candidate (debug)
                var rectPx = new Rect(best.w.x, best.w.y, best.w.w, best.w.h);
                overlay.RenderSingleBox(
                    screenshotRT,
                    new Vector2(resp.width, resp.height),
                    rectPx,
                    new Color(0f, 1f, 0f, 0.55f)
                );
                Debug.Log($"[OCR] best='{best.w.text}' score={best.score:0.00} — drew green box");

                // If score is low, SKIP only the panel; keep the green box visible
                if (best.score < minScore)
                {
                    Debug.LogWarning($"[OCR] Best score {best.score:0.00} < minScore {minScore:0.00}. Showing green box for debug, skipping callout.");
                    callout.Clear();
                    return;
                }

                // Word rect in ScreenshotImage local space
                Rect uiRect = ImageUiMapper.ImageRectPxToLocalRect(
                    rectPx, screenshotRT, new Vector2(resp.width, resp.height)
                );
                DrawGreenBoxDirect(uiRect);

                // ================== REFS / GEOMETRY ==================
                // A. screenshot bounds in CalloutLayer local
                Vector3[] sc = new Vector3[4];
                screenshotRT.GetWorldCorners(sc);
                Vector2 sBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[0]); // bottom-left
                Vector2 sTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[2]); // top-right
                float L = sBL.x, B = sBL.y, R = sTR.x, T = sTR.y;

                // B. container bounds = parent of CalloutLayer (Panel or Canvas)
                RectTransform containerRT = (RectTransform)callout.CalloutLayer.parent;
                Vector3[] cc = new Vector3[4];
                containerRT.GetWorldCorners(cc);
                Vector2 cBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[0]);
                Vector2 cTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[2]);
                float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

                // C. word center in CalloutLayer local
                Vector2 wordCenterLocal = (Vector2)callout.CalloutLayer.InverseTransformPoint(
                    screenshotRT.TransformPoint(uiRect.center)
                );

                // ================== SPAWN & MEASURE (dynamic panel) ==================
                float margin = 20f;

                callout.Clear();
                var rt = callout.ShowCalloutSmart("Step 1", $"Click {targetText}", Vector2.zero, 260f, 120f);
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

                // --- bound panel width so it fits above the screenshot ---
                float pad = 8f;
                float screenshotWidth = R - L;
                float containerWidth = CR - CL;
                float maxPanelWidth = Mathf.Min(300f, screenshotWidth - 2f * pad, containerWidth - 2f * pad);

                var le = rt.GetComponent<UnityEngine.UI.LayoutElement>() ?? rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
                le.preferredWidth = maxPanelWidth;
                le.flexibleWidth = 0;

                var csf = rt.GetComponent<UnityEngine.UI.ContentSizeFitter>();
                if (csf)
                {
                    csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                    csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
                }

                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                float w = rt.rect.width, h = rt.rect.height;

                // ================== ALWAYS ABOVE + CENTERED ==================
                // bottom-center pivot so it grows upward
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);

                // center X at screenshot center, clamp if needed
                float x = (L + R) * 0.5f;
                if (x - w * 0.5f < L + pad) x = L + pad + w * 0.5f;
                if (x + w * 0.5f > R - pad) x = R - pad - w * 0.5f;

                // Y just above the screenshot
                Vector2 finalPos = new Vector2(x, T + margin);
                rt.anchoredPosition = finalPos;

                // ================== FINAL CONTAINER CLAMP ==================
                Rect PanelLocalRect(Vector2 pos, Vector2 pv, float W, float H)
                {
                    float xMin = pos.x - pv.x * W;
                    float yMin = pos.y - pv.y * H;
                    return new Rect(xMin, yMin, W, H);
                }
                Rect placed = PanelLocalRect(rt.anchoredPosition, rt.pivot, w, h);
                Rect container = new Rect(CL, CB, CR - CL, CT - CB);

                float dx = 0f, dy = 0f;
                if (placed.xMin < container.xMin + pad) dx = (container.xMin + pad) - placed.xMin;
                if (placed.xMax > container.xMax - pad) dx = (container.xMax - pad) - placed.xMax;
                if (placed.yMin < container.yMin + pad) dy = (container.yMin + pad) - placed.yMin;
                if (placed.yMax > container.yMax - pad) dy = (container.yMax - pad) - placed.yMax;

                rt.anchoredPosition += new Vector2(dx, dy);

                Debug.Log($"[PLACE] AboveCenteredOverScreenshot | size=({w:0}x{h:0}) | pos=({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0})");
            },
            err =>
            {
                Debug.LogError(err);
                overlay.Clear();
                callout.Clear();
            }
        ));
    }

    void DrawGreenBoxDirect(Rect uiRect)
    {
        // make a solid green UI rect directly under ScreenshotImage to prove coordinates are correct
        var go = new GameObject("DEBUG_GreenBox", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(screenshotRT, false);

        // local coords match what ImageUiMapper returned
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0f);

        rt.sizeDelta = new Vector2(uiRect.width, uiRect.height);
        rt.anchoredPosition = new Vector2(uiRect.x, uiRect.y);

        var img = go.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0f, 1f, 0f, 0.35f); // semi-transparent fill

        go.transform.SetAsLastSibling(); // ensure it renders above the RawImage
    }

    float Score(string a, string b)
    {
        a = (a ?? "").ToLowerInvariant();
        b = (b ?? "").ToLowerInvariant();
        if (a.Length == 0 || b.Length == 0) return 0f;
        float contains = (a.Contains(b) || b.Contains(a)) ? 1f : 0f;
        float lenSim = 1f - Mathf.Abs(a.Length - b.Length) / (float)Mathf.Max(a.Length, b.Length);
        return 0.7f * contains + 0.3f * lenSim;
    }
}


//using System.Linq;
//using UnityEngine;
//using UnityEngine.UI;
//using UnityEngine.UIElements;

//public class OcrHighlighter : MonoBehaviour
//{
//    [Header("Refs")]
//    public EasyOcrClient ocrClient;
//    public RawImage screenshotImage;        // ScreenshotImage
//    public RectTransform screenshotRT;      // ScreenshotImage RectTransform
//    public OcrOverlay overlay;              // has rawImageRT + overlayRoot assigned
//    public CalloutOverlay callout;          // simple version (ShowCallout(x,y,w,h))

//    [Header("Target")]
//    public string targetText = "Data";
//    [Range(0f, 1f)] public float minScore = 0.6f;

//    void Start()
//    {
//        var tex = screenshotImage.texture as Texture2D;
//        if (tex == null)
//        {
//            Debug.LogError("ScreenshotImage has no Texture2D. Assign a texture before running OCR.");
//            return;
//        }

//        StartCoroutine(ocrClient.Run(
//            tex,
//            resp =>
//            {
//                Debug.Log($"[OCR] words={resp.words.Length}, image={resp.width}x{resp.height}");

//                // 1) Show ALL OCR boxes (red) so you can see detections
//                overlay.Clear();
//                overlay.Render(resp);

//                // 2) Pick best match against targetText
//                var best = resp.words
//                    .Select(w => new { w, score = Score(w.text, targetText) })
//                    .OrderByDescending(p => p.score)
//                    .FirstOrDefault();

//                if (best == null)
//                {
//                    Debug.LogWarning("[OCR] No words returned by OCR; cannot draw highlight.");
//                    callout.Clear();
//                    return;
//                }

//                // ✅ Always draw the green box for the best candidate (debug visibility first)
//                var rectPx = new Rect(best.w.x, best.w.y, best.w.w, best.w.h);
//                overlay.RenderSingleBox(
//                    screenshotRT,
//                    new Vector2(resp.width, resp.height),
//                    rectPx,
//                    new Color(0f, 1f, 0f, 0.55f)
//                );
//                Debug.Log($"[OCR] best='{best.w.text}' score={best.score:0.00} — drew green box");

//                // If score is low, SKIP only the panel; keep the green box visible
//                if (best.score < minScore)
//                {
//                    Debug.LogWarning($"[OCR] Best score {best.score:0.00} < minScore {minScore:0.00}. Showing green box for debug, skipping callout.");
//                    callout.Clear();
//                    return;
//                }

//                // Word rect in ScreenshotImage local space
//                Rect uiRect = ImageUiMapper.ImageRectPxToLocalRect(
//                    rectPx, screenshotRT, new Vector2(resp.width, resp.height)
//                );
//                DrawGreenBoxDirect(uiRect);

//                // ================== REFS / GEOMETRY ==================

//                // ================== GEOMETRY FOR WORLD-SPACE POSITIONING ==================
//                // A. screenshot bounds in CalloutLayer local (for word center calculation)
//                Vector3[] sc = new Vector3[4];
//                screenshotRT.GetWorldCorners(sc);
//                Vector2 sBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[0]); // bottom-left
//                Vector2 sTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(sc[2]); // top-right
//                float L = sBL.x, B = sBL.y, R = sTR.x, T = sTR.y;

//                // B. container bounds (the canvas/panel containing everything)
//                RectTransform containerRT = (RectTransform)callout.CalloutLayer.parent;
//                Vector3[] cc = new Vector3[4];
//                containerRT.GetWorldCorners(cc);
//                Vector2 cBL = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[0]);
//                Vector2 cTR = (Vector2)callout.CalloutLayer.InverseTransformPoint(cc[2]);
//                float CL = cBL.x, CB = cBL.y, CR = cTR.x, CT = cTR.y;

//                // C. word center in CalloutLayer local
//                Vector2 wordCenterLocal = (Vector2)callout.CalloutLayer.InverseTransformPoint(
//                    screenshotRT.TransformPoint(uiRect.center)
//                );

//                // ================== SPAWN & MEASURE (dynamic panel) ==================
//                float margin = 20f;

//                // Create panel first to measure its size
//                callout.Clear();
//                var rt = callout.ShowCalloutSmart("Step 1", $"Click {targetText}", Vector2.zero, 260f, 120f);

//                // Force layout rebuild to get accurate measurements
//                Canvas.ForceUpdateCanvases();
//                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

//                // Hard-cap width so long text wraps (prevents oversized panels)
//                float maxW = Mathf.Min(300f, 400f);  // Fixed max width since we're going outside
//                var le = rt.GetComponent<UnityEngine.UI.LayoutElement>() ?? rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
//                le.preferredWidth = maxW;
//                le.flexibleWidth = 0;

//                var csf = rt.GetComponent<UnityEngine.UI.ContentSizeFitter>();
//                if (csf)
//                {
//                    csf.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
//                    csf.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
//                }

//                // Force another layout update after size constraints
//                Canvas.ForceUpdateCanvases();
//                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

//                float w = rt.rect.width, h = rt.rect.height;
//                Debug.Log($"[PANEL] Final measured size: {w:0}x{h:0}");

//                // ================== WORLD-SPACE ZONES (outside container) ==================
//                // These zones are OUTSIDE the entire container, not just the screenshot
//                Rect leftWorldZone = new Rect(CL - 400f - margin, CB, 400f, CT - CB);     // 400px wide zone to the left
//                Rect rightWorldZone = new Rect(CR + margin, CB, 400f, CT - CB);           // 400px wide zone to the right  
//                Rect topWorldZone = new Rect(CL, CT + margin, CR - CL, 300f);             // 300px tall zone above
//                Rect bottomWorldZone = new Rect(CL, CB - 300f - margin, CR - CL, 300f);   // 300px tall zone below

//                // All world zones should always fit since we control their size
//                bool fitLeft = leftWorldZone.width >= w && leftWorldZone.height >= h;
//                bool fitRight = rightWorldZone.width >= w && rightWorldZone.height >= h;
//                bool fitTop = topWorldZone.width >= w && topWorldZone.height >= h;
//                bool fitBottom = bottomWorldZone.width >= w && bottomWorldZone.height >= h;

//                Debug.Log($"[WORLD-ZONES] L:{leftWorldZone} fits:{fitLeft} | R:{rightWorldZone} fits:{fitRight} | T:{topWorldZone} fits:{fitTop} | B:{bottomWorldZone} fits:{fitBottom}");

//                // ================== PICK NEAREST FITTING ZONE (prioritize left for left-side words) ==================
//                float distLeft = Mathf.Abs(wordCenterLocal.x - leftWorldZone.center.x);     // Distance to left zone center
//                float distRight = Mathf.Abs(wordCenterLocal.x - rightWorldZone.center.x);   // Distance to right zone center  
//                float distTop = Mathf.Abs(wordCenterLocal.y - topWorldZone.center.y);       // Distance to top zone center
//                float distBottom = Mathf.Abs(wordCenterLocal.y - bottomWorldZone.center.y); // Distance to bottom zone center

//                (string side, float d) chosen;
//                var candidates = new System.Collections.Generic.List<(string, float)>();
//                if (fitLeft) candidates.Add(("left", distLeft));
//                if (fitRight) candidates.Add(("right", distRight));
//                if (fitTop) candidates.Add(("above", distTop));
//                if (fitBottom) candidates.Add(("below", distBottom));

//                if (candidates.Count == 0)
//                {
//                    // Should never happen with our fixed world zones, but fallback to left
//                    Debug.LogWarning("[PANEL] No world zones fit! Forcing left placement.");
//                    chosen = ("left", 0f);
//                }
//                else
//                {
//                    // For words in left half of screenshot, strongly prefer left placement
//                    float screenCenterX = (L + R) * 0.5f;
//                    if (wordCenterLocal.x < screenCenterX && fitLeft)
//                    {
//                        chosen = ("left", distLeft);
//                        Debug.Log($"[LOGIC] Word in left half ({wordCenterLocal.x:0} < {screenCenterX:0}), forcing left placement");
//                    }
//                    else
//                    {
//                        candidates.Sort((a, b) => a.Item2.CompareTo(b.Item2));
//                        chosen = candidates[0];
//                    }
//                }

//                // ================== FINAL POSITION (outside container in world space) ==================
//                Vector2 finalPos;
//                Vector2 pivot;

//                if (chosen.side == "left")
//                {
//                    // Place with pivot at RIGHT-MIDDLE so the panel grows to the left
//                    pivot = new Vector2(1f, 0.5f);
//                    float y = Mathf.Clamp(wordCenterLocal.y, CB + h * 0.5f, CT - h * 0.5f);
//                    finalPos = new Vector2(leftWorldZone.xMax, y);
//                }
//                else if (chosen.side == "right")
//                {
//                    // Place with pivot at LEFT-MIDDLE so the panel grows to the right
//                    pivot = new Vector2(0f, 0.5f);
//                    float y = Mathf.Clamp(wordCenterLocal.y, CB + h * 0.5f, CT - h * 0.5f);
//                    finalPos = new Vector2(rightWorldZone.xMin, y);
//                }
//                else if (chosen.side == "above")
//                {
//                    // Place with pivot at BOTTOM-CENTER so the panel grows upward
//                    pivot = new Vector2(0.5f, 0f);
//                    float x = Mathf.Clamp(wordCenterLocal.x, CL + w * 0.5f, CR - w * 0.5f);
//                    finalPos = new Vector2(x, topWorldZone.yMin);
//                }
//                else // "below"
//                {
//                    // Place with pivot at TOP-CENTER so the panel grows downward
//                    pivot = new Vector2(0.5f, 1f);
//                    float x = Mathf.Clamp(wordCenterLocal.x, CL + w * 0.5f, CR - w * 0.5f);
//                    finalPos = new Vector2(x, bottomWorldZone.yMax);
//                }

//                // Apply anchors & pivot
//                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
//                rt.pivot = pivot;
//                rt.anchoredPosition = finalPos;

//                // ================== NO CLAMP NEEDED (positioning outside container) ==================
//                // Since we're positioning outside the container, no clamping needed

//                // Enhanced logging
//                Debug.Log($"[WORLD-PLACE] chosen={chosen.side} distance={chosen.d:0.0} | world zones fit L/R/T/B=({fitLeft},{fitRight},{fitTop},{fitBottom})");
//                Debug.Log($"[WORLD-PLACE] panel size=({w:0}x{h:0}) | final pos=({rt.anchoredPosition.x:0},{rt.anchoredPosition.y:0}) pivot={rt.pivot}");
//                Debug.Log($"[WORLD-PLACE] word center in CalloutLayer: ({wordCenterLocal.x:0},{wordCenterLocal.y:0})");
//                Debug.Log($"[WORLD-PLACE] container bounds in CalloutLayer: CL={CL:0} CR={CR:0} CB={CB:0} CT={CT:0}");
//                Debug.Log($"[WORLD-PLACE] leftWorldZone: {leftWorldZone} | rightWorldZone: {rightWorldZone}");
//            },
//            err =>
//            {
//                Debug.LogError(err);
//                overlay.Clear();
//                callout.Clear();
//            }
//        ));
//    }

//    void DrawGreenBoxDirect(Rect uiRect)
//    {
//        // make a solid green UI rect directly under ScreenshotImage to prove coordinates are correct
//        var go = new GameObject("DEBUG_GreenBox", typeof(RectTransform), typeof(UnityEngine.UI.Image));
//        var rt = go.GetComponent<RectTransform>();
//        rt.SetParent(screenshotRT, false);

//        // local coords match what ImageUiMapper returned
//        rt.anchorMin = Vector2.zero;
//        rt.anchorMax = Vector2.zero;
//        rt.pivot = new Vector2(0f, 0f);

//        rt.sizeDelta = new Vector2(uiRect.width, uiRect.height);
//        rt.anchoredPosition = new Vector2(uiRect.x, uiRect.y);

//        var img = go.GetComponent<UnityEngine.UI.Image>();
//        img.color = new Color(0f, 1f, 0f, 0.35f); // semi-transparent fill

//        go.transform.SetAsLastSibling(); // ensure it renders above the RawImage
//    }

//    float Score(string a, string b)
//    {
//        a = (a ?? "").ToLowerInvariant();
//        b = (b ?? "").ToLowerInvariant();
//        if (a.Length == 0 || b.Length == 0) return 0f;
//        float contains = (a.Contains(b) || b.Contains(a)) ? 1f : 0f;
//        float lenSim = 1f - Mathf.Abs(a.Length - b.Length) / (float)Mathf.Max(a.Length, b.Length);
//        return 0.7f * contains + 0.3f * lenSim;
//    }
//}