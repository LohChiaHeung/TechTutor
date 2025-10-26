using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CalloutItem : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform bubble;       // the rounded panel
    public RectTransform arrow;        // the small pointer triangle/chevron
    public TMP_Text titleText;
    public TMP_Text bodyText;
    public LayoutElement layout;       // optional; controls preferred width/height
    public CanvasGroup cg;             // for fade-in/out (optional)

    [Header("Config")]
    public Vector2 defaultSize = new Vector2(260, 120);
    public Vector2 padding = new Vector2(16, 12); // text insets (if not using a LayoutGroup)
    public float fadeInTime = 0.15f;

    // Expected pivot for the bubble = (0, 0.5) so x is left-edge, y is middle
    // Arrow pivot recommended: (1, 0.5) so its right edge sits at the anchor point

    public void SetText(string title, string body)
    {
        if (titleText) titleText.text = title;
        if (bodyText) bodyText.text = body;
    }

    public void SetSize(Vector2 size)
    {
        if (layout)
        {
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
        }
        else if (bubble)
        {
            bubble.sizeDelta = size;
        }
        else
        {
            var rt = (RectTransform)transform;
            rt.sizeDelta = size;
        }
    }

    /// <summary>Place the whole callout (bubble + arrow) so the bubble’s left edge is at panelPos, vertically centered at panelPos.y.</summary>
    public void Place(Vector2 panelPos)
    {
        var rt = (RectTransform)transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = panelPos;
        rt.SetAsLastSibling();
    }

    /// <summary>Point the arrow toward a target “anchor” (the word center), flipping arrow/bubble if needed.</summary>
    public void PointTo(Vector2 anchorLocal, string side)
    {
        if (!arrow || !bubble) return;

        // Arrow sits at the anchor, aimed toward the bubble
        arrow.anchorMin = Vector2.zero;
        arrow.anchorMax = Vector2.zero;
        arrow.pivot = new Vector2(1f, 0.5f);
        arrow.anchoredPosition = anchorLocal;

        // Flip/position by side (assumes bubble pivot (0,0.5) and arrow pivot (1,0.5))
        // We’ll just rotate/offset arrow; the bubble is already placed by Place()
        float rot = 0f;
        Vector2 off = Vector2.zero;

        switch (side)
        {
            case "left": rot = 180f; off = new Vector2(-8f, 0f); break; // arrow points left
            case "right": rot = 0f; off = new Vector2(8f, 0f); break;
            case "above": rot = -90f; off = new Vector2(0f, 8f); break;
            case "below": rot = 90f; off = new Vector2(0f, -8f); break;
        }
        arrow.localEulerAngles = new Vector3(0, 0, rot);

        // Optional: nudge bubble slightly away from anchor to give breathing room
        var rt = (RectTransform)transform;
        rt.anchoredPosition += off;
    }

    public void Show(float alpha = 1f)
    {
        if (!cg) return;
        cg.alpha = 0f;
        //cg.LeanAlpha(alpha, fadeInTime); // or any tween system you use; replace if not using LeanTween
    }
}
