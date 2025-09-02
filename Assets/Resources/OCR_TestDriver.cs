using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OCR_TestDriver : MonoBehaviour
{
    [Header("Assign (Scene)")]
    public EasyOcrClient ocrClient;           // your EasyOcrClient in scene
    public AR2_OcrOverlay overlay;            // overlay on top of the RawImage
    public RawImage screenshotImage;          // RawImage that displays the test image

    [Header("Test Image")]
    [Tooltip("Optional: assign directly in Inspector. If null, will load Resources/testpic.png")]
    public Texture2D testTexture;

    [Header("Debug")]
    public bool drawAllOcrBoxes = true;
    public bool drawImageBounds = true;

    [Header("Auto Fit")]
    [Tooltip("If present, will auto-set aspect ratio from OCR response")]
    public AspectRatioFitter aspectRatioFitter;

    void Start()
    {
        if (!ocrClient || !overlay || !screenshotImage)
        {
            Debug.LogError("[OCR_Test] Please assign ocrClient, overlay, screenshotImage.");
            return;
        }

        // 1) Choose test texture
        var tex = testTexture;
        if (tex == null)
        {
            tex = Resources.Load<Texture2D>("testpic"); // Assets/Resources/testpic.png
            if (tex == null)
            {
                Debug.LogError("[OCR_Test] No test image. Assign 'testTexture' or put Assets/Resources/testpic.png");
                return;
            }
        }

        // 2) Show it
        screenshotImage.texture = tex;
        screenshotImage.uvRect = new Rect(0, 0, 1, 1);

        // 3) Kick OCR
        StartCoroutine(ocrClient.Run(
            tex,
            OnOcrOk,
            err => Debug.LogError("[OCR_Test] " + err)
        ));
    }

    void OnOcrOk(OcrResponse resp)
    {
        if (resp == null)
        {
            Debug.LogError("[OCR_Test] OCR response is null.");
            return;
        }

        // Optional: sync AspectRatioFitter to OCR image aspect
        if (aspectRatioFitter)
        {
            aspectRatioFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspectRatioFitter.aspectRatio = Mathf.Max(0.0001f, (float)resp.width / Mathf.Max(1, resp.height));
        }

        // Let layout settle one frame before drawing boxes
        StartCoroutine(DeferredDraw(resp));
    }

    IEnumerator DeferredDraw(OcrResponse resp)
    {
        yield return null;                   // wait one frame
        yield return new WaitForEndOfFrame();// after layout/ARF

        overlay.Clear();
        overlay.showImageBounds = drawImageBounds;
        overlay.DrawImageBounds(resp.width, resp.height);

        if (drawAllOcrBoxes)
        {
            overlay.showDebugBoxes = true;
            overlay.DrawAllOcrBoxes(resp);
        }

        // Also draw the best match for a hardcoded keyword (optional demo)
        var best = FindFirst(resp, "Scenes");
        if (best != null) overlay.DrawBox(best, resp, Color.green);

        Debug.Log($"[OCR_Test] Drawn {resp.words?.Length ?? 0} boxes. OCR image = {resp.width}x{resp.height}");
    }

    // Tiny helper: exact/contains match for a single keyword
    OcrItem FindFirst(OcrResponse resp, string keyword)
    {
        if (resp?.words == null || string.IsNullOrWhiteSpace(keyword)) return null;
        string k = keyword.ToLowerInvariant().Trim();
        foreach (var w in resp.words)
        {
            var t = (w.text ?? "").ToLowerInvariant();
            if (t == k || t.Contains(k) || k.Contains(t)) return w;
        }
        return null;
    }

    // Optional: call this from a UI Button to re-run OCR without reloading scene
    public void Rerun()
    {
        var tex = screenshotImage.texture as Texture2D;
        if (!tex) { Debug.LogError("[OCR_Test] No Texture2D on RawImage."); return; }

        StartCoroutine(ocrClient.Run(
            tex,
            OnOcrOk,
            err => Debug.LogError("[OCR_Test] " + err)
        ));
    }
}
