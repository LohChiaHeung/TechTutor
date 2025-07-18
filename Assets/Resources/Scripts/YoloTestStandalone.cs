using UnityEngine;
using System.Collections.Generic;

public class YoloTestStandalone : MonoBehaviour
{
    public YoloDetector yoloDetector;
    public Texture2D testImage;

    void Start()
    {
        yoloDetector.Init();

        Debug.Log("➡️ Starting YOLO test...");

        Texture2D resized = ResizeTexture(testImage, 640, 640);

        List<YoloBox> boxes = yoloDetector.Detect(resized);
        Debug.Log($"✅ YOLO detected {boxes.Count} boxes.");

        List<YoloBox> filtered = yoloDetector.ApplyNMS(boxes, 0.5f);
        Debug.Log($"✅ After NMS: {filtered.Count} boxes.");

        foreach (var box in filtered)
        {
            Debug.Log($"📦 Label: {box.label} | Confidence: {box.confidence:F2} | (x={box.x}, y={box.y}, w={box.w}, h={box.h})");
        }

    }


    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
}
