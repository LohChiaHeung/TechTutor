//---- TEST ----
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class calloutOverlay : MonoBehaviour
//{
//    [Header("Refs")]
//    public RawImage photo;                 // the image
//    public RectTransform calloutRoot;      // under Photo
//    public RectTransform calloutPrefab;    // prefab with Arrow + Bubble
//                                           // Add this inside the class
//    public RectTransform calloutLayer;

//    [Header("Layout")]
//    public Vector2 bubbleOffset = new Vector2(120, 60); // px offset from target point
//    public float arrowThickness = 6f;                   // px

//    // region: normalized [0..1], TOP-LEFT origin; if it's a box, we'll use its center
//    public void ShowCallout(string title, string body, float x, float y, float w = 0f, float h = 0f)
//    {
//        // Make sure Photo has laid out for this frame
//        LayoutRebuilder.ForceRebuildLayoutImmediate(photo.rectTransform);

//        // compute drawn rect of the texture inside RawImage
//        Rect R = GetDisplayedImageRect(photo);

//        // convert TOP-LEFT normalized to Photo local (bottom-left)
//        float px = R.xMin + (x + w * 0.5f) * R.width;
//        float py = R.yMin + (1f - y - h * 0.5f) * R.height;

//        // spawn callout
//        foreach (Transform c in calloutRoot) Destroy(c.gameObject); // one at a time
//        RectTransform co = Instantiate(calloutPrefab, calloutRoot);

//        // find parts
//        var arrow = co.Find("Arrow") as RectTransform;
//        var bubble = co.Find("Bubble") as RectTransform;
//        var titleText = bubble.GetComponentInChildren<TMP_Text>(); // top text
//        if (titleText) titleText.text = $"{title}\n{body}";

//        // place bubble with an offset (you can change to left/right based on space)
//        Vector2 target = new Vector2(px, py);
//        Vector2 bubblePos = target + bubbleOffset;

//        // keep bubble inside the photo rect (simple clamp)
//        Vector2 min = new Vector2(R.xMin + 8, R.yMin + 8);
//        Vector2 max = new Vector2(R.xMax - bubble.rect.width - 8, R.yMax - bubble.rect.height - 8);
//        bubblePos.x = Mathf.Clamp(bubblePos.x, min.x, max.x);
//        bubblePos.y = Mathf.Clamp(bubblePos.y, min.y, max.y);

//        // set positions (all in Photo local space because calloutRoot is under Photo)
//        bubble.anchorMin = bubble.anchorMax = new Vector2(0, 0);
//        bubble.pivot = new Vector2(0, 0);
//        bubble.anchoredPosition = bubblePos;

//        // draw arrow from target → bubble edge
//        Vector2 bubbleCenter = bubblePos + bubble.rect.size * 0.5f;
//        Vector2 dir = (bubbleCenter - target);
//        float len = dir.magnitude;
//        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

//        arrow.anchorMin = arrow.anchorMax = new Vector2(0, 0);
//        arrow.pivot = new Vector2(0, 0.5f);
//        arrow.sizeDelta = new Vector2(len, arrowThickness);
//        arrow.anchoredPosition = target;
//        arrow.localRotation = Quaternion.Euler(0, 0, angle);
//    }

//    Rect GetDisplayedImageRect(RawImage raw)
//    {
//        Rect r = raw.rectTransform.rect;              // Photo local rect
//        var tex = raw.texture;
//        if (!tex) return r;

//        float imgAspect = (float)tex.width / tex.height;
//        float rectAspect = r.width / r.height;

//        float drawW, drawH, offX = 0, offY = 0;
//        if (imgAspect >= rectAspect) { drawW = r.width; drawH = r.width / imgAspect; offY = (r.height - drawH) * 0.5f; }
//        else { drawH = r.height; drawW = r.height * imgAspect; offX = (r.width - drawW) * 0.5f; }

//        return new Rect(r.xMin + offX, r.yMin + offY, drawW, drawH);
//    }


//}
