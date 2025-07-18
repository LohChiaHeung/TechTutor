using UnityEngine;
using System.Collections.Generic;

public class QuadYoloOverlay : MonoBehaviour
{
    private List<YoloBox> boxes;
    private float imgW, imgH;

    public void Init(List<YoloBox> detections, float width, float height)
    {
        boxes = detections;
        imgW = width; imgH = height;
        DrawOnQuad();
    }

    void DrawOnQuad()
    {
        float half = transform.localScale.x * 0.5f;
        foreach (var b in boxes)
        {
            float u = b.x / imgW;
            float v = 1 - (b.y / imgH);
            Vector3 pos = new Vector3((u - 0.5f) * transform.localScale.x,
                                      (v - 0.5f) * transform.localScale.y,
                                      0.01f);

            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;

            var text = go.AddComponent<TextMesh>();
            text.text = $"{b.label} ({b.confidence:F2})";
            text.characterSize = 0.02f;
        }
    }
}