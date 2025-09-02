//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class CalloutOverlay : MonoBehaviour
//{
//    [SerializeField] RectTransform calloutLayer;   // assign in Inspector
//    [SerializeField] RectTransform calloutPrefab;  // small panel prefab with TMP text (optional)
//    [SerializeField] float defaultWidth = 260f;
//    [SerializeField] float defaultHeight = 120f;

//    public void Clear()
//    {
//        foreach (Transform c in calloutLayer) Destroy(c.gameObject);
//    }

//    // Spawns a panel to the RIGHT of the anchor (x,y) with an arrow
//    public void ShowCallout(string title, string body, Vector2 anchor, float offset = 12f)
//    {
//        var go = calloutPrefab != null ? Instantiate(calloutPrefab, calloutLayer)
//                                       : new GameObject("Callout", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();

//        var rt = go.GetComponent<RectTransform>();
//        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0, 0.5f);
//        rt.anchoredPosition = new Vector2(anchor.x + offset, anchor.y);
//        if (calloutPrefab == null) { rt.sizeDelta = new Vector2(defaultWidth, defaultHeight); go.GetComponent<Image>().color = new Color(1, 1, 1, 0.85f); }

//        // set text if prefab has a TMP_Text
//        var tmp = go.GetComponentInChildren<TMP_Text>();
//        if (tmp != null) tmp.text = $"{title}\n{body}";

