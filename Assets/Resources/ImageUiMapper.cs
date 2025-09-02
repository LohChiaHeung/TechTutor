//using UnityEngine;

//public static class ImageUiMapper
//{
//    // OCR uses top-left origin, Unity UI uses bottom-left
//    public static Vector2 ImagePxToLocal(Vector2 imgPx, RectTransform rawImageRT, Vector2 imageSizePx)
//    {
//        var rt = rawImageRT.rect.size;
//        float scale = Mathf.Min(rt.x / imageSizePx.x, rt.y / imageSizePx.y);
//        Vector2 fitted = imageSizePx * scale;
//        Vector2 offset = (rt - fitted) * 0.5f;

//        return new Vector2(
//            imgPx.x * scale + offset.x,
//            (imageSizePx.y - imgPx.y) * scale + offset.y
//        );
//    }

//    public static Rect ImageRectPxToLocalRect(Rect imgRectPx, RectTransform rawImageRT, Vector2 imageSizePx)
//    {
//        var blPx = new Vector2(imgRectPx.x, imgRectPx.y + imgRectPx.height);
//        var trPx = new Vector2(imgRectPx.x + imgRectPx.width, imgRectPx.y);
//        var bl = ImagePxToLocal(blPx, rawImageRT, imageSizePx);
//        var tr = ImagePxToLocal(trPx, rawImageRT, imageSizePx);
//        return new Rect(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y);
//    }
//}

using UnityEngine;

public static class ImageUiMapper
{
    /// ocrRectPx: Rect in OCR image pixels (origin top-left, Y down).
    /// rawImageRT: RectTransform of the RawImage that displays the screenshot on your world-space canvas.
    /// imgSizePx:  image (ocr) size in pixels.
    public static Rect ImageRectPxToLocalRect(Rect ocrRectPx, RectTransform rawImageRT, Vector2 imgSizePx)
    {
        // 1) Get RawImage rect in its own local space
        var rawRect = rawImageRT.rect; // local space (0,0) = bottom-left, Y up

        // 2) Compute displayed texture area (letterbox inside rawRect) that preserves aspect
        float targetW = rawRect.width, targetH = rawRect.height;
        float texW = imgSizePx.x, texH = imgSizePx.y;
        float texAR = texW / texH, rectAR = targetW / targetH;

        float dispW, dispH, offX, offY;
        if (rectAR > texAR)
        {
            // pillarboxes left/right
            dispH = targetH;
            dispW = dispH * texAR;
            offX = (targetW - dispW) * 0.5f;
            offY = 0f;
        }
        else
        {
            // letterboxes top/bottom
            dispW = targetW;
            dispH = dispW / texAR;
            offX = 0f;
            offY = (targetH - dispH) * 0.5f;
        }

        // 3) Map OCR pixels (top-left origin) into that displayed sub-rect (bottom-left origin)
        float sx = dispW / texW;
        float sy = dispH / texH;

        // OCR: (0,0) = top-left. UI local: (0,0) = bottom-left → flip Y
        float uiX = offX + ocrRectPx.x * sx;
        float uiY = offY + (texH - (ocrRectPx.y + ocrRectPx.height)) * sy;
        float uiW = ocrRectPx.width * sx;
        float uiH = ocrRectPx.height * sy;

        // 4) Return in rawImageRT local coordinates
        return new Rect(uiX + rawRect.x, uiY + rawRect.y, uiW, uiH);
    }
}

