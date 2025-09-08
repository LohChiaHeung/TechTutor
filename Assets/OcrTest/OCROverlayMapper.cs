using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OCROverlayMapper : MonoBehaviour
{
    public RawImage screenshotImage;
    public RectTransform overlayParent;
    public GameObject boxPrefab;

    [System.Serializable]
    public struct OcrBox
    {
        public float x, y, w, h;
        public string text;
    }

    public void RenderBoxes(List<OcrBox> boxes)
    {
        if (!screenshotImage || !overlayParent || screenshotImage.texture == null)
            return;

        foreach (Transform child in overlayParent)
            Destroy(child.gameObject);

        Rect imageRect = GetDisplayedTextureRect();
        float imgW = screenshotImage.texture.width;
        float imgH = screenshotImage.texture.height;
        float scale = imageRect.width / imgW;

        foreach (var b in boxes)
        {
            float uiX = imageRect.x + b.x * scale;
            float uiY = imageRect.y + (imgH - b.y - b.h) * scale;
            float uiW = b.w * scale;
            float uiH = b.h * scale;

            GameObject box = Instantiate(boxPrefab, overlayParent);
            RectTransform rt = box.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(uiX, uiY);
            rt.sizeDelta = new Vector2(uiW, uiH);
        }
    }

    private Rect GetDisplayedTextureRect()
    {
        Vector3[] corners = new Vector3[4];
        screenshotImage.rectTransform.GetWorldCorners(corners);
        Vector2 bl = WorldToLocal(overlayParent, corners[0]);
        Vector2 tr = WorldToLocal(overlayParent, corners[2]);
        Rect rawRect = new Rect(bl, tr - bl);

        float texW = screenshotImage.texture.width;
        float texH = screenshotImage.texture.height;
        float rectW = rawRect.width;
        float rectH = rawRect.height;

        float texAspect = texW / texH;
        float rectAspect = rectW / rectH;

        float dispW, dispH, offX, offY;
        if (rectAspect > texAspect)
        {
            dispH = rectH;
            dispW = dispH * texAspect;
            offX = (rectW - dispW) * 0.5f;
            offY = 0f;
        }
        else
        {
            dispW = rectW;
            dispH = dispW / texAspect;
            offX = 0f;
            offY = (rectH - dispH) * 0.5f;
        }

        return new Rect(rawRect.x + offX, rawRect.y + offY, dispW, dispH);
    }

    private Vector2 WorldToLocal(RectTransform parent, Vector3 world)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent, RectTransformUtility.WorldToScreenPoint(null, world), null, out Vector2 local);
        return local + parent.rect.size * 0.5f;
    }
}
