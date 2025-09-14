//using UnityEngine;
//using UnityEngine.UI;
//using System.Collections.Generic;

//public class OcrRedboxOverlayController : MonoBehaviour
//{
//    [Header("Assign in Inspector")]
//    public RawImage displayImage;      // Your RawImage (Preserve Aspect = OFF)
//    public RectTransform overlayRoot;  // Empty RectTransform over RawImage
//    public Image boxTemplate;          // Disabled UI Image with red Outline

//    readonly List<GameObject> pool = new();


//    //public void SetTexture(Texture2D tex)
//    //{
//    //    displayImage.texture = tex;

//    //    var imgRT = displayImage.rectTransform;
//    //    overlayRoot.anchorMin = imgRT.anchorMin;
//    //    overlayRoot.anchorMax = imgRT.anchorMax;
//    //    overlayRoot.pivot = imgRT.pivot;
//    //    overlayRoot.anchoredPosition = imgRT.anchoredPosition;
//    //    overlayRoot.sizeDelta = imgRT.sizeDelta;

//    //    Debug.Log($"[Redbox] UI {imgRT.rect.size} | Tex {tex.width}x{tex.height}");
//    //}
//    //public void SetTexture(Texture2D tex)
//    //{
//    //    displayImage.texture = tex;

//    //    var imgRT = displayImage.rectTransform;

//    //    // Parent and STRETCH to fill the RawImage
//    //    overlayRoot.SetParent(imgRT, false);
//    //    overlayRoot.anchorMin = new Vector2(0, 0);
//    //    overlayRoot.anchorMax = new Vector2(1, 1);
//    //    overlayRoot.pivot = new Vector2(0, 1);   // keep top-left pivot for our boxes
//    //    overlayRoot.offsetMin = Vector2.zero;        // left/bottom inset = 0
//    //    overlayRoot.offsetMax = Vector2.zero;        // right/top inset = 0

//    //    // No need to copy sizeDelta anymore; stretch will follow RawImage size
//    //}
//    void Awake()
//    {
//        if (boxTemplate) boxTemplate.gameObject.SetActive(false);
//    }

//    public void SetTexture(Texture2D tex)
//    {
//        if (tex == null) return;

//        // 1) assign to RawImage
//        displayImage.texture = tex;

//        // 2) make it crisp on world-space canvas
//        tex.filterMode = FilterMode.Point;
//        tex.anisoLevel = 0;

//        // 3) show at native pixel size (pixel-perfect)
//        displayImage.SetNativeSize();

//        // 4) ensure overlay is a child of the RawImage and STRETCHES with it
//        overlayRoot.SetParent(displayImage.rectTransform, false);   // <-- add this line
//        overlayRoot.anchorMin = Vector2.zero;
//        overlayRoot.anchorMax = Vector2.one;
//        overlayRoot.offsetMin = Vector2.zero;
//        overlayRoot.offsetMax = Vector2.zero;
//    }

//    //public void SetTexture(Texture2D tex)
//    //{
//    //    if (tex == null) return;

//    //    displayImage.texture = tex;

//    //    // make it crisp (esp. world-space canvas)
//    //    tex.filterMode = FilterMode.Point;
//    //    tex.anisoLevel = 0;

//    //    // show at native pixels (no preserve aspect)
//    //    displayImage.SetNativeSize();

//    //    // overlay follows the RawImage exactly
//    //    overlayRoot.SetParent(displayImage.rectTransform, false);
//    //    overlayRoot.anchorMin = Vector2.zero;
//    //    overlayRoot.anchorMax = Vector2.one;
//    //    overlayRoot.offsetMin = Vector2.zero;
//    //    overlayRoot.offsetMax = Vector2.zero;
//    //    overlayRoot.localScale = Vector3.one;

//    //    // force layout so overlayRoot.rect is valid THIS frame
//    //    Canvas.ForceUpdateCanvases();
//    //    LayoutRebuilder.ForceRebuildLayoutImmediate(displayImage.rectTransform);

//    //    Debug.Log($"[Redbox/UI] RawImage={displayImage.rectTransform.rect.size} overlay={overlayRoot.rect.size} tex={tex.width}x{tex.height}");
//    //}




//    //public void ClearBoxes()
//    //{
//    //    foreach (var go in pool) Destroy(go);
//    //    pool.Clear();
//    //}

//    public void ClearBoxes()
//    {
//        // destroy every child except the hidden template
//        for (int i = overlayRoot.childCount - 1; i >= 0; i--)
//        {
//            var child = overlayRoot.GetChild(i).gameObject;
//            if (boxTemplate != null && child == boxTemplate.gameObject) continue;
//            Destroy(child);
//        }
//        pool.Clear();
//        Debug.Log($"[Redbox/UI] Cleared overlay. Children now = {overlayRoot.childCount}");
//    }


