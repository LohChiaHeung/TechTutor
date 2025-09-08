using UnityEngine;
using UnityEngine.UI;

public class UiBoxDrawer_New : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RawImage screenshotImage;    // the RawImage showing the screenshot
    public RectTransform overlay;       // BoxesOverlay rect (same size as ScreenshotImage parent viewport)
    public RectTransform boxPrefab;     // BoxPrefab_New (Image) -> RectTransform
    [Space]
    public Vector2 extraPixelOffset = Vector2.zero; // manual fine-tune if needed (applied in source-pixel space before scaling)

    /// <summary>
    /// Clear all existing boxes under overlay.
    /// </summary>
    public void ClearBoxes()
    {
        for (int i = overlay.childCount - 1; i >= 0; i--)
            DestroyImmediate(overlay.GetChild(i).gameObject);
    }

    /// <summary>
    /// Draw a box for a word whose rect is in ORIGINAL image pixel space (origin: top-left).
    /// </summary>
    public void DrawWordBox(int imgW, int imgH, float x, float y, float w, float h)
    {
        // 1) Compute how the image is LETTERBOXED inside the overlay rect
        var viewportRect = overlay.rect; // size of the panel (rw, rh)
        float rw = viewportRect.width;
        float rh = viewportRect.height;

        float scale = Mathf.Min(rw / imgW, rh / imgH);
        float dispW = imgW * scale;
        float dispH = imgH * scale;
        float offX = (rw - dispW) * 0.5f;
        float offY = (rh - dispH) * 0.5f;

        // 2) Convert from top-left origin to bottom-left origin used by UI
        // source y_top -> y_bottom = (imgH - (y + h))
        float srcX = x + extraPixelOffset.x;
        float srcY = y + extraPixelOffset.y;
        float srcW = w;
        float srcH = h;

        float yBottom = imgH - (srcY + srcH);

        // 3) Scale and offset into overlay local space
        float uiX = offX + srcX * scale;
        float uiY = offY + yBottom * scale;
        float uiW = srcW * scale;
        float uiH = srcH * scale;

        // 4) Instantiate box
        var box = Instantiate(boxPrefab, overlay);
        var rt = box as RectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0f); // bottom-left pivoting
        rt.pivot = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(uiX, uiY);
        rt.sizeDelta = new Vector2(uiW, uiH);
        box.gameObject.SetActive(true);
    }
}
