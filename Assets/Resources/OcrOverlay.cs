using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OcrOverlay : MonoBehaviour
{
    [SerializeField] RectTransform rawImageRT;  // assign ScreenshotImage RectTransform
    [SerializeField] RectTransform overlayRoot; // assign Overlay RectTransform
    [SerializeField] Color boxColor = new Color(1, 0, 0, 0.25f);

    [Header("Debug")]
    public bool allowDebugAll = false;

    readonly List<GameObject> pool = new();

    public void Clear()
    {
        foreach (var go in pool) Destroy(go);
        pool.Clear();
    }

    public void RenderSingleBox(RectTransform rawImageRT, Vector2 imgSize, Rect imgRectPx, Color color)
    {
        var uiRect = ImageUiMapper.ImageRectPxToLocalRect(imgRectPx, rawImageRT, imgSize);

        var go = new GameObject("OCR_Highlight_Green", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(overlayRoot, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
        rt.anchoredPosition = new Vector2(uiRect.x, uiRect.y);
        rt.sizeDelta = new Vector2(uiRect.width, uiRect.height);

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;              // avoid blocking clicks
        img.color = color;                      // <-- use the passed-in color
        go.transform.SetAsLastSibling();        // <-- ensure on top of red boxes

        pool.Add(go);
    }


    public void Render(OcrResponse resp)
    {
        if (!allowDebugAll) return; //add this guard
        if (resp == null || resp.words == null) return;
        Clear();

        var imgSize = new Vector2(resp.width, resp.height);

        foreach (var w in resp.words)
        {
            var uiRect = ImageUiMapper.ImageRectPxToLocalRect(
                new Rect(w.x, w.y, w.w, w.h),
                rawImageRT, imgSize
            );

            var go = new GameObject("OCRBox", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(overlayRoot, false);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
            rt.anchoredPosition = new Vector2(uiRect.x, uiRect.y);
            rt.sizeDelta = new Vector2(uiRect.width, uiRect.height);

            var img = go.GetComponent<Image>();
            img.color = boxColor;

            pool.Add(go);
        }
    }
}
