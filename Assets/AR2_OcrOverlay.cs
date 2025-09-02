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

    Rect MapOcrRectToOverlay(Rect ocrRectPx, int ocrW, int ocrH)
    {
        var container = screenshotImage.rectTransform.rect;

        // Get the actual displayed image area (accounts for aspect ratio preservation)
        var displayedRect = GetDisplayedImageRect(ocrW, ocrH);

        // Calculate scaling factors based on displayed image, not container
        float sx = displayedRect.width / Mathf.Max(1, ocrW);
        float sy = displayedRect.height / Mathf.Max(1, ocrH);

        float uiX = ocrRectPx.x * sx;
        float uiY = ocrRectPx.y * sy;
        float uiW = ocrRectPx.width * sx;
        float uiH = ocrRectPx.height * sy;

        if (ocrYTopLeft) uiY = displayedRect.height - uiY - uiH;

        // Adjust for the displayed image's position within the container
        uiX += displayedRect.x;
        uiY += displayedRect.y;

        // Convert to centered coordinates
        float ax = uiX + uiW * 0.5f - container.width * 0.5f;
        float ay = uiY + uiH * 0.5f - container.height * 0.5f;

        return new Rect(ax, ay, uiW, uiH);
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