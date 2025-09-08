using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class AR2_OcrOverlay : MonoBehaviour
{
    [Header("Assign")]
    public RawImage screenshotImage;
    public RectTransform overlayLayer;
    public Image boxPrefab;

    [Header("Debug")]
    public bool debugLayout = true;
    public bool showDebugBoxes = false;
    public bool showImageBounds = false;
    public Color debugBoxColor = new Color(1, 0, 0, 0.1f);
    public Color imageBoundsColor = new Color(0, 1, 0, 0.3f);

    [Header("Mapping")]
    public bool ocrYTopLeft = true;

    [Header("Calibration")]
    public Vector2 pixelNudge = Vector2.zero;   // UI pixels in the overlay's (center-anchored) space
    public Vector2 sizeNudge = Vector2.zero;

    readonly List<Image> pool = new();

    void Awake()
    {
        if (overlayLayer == null) overlayLayer = GetComponent<RectTransform>();

        if (screenshotImage && overlayLayer.parent != screenshotImage.transform)
            overlayLayer.SetParent(screenshotImage.transform, false);

        StretchOverlayToParent();
    }

    void Start()
    {
        if (screenshotImage && screenshotImage.texture == null && GuideRunContext.I?.screenshot != null)
            screenshotImage.texture = GuideRunContext.I.screenshot;

        StartCoroutine(PostLayoutSync());
    }

    IEnumerator PostLayoutSync()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        StretchOverlayToParent();

        var ri = screenshotImage.rectTransform.rect;
        var ov = overlayLayer.rect;
        Debug.Log($"[PostLayout] RawImage={ri.width:0.##}x{ri.height:0.##}  Overlay={ov.width:0.##}x{ov.height:0.##}");
    }


    void StretchOverlayToParent()
    {
        if (!overlayLayer || !screenshotImage) return;
        overlayLayer.anchorMin = Vector2.zero;
        overlayLayer.anchorMax = Vector2.one;
        overlayLayer.pivot = new Vector2(0.5f, 0.5f);
        overlayLayer.anchoredPosition = Vector2.zero;
        overlayLayer.sizeDelta = Vector2.zero;
    }

    public void Clear()
    {
        foreach (var img in pool) img.gameObject.SetActive(false);
    }

    public void DrawBox(OcrItem w, OcrResponse resp, Color color)
    {
        if (w == null || resp == null || screenshotImage == null || overlayLayer == null) return;

        Rect ui = MapOcrRectToOverlay(new Rect(w.x, w.y, w.w, w.h), resp.width, resp.height);
        var img = GetBox();
        img.color = new Color(color.r, color.g, color.b, 0.22f);

        img.rectTransform.anchoredPosition = ui.center;
        img.rectTransform.sizeDelta = ui.size;
        img.rectTransform.SetAsLastSibling();

        if (debugLayout)
        {
            Debug.Log($"[AR2_OcrOverlay] Drew box: OCR({w.x},{w.y},{w.w},{w.h}) -> UI({ui.x},{ui.y},{ui.width},{ui.height})");
        }
    }

    Image GetBox()
    {
        foreach (var i in pool) if (!i.gameObject.activeSelf) { i.gameObject.SetActive(true); return i; }
        var inst = Instantiate(boxPrefab, overlayLayer);

        var rt = inst.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        pool.Add(inst);
        inst.gameObject.SetActive(true);
        return inst;
    }

    // FIXED: Calculate the actual displayed image area within the RawImage container
    Rect GetDisplayedImageRect(int ocrW, int ocrH)
    {
        var container = overlayLayer.rect;  // use overlay’s rect (final drawing space)

        float imgAspect = (float)ocrW / Mathf.Max(1, ocrH);
        float contAspect = container.width / Mathf.Max(1f, container.height);

        float w, h, offX = 0f, offY = 0f;
        if (imgAspect > contAspect) { w = container.width; h = container.width / imgAspect; offY = (container.height - h) * 0.5f; }
        else { h = container.height; w = container.height * imgAspect; offX = (container.width - w) * 0.5f; }
        return new Rect(offX, offY, w, h);
    }

    // --- REPLACE THIS METHOD IN AR2_OcrOverlay.cs ---
    Rect MapOcrRectToOverlay(Rect ocrRectPx, int ocrW, int ocrH)
    {
        // Get the overlay's rect (this is our drawing canvas)
        var overlayRect = overlayLayer.rect;

        // Simple direct mapping - assume RawImage fills the entire overlay area
        float scaleX = overlayRect.width / Mathf.Max(1f, ocrW);
        float scaleY = overlayRect.height / Mathf.Max(1f, ocrH);

        // Scale OCR coordinates to UI coordinates
        float uiX = ocrRectPx.x * scaleX;
        float uiY = ocrRectPx.y * scaleY;
        float uiW = ocrRectPx.width * scaleX;
        float uiH = ocrRectPx.height * scaleY;

        // Handle Y-coordinate system (OCR typically uses top-left origin, UI uses bottom-left)
        if (ocrYTopLeft)
        {
            // Flip Y coordinate: convert from top-left to bottom-left origin
            uiY = overlayRect.height - uiY - uiH;
        }

        // Convert from bottom-left position to center-anchored position
        // (since your overlay uses center pivot)
        float centerX = uiX + uiW * 0.5f;
        float centerY = uiY + uiH * 0.5f;

        // Convert to anchoredPosition relative to overlay center
        float ax = centerX - overlayRect.width * 0.5f;
        float ay = centerY - overlayRect.height * 0.5f;

        // Apply manual calibration nudges
        ax += pixelNudge.x;
        ay += pixelNudge.y;
        uiW += sizeNudge.x;
        uiH += sizeNudge.y;

        return new Rect(ax, ay, uiW, uiH);
    }


    // --- ADD THIS OPTIONAL CALIBRATION VISUALIZER ---
    public void DrawCalibrationCross(int ocrW, int ocrH)
    {
        var corners = new[]
        {
        new Rect(0, 0, 120, 40),                      // TL (in OCR px, y=top if ocrYTopLeft=true)
        new Rect(ocrW - 120, 0, 120, 40),             // TR
        new Rect(0, ocrH - 40, 120, 40),              // BL
        new Rect(ocrW - 120, ocrH - 40, 120, 40)      // BR
    };

        foreach (var r in corners)
        {
            var ui = MapOcrRectToOverlay(r, ocrW, ocrH);
            var img = GetBox();
            img.color = new Color(0, 0.6f, 1f, 0.25f);
            img.rectTransform.anchoredPosition = ui.center;
            img.rectTransform.sizeDelta = ui.size;
            img.rectTransform.SetAsLastSibling();
        }
    }


    void LoadNudge()
    {
        string key = SystemInfo.deviceModel + "_ocr_nudge";
        if (PlayerPrefs.HasKey(key))
        {
            var s = PlayerPrefs.GetString(key).Split(',');
            if (s.Length == 2 && float.TryParse(s[0], out var nx) && float.TryParse(s[1], out var ny))
                pixelNudge = new Vector2(nx, ny);
        }
    }
    void SaveNudge()
    {
        string key = SystemInfo.deviceModel + "_ocr_nudge";
        PlayerPrefs.SetString(key, $"{pixelNudge.x},{pixelNudge.y}");
        PlayerPrefs.Save();
    }
    public void DrawAllOcrBoxes(OcrResponse resp)
    {
        if (!showDebugBoxes || resp?.words == null) return;

        foreach (var word in resp.words)
        {
            DrawBox(word, resp, debugBoxColor);
        }

        Debug.Log($"[AR2_OcrOverlay] Drew {resp.words.Length} debug OCR boxes");
    }

    public void DrawImageBounds(int ocrW, int ocrH)
    {
        if (!showImageBounds || !screenshotImage) return;

        var container = screenshotImage.rectTransform.rect;
        var displayedRect = GetDisplayedImageRect(ocrW, ocrH);

        var img = GetBox();
        img.color = imageBoundsColor;

        float centerX = displayedRect.x + displayedRect.width * 0.5f - container.width * 0.5f;
        float centerY = displayedRect.y + displayedRect.height * 0.5f - container.height * 0.5f;

        img.rectTransform.anchoredPosition = new Vector2(centerX, centerY);
        img.rectTransform.sizeDelta = new Vector2(displayedRect.width, displayedRect.height);
        img.rectTransform.SetAsFirstSibling();

        Debug.Log($"[ImageBounds] Container({container.width:F1}x{container.height:F1}) " +
                  $"Display({displayedRect.width:F1}x{displayedRect.height:F1}) " +
                  $"Offset({displayedRect.x:F1},{displayedRect.y:F1})");
    }
}