//        // tiny arrow at the anchor
//        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
//        arrow.transform.SetParent(calloutLayer, false);
//        arrow.anchorMin = Vector2.zero; arrow.anchorMax = Vector2.zero; arrow.pivot = new Vector2(1, 0.5f);
//        arrow.sizeDelta = new Vector2(16, 8);
//        arrow.anchoredPosition = anchor;
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalloutOverlay : MonoBehaviour
{
    [Header("Targets")]
    [SerializeField] RectTransform calloutLayer;   // assign CalloutLayer RectTransform
    [SerializeField] RectTransform calloutPrefab;  // optional: a prefab panel with TMP_Text inside
    public RectTransform CalloutLayer => calloutLayer;

    [Header("Defaults")]
    [SerializeField] float defaultWidth = 260f;
    [SerializeField] float defaultHeight = 120f;

    public void Clear()
    {
        foreach (Transform c in calloutLayer) Destroy(c.gameObject);
    }

    // ===== Style 1: anchor + offset (Vector2) =====
    public void ShowCallout(string title, string body, Vector2 anchor, float offset = 12f)
    {
        var go = calloutPrefab
            ? Instantiate(calloutPrefab, calloutLayer)
            : new GameObject("Callout", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = new Vector2(0, 0.5f);
        rt.anchoredPosition = new Vector2(anchor.x + offset, anchor.y);

        if (!calloutPrefab)
        {
            rt.sizeDelta = new Vector2(defaultWidth, defaultHeight);
            var img = go.GetComponent<Image>(); if (img) img.color = new Color(1, 1, 1, 0.9f);
        }

        var tmp = go.GetComponentInChildren<TMP_Text>();
        if (tmp) tmp.text = $"{title}\n{body}";

        // small arrow at anchor
        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        arrow.transform.SetParent(calloutLayer, false);
        arrow.anchorMin = Vector2.zero; arrow.anchorMax = Vector2.zero; arrow.pivot = new Vector2(1, 0.5f);
        arrow.sizeDelta = new Vector2(16, 8);
        arrow.anchoredPosition = anchor;
    }

    // ===== Style 2: explicit x,y,w,h (floats) =====
    public void ShowCallout(string title, string body, float x, float y, float w, float h)
    {
        // 1) Create or instantiate the panel under CalloutLayer
        RectTransform rt;
        if (calloutPrefab)
        {
            rt = Instantiate(calloutPrefab, calloutLayer);
        }
        else
        {
            var go = new GameObject("Callout", typeof(RectTransform), typeof(Image));
            rt = go.GetComponent<RectTransform>();
            var img = go.GetComponent<Image>(); if (img) img.color = new Color(1, 1, 1, 0.9f);
        }

        // 2) FORCE anchors/pivot so anchoredPosition is in CalloutLayer local space
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);   // left-center pivot

        // 3) Size + position
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);

        // 4) Ensure text lives INSIDE the panel and gets updated
        var tmp = rt.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (!tmp)
        {
            // create a TMP child if prefab didn't have one
            var textGO = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            var tr = textGO.GetComponent<RectTransform>();
            tr.SetParent(rt, false);
            tr.anchorMin = new Vector2(0, 0);
            tr.anchorMax = new Vector2(1, 1);
            tr.offsetMin = new Vector2(12, 12);
            tr.offsetMax = new Vector2(-12, -12);
            tmp = textGO.GetComponent<TMPro.TextMeshProUGUI>();
            tmp.enableWordWrapping = true;
            tmp.fontSize = 28;
            tmp.color = Color.white;
        }
        tmp.text = $"{title}\n{body}";

        // 5) Optional: small arrow pointing from panel toward the anchor
        var arrow = new GameObject("Arrow", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
        arrow.SetParent(calloutLayer, false);
        arrow.anchorMin = Vector2.zero; arrow.anchorMax = Vector2.zero; arrow.pivot = new Vector2(1, 0.5f);
        arrow.sizeDelta = new Vector2(16, 10);
        arrow.anchoredPosition = new Vector2(x, y); // same anchor as panel start
        arrow.SetAsLastSibling();

        // Bring panel on top of everything in the layer
        rt.SetAsLastSibling();
    }


    //public void ShowCalloutSmart(string title, string body, Vector2 absolutePos, float width = 260f, float height = 120f)
    //{
    //    var go = calloutPrefab
    //        ? Instantiate(calloutPrefab, calloutLayer)
    //        : new GameObject("Callout", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();

    //    var rt = go.GetComponent<RectTransform>();
    //    // Absolute placement in CalloutLayer local space
    //    rt.anchorMin = Vector2.zero;
    //    rt.anchorMax = Vector2.zero;
    //    rt.pivot = new Vector2(0f, 0.5f);            // left-mid pivot (matches your calculations)
    //    rt.anchoredPosition = absolutePos;

    //    if (!calloutPrefab)
    //    {
    //        rt.sizeDelta = new Vector2(width > 0 ? width : defaultWidth, height > 0 ? height : defaultHeight);
    //        var img = go.GetComponent<Image>(); if (img) img.color = new Color(1, 1, 1, 0.9f);
    //    }

    //    var tmp = go.GetComponentInChildren<TMP_Text>();
    //    if (tmp) tmp.text = $"{title}\n{body}";
    //}

    //public RectTransform ShowCalloutSmart(string title, string body, Vector2 panelPos, float width = 220f, float height = 100f)
    //{
    //    var go = calloutPrefab
    //        ? Instantiate(calloutPrefab, calloutLayer)
    //        : new GameObject("Callout", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();

    //    var rt = go.GetComponent<RectTransform>();
    //    rt.anchorMin = Vector2.zero;
    //    rt.anchorMax = Vector2.zero;
    //    rt.pivot = new Vector2(0f, 0.5f);     // x = left edge, y = vertical middle
    //    rt.anchoredPosition = panelPos;

    //    // size (acts as a cap/initial for dynamic prefabs)
    //    if (calloutPrefab)
    //        rt.sizeDelta = new Vector2(width, height); // harmless if a LayoutGroup resizes it
    //    else
    //        rt.sizeDelta = new Vector2(width, height);

    //    // set text if there is a TMP in children
    //    var tmp = go.GetComponentInChildren<TMPro.TMP_Text>();
    //    if (tmp) tmp.text = $"{title}\n{body}";

    //    go.transform.SetAsLastSibling();
    //    return rt;
    //}

    public RectTransform ShowCalloutSmart(string title, string body, Vector2 panelPos, float width = 220f, float height = 100f)
    {
        var go = calloutPrefab
            ? Instantiate(calloutPrefab, calloutLayer)
            : new GameObject("Callout", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = panelPos;

        rt.sizeDelta = new Vector2(width, height);

        var tmp = go.GetComponentInChildren<TMPro.TMP_Text>();
        if (tmp) tmp.text = $"{title}\n{body}";

        go.transform.SetAsLastSibling();
        Debug.Log($"[Callout] Spawned panel size=({rt.rect.width:F0}x{rt.rect.height:F0}) at {rt.anchoredPosition}");

        return rt;
    }


}
