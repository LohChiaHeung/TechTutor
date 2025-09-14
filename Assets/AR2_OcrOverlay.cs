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
    public bool coordsAreNormalized = false;

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
            Debug.Log($"[AR2_OcrOverlay] Drew box: OCR({w.x},{w.y},{w.w},{w.h}) -> UI({ui.x:F1},{ui.y:F1},{ui.width:F1},{ui.height:F1})");
        }
    }

    Image GetBox()
    {
        foreach (var i in pool) if (!i.gameObject.activeSelf) { i.gameObject.SetActive(true); return i; }
        var inst = Instantiate(boxPrefab, overlayLayer);

        var rt = inst.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);

        pool.Add(inst);
        inst.gameObject.SetActive(true);
        return inst;
    }

    // FIXED: Calculate the actual displayed image area considering AspectRatioFitter
    Rect GetDisplayedImageRect(int ocrW, int ocrH)
    {
        // Use the RawImage's actual rect (after AspectRatioFitter has done its work)
        var rawImageRect = screenshotImage.rectTransform.rect;

        // Get the AspectRatioFitter if it exists
        var arf = screenshotImage.GetComponent<AspectRatioFitter>();

        if (arf != null && arf.enabled)
        {
            float imgAspect = (float)ocrW / Mathf.Max(1, ocrH);
            float containerAspect = rawImageRect.width / Mathf.Max(1f, rawImageRect.height);

            float displayedW, displayedH, offsetX = 0f, offsetY = 0f;

            switch (arf.aspectMode)
            {
                case AspectRatioFitter.AspectMode.FitInParent:
                    if (imgAspect > containerAspect)
                    {
                        // Image is wider - fit to container width, letterbox top/bottom
                        displayedW = rawImageRect.width;
                        displayedH = rawImageRect.width / imgAspect;
                        offsetY = (rawImageRect.height - displayedH) * 0.5f;
                    }
                    else
                    {
                        // Image is taller - fit to container height, letterbox left/right
                        displayedH = rawImageRect.height;
                        displayedW = rawImageRect.height * imgAspect;
                        offsetX = (rawImageRect.width - displayedW) * 0.5f;
                    }
                    break;

                case AspectRatioFitter.AspectMode.EnvelopeParent:
                    if (imgAspect > containerAspect)
                    {
                        displayedH = rawImageRect.height;
                        displayedW = rawImageRect.height * imgAspect;
                        offsetX = (rawImageRect.width - displayedW) * 0.5f;
                    }
                    else
                    {
                        displayedW = rawImageRect.width;
                        displayedH = rawImageRect.width / imgAspect;
                        offsetY = (rawImageRect.height - displayedH) * 0.5f;
                    }
                    break;

                default:
                    // No aspect ratio fitting or other modes - use full container
                    displayedW = rawImageRect.width;
                    displayedH = rawImageRect.height;
                    break;
            }

            return new Rect(offsetX, offsetY, displayedW, displayedH);
        }
        else
        {
            // No AspectRatioFitter - image fills entire container
            return new Rect(0, 0, rawImageRect.width, rawImageRect.height);
        }
    }

    // FIXED: Better coordinate mapping that accounts for AspectRatioFitter
    Rect MapOcrRectToOverlay(Rect ocrRect, int ocrW, int ocrH)
    {
        // 1) Get the actual displayed image area within the RawImage
        var displayRect = GetDisplayedImageRect(ocrW, ocrH);
        float dispX = displayRect.x;
        float dispY = displayRect.y;
        float dispW = Mathf.Max(1f, displayRect.width);
        float dispH = Mathf.Max(1f, displayRect.height);

        // 2) Interpret incoming OCR rect (either pixels or normalized)
        float srcX = ocrRect.x;
        float srcY = ocrRect.y;
        float srcW = Mathf.Max(0f, ocrRect.width);
        float srcH = Mathf.Max(0f, ocrRect.height);

        if (coordsAreNormalized)
        {
            // normalized [0..1] → pixels in OCR space
            srcX *= ocrW;
            srcY *= ocrH;
            srcW *= ocrW;
            srcH *= ocrH;
        }

        // 3) Scale OCR pixels → displayed-image pixels
        float scaleX = dispW / Mathf.Max(1f, ocrW);
        float scaleY = dispH / Mathf.Max(1f, ocrH);

        float uiX = dispX + srcX * scaleX;
        float uiY = dispY + srcY * scaleY;
        float uiW = srcW * scaleX;
        float uiH = srcH * scaleY;

        // 4) Handle Y coordinate system conversion
        if (ocrYTopLeft)
        {
            // OCR Y=0 is at top, Unity UI Y=0 is at bottom
            // Convert: ocrY → displayHeight - ocrY - ocrHeight
            uiY = dispY + dispH - (srcY + srcH) * scaleY;
        }

        // 5) Convert to center-anchored RectTransform coordinates
        var overlayRect = overlayLayer.rect;
        float centerX = uiX + uiW * 0.5f;
        float centerY = uiY + uiH * 0.5f;

        // Convert from bottom-left origin to center origin
        float anchoredX = uiX + pixelNudge.x;
        float anchoredY = -(uiY + uiH) + pixelNudge.y;

        if (debugLayout)
        {
            Debug.Log($"[MapOcr] OCR({srcX:F1},{srcY:F1},{srcW:F1},{srcH:F1}) " +
                     $"Display({dispX:F1},{dispY:F1},{dispW:F1},{dispH:F1}) " +
                     $"Scale({scaleX:F3},{scaleY:F3}) " +
                     $"UI({uiX:F1},{uiY:F1},{uiW:F1},{uiH:F1}) " +
                     $"Final({anchoredX:F1},{anchoredY:F1})");
        }

        return new Rect(anchoredX, anchoredY, uiW + sizeNudge.x, uiH + sizeNudge.y);
    }

    // Calibration helper - draws boxes at OCR corners to verify mapping
    public void DrawCalibrationCross(int ocrW, int ocrH)
    {
        var corners = new[]
        {
            new Rect(0, 0, 120, 40),                      // TL
            new Rect(ocrW - 120, 0, 120, 40),             // TR
            new Rect(0, ocrH - 40, 120, 40),              // BL
            new Rect(ocrW - 120, ocrH - 40, 120, 40)      // BR
        };

        foreach (var r in corners)
        {
            var ui = MapOcrRectToOverlay(r, ocrW, ocrH);
            var img = GetBox();
            img.color = new Color(0, 0.6f, 1f, 0.35f);
            img.rectTransform.anchoredPosition = ui.center;
            img.rectTransform.sizeDelta = ui.size;
            img.rectTransform.SetAsLastSibling();
        }

        Debug.Log($"[CalibrationCross] Drew corner boxes for OCR({ocrW}x{ocrH})");
    }

    // DEBUGGING: Call this to test if coordinates are mapping correctly
    public void TestCoordinateMapping(int ocrW, int ocrH)
    {
        Debug.Log("=== COORDINATE MAPPING TEST ===");
        Debug.Log($"OCR Image Size: {ocrW}x{ocrH}");
        Debug.Log($"RawImage Rect: {screenshotImage.rectTransform.rect}");
        Debug.Log($"RawImage uvRect: {screenshotImage.uvRect}");
        Debug.Log($"Overlay Rect: {overlayLayer.rect}");

        // Test a few key points
        var testPoints = new[]
        {
            new Rect(0, 0, 50, 50),           // Top-left corner
            new Rect(ocrW/2f-25, ocrH/2f-25, 50, 50), // Center
            new Rect(ocrW-50, ocrH-50, 50, 50) // Bottom-right corner
        };

        foreach (var testRect in testPoints)
        {
            var mapped = MapOcrRectToOverlay(testRect, ocrW, ocrH);
            Debug.Log($"OCR({testRect.x},{testRect.y},{testRect.width}x{testRect.height}) -> UI({mapped.x:F1},{mapped.y:F1},{mapped.width:F1}x{mapped.height:F1})");
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

        var displayedRect = GetDisplayedImageRect(ocrW, ocrH);
        var overlayRect = overlayLayer.rect;

        var img = GetBox();
        img.color = imageBoundsColor;

        // Center the bounds visualization
        float centerX = displayedRect.x + displayedRect.width * 0.5f - overlayRect.width * 0.5f;
        float centerY = displayedRect.y + displayedRect.height * 0.5f - overlayRect.height * 0.5f;

        img.rectTransform.anchoredPosition = new Vector2(centerX, centerY);
        img.rectTransform.sizeDelta = new Vector2(displayedRect.width, displayedRect.height);
        img.rectTransform.SetAsFirstSibling();

        Debug.Log($"[ImageBounds] RawImage({screenshotImage.rectTransform.rect.width:F1}x{screenshotImage.rectTransform.rect.height:F1}) " +
                  $"Display({displayedRect.width:F1}x{displayedRect.height:F1}) " +
                  $"Offset({displayedRect.x:F1},{displayedRect.y:F1})");
    }
}