//    /// Draw exactly one word box in the given color (e.g., green)
//    public void DrawOneWord(OcrRedboxWord b, int imgW, int imgH, Color borderColor)
//    {
//        ClearBoxes();

//        var rect = overlayRoot.rect;
//        float rw = rect.width, rh = rect.height;

//        // pixels -> normalized
//        float nx = b.x / imgW, ny = b.y / imgH;
//        float nw = b.w / imgW, nh = b.h / imgH;

//        // top-left anchors/pivot: X right +, Y down NEGATIVE
//        float x = nx * rw;
//        float y = -ny * rh;
//        float w = Mathf.Max(1f, nw * rw);
//        float h = Mathf.Max(1f, nh * rh);

//        var go = Instantiate(boxTemplate.gameObject, overlayRoot);
//        go.SetActive(true);
//        pool.Add(go);

//        var r = go.GetComponent<RectTransform>();
//        r.pivot = new Vector2(0, 1);
//        r.anchorMin = new Vector2(0, 1);
//        r.anchorMax = new Vector2(0, 1);
//        r.anchoredPosition = new Vector2(x, y);
//        r.sizeDelta = new Vector2(w, h);

//        // Make it non-blocking and GREEN outline
//        var img = go.GetComponent<Image>();
//        if (img != null) img.raycastTarget = false;
//        var outline = go.GetComponent<Outline>();
//        if (outline != null) outline.effectColor = borderColor; // set to green
//    }
//    public void DrawWords(List<OcrRedboxWord> words, int imgW, int imgH, float confMin = 0f)
//    {
//        // Clear previous
//        foreach (var go in pool) Destroy(go);
//        pool.Clear();

//        var rect = overlayRoot.rect;
//        float rw = rect.width, rh = rect.height;

//        foreach (var b in words)
//        {
//            if (b.conf < confMin) continue;

//            // Normalize from pixel -> [0..1]
//            float nx = b.x / imgW, ny = b.y / imgH;
//            float nw = b.w / imgW, nh = b.h / imgH;

//            // Map to UI (top-left pivot)
//            float x = nx * rw;
//            float y = -ny * rh;         // flip Y for top-left UI
//            float w = Mathf.Max(1f, nw * rw);
//            float h = Mathf.Max(1f, nh * rh);

//            var go = Instantiate(boxTemplate.gameObject, overlayRoot);
//            go.SetActive(true);
//            pool.Add(go);

//            var r = go.GetComponent<RectTransform>();
//            r.pivot = new Vector2(0, 1);
//            r.anchorMin = new Vector2(0, 1);
//            r.anchorMax = new Vector2(0, 1);
//            r.anchoredPosition = new Vector2(x, y);
//            r.sizeDelta = new Vector2(w, h);
//        }

