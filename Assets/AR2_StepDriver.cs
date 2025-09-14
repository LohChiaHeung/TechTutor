using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using static System.Net.Mime.MediaTypeNames;

public class AR2_StepDriver : MonoBehaviour
{
    [Header("Scene Refs")]
    public AR2_GuideBridge guide;
    public EasyOcrClient ocrClient;
    public AR2_OcrOverlay overlay;
    public RawImage screenshotImage;

    [Header("UI")]
    public TMP_Text stepTitle;
    public TMP_Text stepDesc;
    public Button prevButton;
    public Button nextButton;

    [Header("Match")]
    [Range(0.0f, 1.0f)] public float minScore = 0.55f;

    [Header("Debug")]
    public bool debugLogMatches = true;
    public bool debugDrawAll = false;
    public Color allBoxesColor = new Color(1, 0, 0, 0.08f); // translucent red

    [Header("Debug/Mode")]
    public bool useDisplayedTextureForOCR = true;

    OcrResponse _ocr;
    int _idx = 0;

    void Start()
    {
        if (!guide || !ocrClient || !overlay || !screenshotImage)
        {
            Debug.LogError("[AR2] Missing refs on AR2_StepDriver.");
            return;
        }

        // CRITICAL: Assign texture FIRST, before any overlay operations
        if (guide.screenshotOut != null)
        {
            screenshotImage.texture = guide.screenshotOut;
            // Ensure RawImage shows the whole texture and no weird scaling
            screenshotImage.uvRect = new Rect(0, 0, 1, 1);

            // Make sure overlay chain has clean scale = (1,1,1)
            if (overlay && overlay.overlayLayer)
            {
                overlay.overlayLayer.localScale = Vector3.one;
                var t = overlay.overlayLayer;
                for (var p = t.parent as RectTransform; p != null && p.GetComponent<Canvas>() == null; p = p.parent as RectTransform)
                    p.localScale = Vector3.one; // defensive: clear any accidental parent scaling
            }
            // force layout to settle (so overlay sees the final rect)
            LayoutRebuilder.ForceRebuildLayoutImmediate(screenshotImage.rectTransform);
            Canvas.ForceUpdateCanvases();
        }
        else
        {
            Debug.LogError("[AR2] No screenshot available in guide!");
            return;
        }


        // Force UI layout update so RawImage gets proper size
        Canvas.ForceUpdateCanvases();

        prevButton.onClick.AddListener(OnPrev);
        nextButton.onClick.AddListener(OnNext);

        Texture2D texForOCR = guide.screenshotOut; // default: original
        if (useDisplayedTextureForOCR)
        {
            // send exactly what RawImage is showing
            var tex2D = screenshotImage.texture as Texture2D;
            if (tex2D != null) texForOCR = tex2D;
            else Debug.LogError("[AR2] RawImage.texture is not a Texture2D.");
        }

        StartCoroutine(ocrClient.Run(
        texForOCR,
        OnOcrReady,
        err => { Debug.LogError("[AR2/OCR] " + err); RefreshUI(); }
    ));

    }
    void OnOcrReady(OcrResponse resp)
    {
        _ocr = resp;
        StartCoroutine(DeferredInitialDraw());
    }

    IEnumerator DeferredInitialDraw()
    {
        // Let ARF & layout groups finish resizing this frame
        yield return null;
        yield return new WaitForEndOfFrame ();

        LayoutRebuilder.ForceRebuildLayoutImmediate(screenshotImage.rectTransform);
        Canvas.ForceUpdateCanvases();

        Debug.Log($"[AR2] FINAL rect: RawImage={screenshotImage.rectTransform.rect.width}x{screenshotImage.rectTransform.rect.height}  " +
                  $"OCR={_ocr.width}x{_ocr.height}");

        overlay.Clear();
        overlay.showImageBounds = true;
        overlay.DrawImageBounds(_ocr.width, _ocr.height);
        overlay.TestCoordinateMapping(_ocr.width, _ocr.height);
        if (debugDrawAll) overlay.DrawAllOcrBoxes(_ocr);

        _idx = 0;
        RefreshUI();
    }



    void OnPrev() { if (_idx > 0) { _idx--; RefreshUI(); } }
    void OnNext() { if (_idx + 1 < guide.stepsOut.Length) { _idx++; RefreshUI(); } }

    void RefreshUI()
    {
        if (guide.stepsOut == null || guide.stepsOut.Length == 0) return;
        _idx = Mathf.Clamp(_idx, 0, guide.stepsOut.Length - 1);
        var s = guide.stepsOut[_idx];

        if (stepTitle) stepTitle.text = s.title ?? "";
        if (stepDesc) stepDesc.text = s.instruction ?? "";

        overlay.Clear();

        if (_ocr != null && _ocr.words != null && _ocr.words.Length > 0)
        {
            // Log keywords for this step
            if (debugLogMatches)
                Debug.Log($"[AR2] Step {_idx + 1}/{guide.stepsOut.Length} keywords=[{string.Join(", ", s.keywords ?? System.Array.Empty<string>())}]");

            // Find best + log
            var best = FindBestVerbose(s.keywords, _ocr, out string chosenKw, out float bestScore);
            if (best != null)
            {
                overlay.DrawBox(best, _ocr, Color.green);
                if (debugLogMatches)
                    Debug.Log($"[AR2] ✓ matched keyword \"{chosenKw}\" → OCR \"{best.text}\"  score={bestScore:0.00}  rect=({best.x},{best.y},{best.w},{best.h})");
            }
            else if (debugLogMatches)
            {
                Debug.Log($"[AR2] ✗ no match ≥ {minScore:0.00} for this step.");
            }
        }

        prevButton.interactable = _idx > 0;
        nextButton.interactable = _idx + 1 < guide.stepsOut.Length;
    }

    // ---- matching with logging ----
    OcrItem FindBestVerbose(string[] keywords, OcrResponse resp, out string chosenKw, out float bestScore)
    {
        chosenKw = null; bestScore = minScore;
        if (resp == null || resp.words == null || resp.words.Length == 0 || keywords == null) return null;

        OcrItem best = null;
        float bestWeighted = -1f;

        // image dims
        float W = Mathf.Max(1, resp.width);
        float H = Mathf.Max(1, resp.height);

        foreach (var kw in keywords)
        {
            if (string.IsNullOrWhiteSpace(kw)) continue;
            string k = kw.ToLowerInvariant().Trim();

            foreach (var w in resp.words)
            {
                string t = (w.text ?? "").ToLowerInvariant();

                // base text score
                float textScore =
                    (t == k) ? 1.0f :
                    (t.Contains(k) || k.Contains(t)) ? 0.85f : 0f;

                if (textScore < minScore) continue;

                // heuristics: prefer folder row (bigger, left, mid-height)
                float areaNorm = (w.w * w.h) / (W * H);                   // bigger is better
                float leftBias = 1f - Mathf.Clamp01((w.x + w.w * 0.5f) / W); // left side gets higher value
                float midY = 1f - Mathf.Abs(((w.y + w.h * 0.5f) / H) - 0.5f) * 2f; // center vertically

                // tune weights
                float weighted = textScore * 0.70f + areaNorm * 0.20f + leftBias * 0.07f + midY * 0.03f;

                if (weighted > bestWeighted)
                {
                    bestWeighted = weighted;
                    bestScore = textScore;
                    chosenKw = kw;
                    best = w;
                }
            }
        }
        return best;
    }


    float Similarity(string a, string b)
    {
        if (a == b) return 1f;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        if (b.Contains(a)) return 0.85f;
        if (a.Contains(b)) return 0.80f;
        return 0f;
    }
}
