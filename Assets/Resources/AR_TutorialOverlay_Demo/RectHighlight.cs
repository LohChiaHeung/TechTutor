using UnityEngine;
using TMPro;

public class RectHighlight : MonoBehaviour
{
    public Transform quad;        // assign child Quad
    public LineRenderer outline;  // assign on parent
    public TextMeshPro label;     // optional (child TMP)

    public void SetSize(float widthM, float heightM)
    {
        if (quad) quad.localScale = new Vector3(widthM, 1f, heightM);
        if (outline)
        {
            outline.positionCount = 5;
            float w = widthM * 0.5f, h = heightM * 0.5f;
            Vector3[] pts = {
                new(-w,0,-h), new(w,0,-h), new(w,0,h), new(-w,0,h), new(-w,0,-h)
            };
            outline.SetPositions(pts);
        }
    }

    public void SetLabel(string text) { if (label) label.text = text; }

    void Update()
    {
        if (outline)
        {
            float s = 1f + 0.05f * Mathf.Sin(Time.time * 4f);
            outline.widthMultiplier = 0.002f * s;
        }
    }
}
