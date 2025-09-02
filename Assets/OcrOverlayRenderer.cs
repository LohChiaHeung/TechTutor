using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OcrOverlayRenderer : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public RawImage screenshotImage;        // The image that shows the OCR source (Texture2D)
    public RectTransform overlayLayer;      // Empty RectTransform on top of screenshotImage
    public Image boxPrefab;                 // Simple UI Image (no sprite) with Outline (green)

    readonly List<Image> pool = new();

    public void Clear()
    {
        foreach (var img in pool) img.gameObject.SetActive(false);
    }

    public void DrawBox(Rect ocrRect, int ocrImgW, int ocrImgH, Color color, float thickness = 2f)
    {
        if (screenshotImage == null || overlayLayer == null || boxPrefab == null) return;
        if (ocrImgW <= 0 || ocrImgH <= 0) return;

        // Map OCR pixel rect → overlay local space
        Rect local = MapOcrRectToOverlay(ocrRect, ocrImgW, ocrImgH);

        var img = GetBox();
        img.color = new Color(color.r, color.g, color.b, 0.18f); // translucent fill
        img.rectTransform.anchoredPosition = local.position;
        img.rectTransform.sizeDelta = local.size;
        img.rectTransform.SetAsLastSibling();

        // Optional: add an Outline for stroke (if attached on prefab) and set thickness via material or a 2nd image
        // If you want a precise frame, consider 4 thin images for edges; this keeps it simple.
    }

    Image GetBox()
    {
        foreach (var i in pool) if (!i.gameObject.activeSelf) { i.gameObject.SetActive(true); return i; }
        var inst = Instantiate(boxPrefab, overlayLayer);
        pool.Add(inst);
        inst.gameObject.SetActive(true);
        return inst;
    }

    Rect MapOcrRectToOverlay(Rect ocrRect, int ocrW, int ocrH)
    {
        // 1) Where is the RawImage content actually drawn (respecting aspect fit)?
        var targetRT = screenshotImage.rectTransform;
        var targetRect = targetRT.rect; // in local space (pivot)
        Vector2 pivot = targetRT.pivot;

        float viewW = targetRect.width;
        float viewH = targetRect.height;
        float viewAspect = viewW / viewH;
        float imgAspect = (float)ocrW / ocrH;

        float drawW, drawH;
        if (imgAspect > viewAspect) { drawW = viewW; drawH = viewW / imgAspect; }
        else { drawH = viewH; drawW = viewH * imgAspect; }

        // 2) Compute letterbox offsets (centered)
        float offX = (viewW - drawW) * 0.5f;
        float offY = (viewH - drawH) * 0.5f;

        // 3) Scale from OCR pixels to drawn pixels
        float sx = drawW / ocrW;
        float sy = drawH / ocrH;

        // OCR origin (x,y) assumed top-left; UI Rect origin (0,0) is at center with pivot.
        // Convert to UI local (centered): first convert to bottom-left origin
        float x = ocrRect.x * sx;
        float yFromTop = ocrRect.y * sy;
        float y = (drawH - yFromTop - ocrRect.height * sy);

        // 4) Position inside the RawImage local rect space
        float px = -targetRect.width * (pivot.x - 0.5f) + offX + x + (ocrRect.width * sx) * 0.5f - targetRect.width * 0.0f;
        float py = -targetRect.height * (pivot.y - 0.5f) + offY + y + (ocrRect.height * sy) * 0.5f - targetRect.height * 0.0f;

        return new Rect(
            new Vector2(px, py),
            new Vector2(ocrRect.width * sx, ocrRect.height * sy)
        );
    }
}
