using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class YoloOverlay : MonoBehaviour
{
    public RawImage imageDisplay;
    public RectTransform boxContainer;
    public GameObject boxPrefab;
    public GameObject labelPrefab;

    private List<GameObject> overlays = new List<GameObject>();

    //    public void DrawDetections(List<YoloBox> detections, float imageWidth, float imageHeight)
    //    {
    //        // Clear previous overlays
    //        foreach (var obj in overlays)
    //            Destroy(obj);
    //        overlays.Clear();

    //        // Scale factors
    //        float scaleX = imageDisplay.rectTransform.rect.width / imageWidth;
    //        float scaleY = imageDisplay.rectTransform.rect.height / imageHeight;

    //        foreach (var det in detections)
    //        {
    //            // Create Box
    //            var boxObj = Instantiate(boxPrefab, boxContainer);
    //            var rt = boxObj.GetComponent<RectTransform>();

    //            // Convert center coordinates to top-left
    //            float left = det.x - det.w / 2f;
    //            float top = det.y - det.h / 2f;


    //            float xPos = left * scaleX;
    //            float yPos = (imageHeight - top - det.h) * scaleY;

    //            float flippedY = yPos;
    //            float anchorY = boxContainer.rect.height - flippedY;

    //            // Place box
    //            rt.anchoredPosition = new Vector2(xPos, boxContainer.rect.height - yPos);
    //            rt.sizeDelta = new Vector2(det.w * scaleX, det.h * scaleY);

    //            // Calculate center of box for label
    //            float centerX = xPos + (det.w * scaleX) / 2f;
    //            float centerY = yPos - (det.h * scaleY) / 2f;

    //            // Create Label
    //            var labelObj = Instantiate(labelPrefab, boxContainer);
    //            var labelRT = labelObj.GetComponent<RectTransform>();

    //            // Set pivot to center so text centers nicely
    //            labelRT.pivot = new Vector2(0.5f, 0.5f);

    //            // Place label at center of box
    //            labelRT.anchoredPosition = new Vector2(centerX, centerY);


    //            labelObj.GetComponent<TMPro.TextMeshProUGUI>().text =
    //                $"{det.label} ({det.confidence:F2})";

    //            overlays.Add(boxObj);
    //            overlays.Add(labelObj);
    //        }
    //    }

    //}

    //public void DrawDetections(List<YoloBox> detections, float imageWidth, float imageHeight)
    //{
    //    Debug.Log(">>> Drawing detections <<<");

    //    foreach (var obj in overlays)
    //        Destroy(obj);
    //    overlays.Clear();

    //    var filteredDetections = ApplyNMS(detections, 0.5f);

    //    var rawRect = imageDisplay.rectTransform.rect;
    //    float rawWidth = rawRect.width;
    //    float rawHeight = rawRect.height;

    //    float scaleX = rawWidth / imageWidth;
    //    float scaleY = rawHeight / imageHeight;

    //    Debug.Log($"✅ rawWidth={rawWidth}, rawHeight={rawHeight}, scaleX={scaleX}, scaleY={scaleY}");

    //    foreach (var det in filteredDetections)
    //    {
    //        float centerX = det.x;
    //        float centerY = det.y;

    //        float boxWidth = det.w * scaleX;
    //        float boxHeight = det.h * scaleY;

    //        float xUI = (centerX - imageWidth / 2f) * scaleX;
    //        float yUI = (imageHeight / 2f - centerY) * scaleY;

    //        var boxObj = Instantiate(boxPrefab, boxContainer);
    //        var rt = boxObj.GetComponent<RectTransform>();
    //        rt.pivot = new Vector2(0.5f, 0.5f);
    //        rt.anchoredPosition = new Vector2(xUI, yUI);
    //        rt.sizeDelta = new Vector2(boxWidth, boxHeight);

    //        var labelObj = Instantiate(labelPrefab, boxContainer);
    //        var labelRT = labelObj.GetComponent<RectTransform>();
    //        labelRT.pivot = new Vector2(0.5f, 0.5f);
    //        labelRT.anchoredPosition = new Vector2(xUI, yUI);
    //        labelObj.GetComponent<TMPro.TextMeshProUGUI>().text =
    //            $"{det.label} ({det.confidence:F2})";

    //        overlays.Add(boxObj);
    //        overlays.Add(labelObj);
    //    }
    //}
    public void DrawDetections(List<YoloBox> detections, float imageWidth, float imageHeight)
    {
        Debug.Log(">>> Drawing detections <<<");

        foreach (var obj in overlays)
            Destroy(obj);
        overlays.Clear();

        var filteredDetections = ApplyNMS(detections, 0.5f);

        var rawRect = imageDisplay.rectTransform.rect;
        float rawWidth = rawRect.width;
        float rawHeight = rawRect.height;

        float scaleX = rawWidth / imageWidth;
        float scaleY = rawHeight / imageHeight;

        Debug.Log($"✅ rawWidth={rawWidth}, rawHeight={rawHeight}, scaleX={scaleX}, scaleY={scaleY}");

        foreach (var det in filteredDetections)
        {
            // draw using the updated det.x and det.y
            float scaledCenterX = det.x * scaleX;
            float scaledCenterY = det.y * scaleY;

            float xUI = scaledCenterX - rawWidth / 2f;
            float yUI = (rawHeight / 2f) - scaledCenterY;

            float boxWidth = det.w * scaleX;
            float boxHeight = det.h * scaleY;

            var boxObj = Instantiate(boxPrefab, boxContainer);
            var rt = boxObj.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(xUI, yUI);
            rt.sizeDelta = new Vector2(boxWidth, boxHeight);

            var labelObj = Instantiate(labelPrefab, boxContainer);
            var labelRT = labelObj.GetComponent<RectTransform>();
            labelRT.pivot = new Vector2(0.5f, 0.5f);
            labelRT.anchoredPosition = new Vector2(xUI, yUI);
            labelObj.GetComponent<TMPro.TextMeshProUGUI>().text =
                $"{det.label} ({det.confidence:F2})";

            overlays.Add(boxObj);
            overlays.Add(labelObj);
        }
    }
    private List<YoloBox> ApplyNMS(List<YoloBox> detections, float iouThreshold = 0.5f)
    {
        var result = new List<YoloBox>();

        var groups = new Dictionary<string, List<YoloBox>>();

        // Group by label
        foreach (var det in detections)
        {
            if (!groups.ContainsKey(det.label))
                groups[det.label] = new List<YoloBox>();

            groups[det.label].Add(det);
        }

        foreach (var group in groups)
        {
            var boxes = group.Value;
            boxes.Sort((a, b) => b.confidence.CompareTo(a.confidence));

            while (boxes.Count > 0)
            {
                var best = boxes[0];
                result.Add(best);
                boxes.RemoveAt(0);

                boxes.RemoveAll(b => IoU(best, b) > iouThreshold);
            }
        }

        return result;
    }


    private float IoU(YoloBox a, YoloBox b)
    {
        float x1 = Mathf.Max(a.x - a.w / 2f, b.x - b.w / 2f);
        float y1 = Mathf.Max(a.y - a.h / 2f, b.y - b.h / 2f);
        float x2 = Mathf.Min(a.x + a.w / 2f, b.x + b.w / 2f);
        float y2 = Mathf.Min(a.y + a.h / 2f, b.y + b.h / 2f);

        float interWidth = x2 - x1;
        float interHeight = y2 - y1;

        if (interWidth <= 0 || interHeight <= 0)
            return 0;

        float intersection = interWidth * interHeight;
        float areaA = a.w * a.h;
        float areaB = b.w * b.h;
        float union = areaA + areaB - intersection;

        return intersection / union;
    }


}
public class YoloBox
{
    public float x;
    public float y;
    public float w;
    public float h;
    public string label;
    public float confidence;
}