//        Debug.Log($"[Redbox] Drew {pool.Count} boxes (img {imgW}x{imgH}).");
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OcrRedboxOverlayController : MonoBehaviour
{
    [Header("Templates")]
    public Image boxTemplate;    // red outline
    public RawImage arrowTemplate; // assign an arrow RawImage (disabled by default)

    [Header("Arrow Settings")]
    public float arrowAbovePx = 12f;          // how high above the box
    public Vector2 arrowSize = new Vector2(24, 24);

    [Header("Assign in Inspector")]
    public RawImage displayImage;      // Your RawImage (Preserve Aspect = OFF)
    public RectTransform overlayRoot;  // Empty RectTransform over RawImage
    //public Image boxTemplate;          // Disabled UI Image with red Outline

    readonly List<GameObject> pool = new();


    //public void SetTexture(Texture2D tex)
    //{
    //    displayImage.texture = tex;

    //    var imgRT = displayImage.rectTransform;
    //    overlayRoot.anchorMin = imgRT.anchorMin;
    //    overlayRoot.anchorMax = imgRT.anchorMax;
    //    overlayRoot.pivot = imgRT.pivot;
    //    overlayRoot.anchoredPosition = imgRT.anchoredPosition;
    //    overlayRoot.sizeDelta = imgRT.sizeDelta;

    //    Debug.Log($"[Redbox] UI {imgRT.rect.size} | Tex {tex.width}x{tex.height}");
    //}
    //public void SetTexture(Texture2D tex)
    //{
    //    displayImage.texture = tex;

    //    var imgRT = displayImage.rectTransform;

    //    // Parent and STRETCH to fill the RawImage
    //    overlayRoot.SetParent(imgRT, false);
    //    overlayRoot.anchorMin = new Vector2(0, 0);
    //    overlayRoot.anchorMax = new Vector2(1, 1);
    //    overlayRoot.pivot = new Vector2(0, 1);   // keep top-left pivot for our boxes
    //    overlayRoot.offsetMin = Vector2.zero;        // left/bottom inset = 0
    //    overlayRoot.offsetMax = Vector2.zero;        // right/top inset = 0

    //    // No need to copy sizeDelta anymore; stretch will follow RawImage size
    //}
    void Awake()
    {
        if (boxTemplate) boxTemplate.gameObject.SetActive(false);
        if (arrowTemplate) arrowTemplate.gameObject.SetActive(false);
    }

    public void SetTexture(Texture2D tex)
    {
        if (tex == null) return;

        // 1) assign to RawImage
        displayImage.texture = tex;

        // 2) make it crisp on world-space canvas
        tex.filterMode = FilterMode.Point;
        tex.anisoLevel = 0;

        // 3) show at native pixel size (pixel-perfect)
        //displayImage.SetNativeSize();

        // 4) ensure overlay is a child of the RawImage and STRETCHES with it
        overlayRoot.SetParent(displayImage.rectTransform, false);   // <-- add this line
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        //Canvas.ForceUpdateCanvases();
        //LayoutRebuilder.ForceRebuildLayoutImmediate(displayImage.rectTransform);
    }



    public void ClearBoxes()
    {
        for (int i = overlayRoot.childCount - 1; i >= 0; i--)
        {
            var child = overlayRoot.GetChild(i).gameObject;
            if (boxTemplate != null && child == boxTemplate.gameObject) continue;
            Destroy(child);
        }
        pool.Clear();
        Debug.Log($"[Redbox/UI] Cleared overlay. Children now = {overlayRoot.childCount}");
    }


    /// Draw exactly one word box in the given color (e.g., green)
    public void DrawOneWord(OcrRedboxWord b, int imgW, int imgH, Color borderColor)
    {
        ClearBoxes();

        var rect = overlayRoot.rect;
        float rw = rect.width, rh = rect.height;

        // pixels -> normalized
        float nx = b.x / imgW, ny = b.y / imgH;
        float nw = b.w / imgW, nh = b.h / imgH;

        // top-left anchors/pivot: X right +, Y down NEGATIVE
        float x = nx * rw;
        float y = -ny * rh;
        float w = Mathf.Max(1f, nw * rw);
        float h = Mathf.Max(1f, nh * rh);

        var go = Instantiate(boxTemplate.gameObject, overlayRoot);
        go.SetActive(true);
        pool.Add(go);

        var r = go.GetComponent<RectTransform>();
        r.pivot = new Vector2(0, 1);
        r.anchorMin = new Vector2(0, 1);
        r.anchorMax = new Vector2(0, 1);
        r.anchoredPosition = new Vector2(x, y);
        r.sizeDelta = new Vector2(w, h);

        if (arrowTemplate != null)
        {
            var arrow = Instantiate(arrowTemplate.gameObject, overlayRoot);
            arrow.SetActive(true);

            var arrowRT = arrow.GetComponent<RectTransform>();
            arrowRT.pivot = new Vector2(0.5f, 0);        // bottom-center pivot
            arrowRT.anchorMin = new Vector2(0, 1);
            arrowRT.anchorMax = new Vector2(0, 1);

            // place it slightly above the top edge of the box
            float centerX = x + w * 0.5f;
            float arrowY = y + arrowAbovePx; // go UP = less negative
            arrowRT.anchoredPosition = new Vector2(centerX, arrowY);
            arrowRT.sizeDelta = arrowSize;
        }

        // Make it non-blocking and GREEN outline
        var img = go.GetComponent<Image>();
        if (img != null) img.raycastTarget = false;
        //var outline = go.GetComponent<Outline>();
        //if (outline != null) outline.effectColor = borderColor; // set to green
    }
    public void DrawWords(List<OcrRedboxWord> words, int imgW, int imgH, float confMin = 0f)
    {
        // Clear previous
        foreach (var go in pool) Destroy(go);
        pool.Clear();

        var rect = overlayRoot.rect;
        float rw = rect.width, rh = rect.height;

        foreach (var b in words)
        {
            if (b.conf < confMin) continue;

            // Normalize from pixel -> [0..1]
            float nx = b.x / imgW, ny = b.y / imgH;
            float nw = b.w / imgW, nh = b.h / imgH;

            // Map to UI (top-left pivot)
            float x = nx * rw;
            float y = -ny * rh;         // flip Y for top-left UI
            float w = Mathf.Max(1f, nw * rw);
            float h = Mathf.Max(1f, nh * rh);

            var go = Instantiate(boxTemplate.gameObject, overlayRoot);
            go.SetActive(true);
            pool.Add(go);

            var r = go.GetComponent<RectTransform>();
            r.pivot = new Vector2(0, 1);
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, y);
            r.sizeDelta = new Vector2(w, h);
        }

        Debug.Log($"[Redbox] Drew {pool.Count} boxes (img {imgW}x{imgH}).");
    }
}
