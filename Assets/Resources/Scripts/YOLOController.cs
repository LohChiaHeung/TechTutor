using UnityEngine;
using Unity.Barracuda;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class YOLOController : MonoBehaviour
{
    public NNModel modelAsset;
    public TextMeshProUGUI debugText;
    public RectTransform canvasRect;
    public GameObject labelPrefab;
    public RawImage cameraView;

    private Model runtimeModel;
    private IWorker worker;

    private WebCamTexture webcam;
    private Texture2D resizedTexture;
    private List<GameObject> currentLabels = new List<GameObject>();

    private Dictionary<int, string> classLabels = new Dictionary<int, string>()
    {
        { 0, "A" }, { 1, "B" }, { 2, "Shift" }, { 3, "Ctrl" }, { 4, "Enter" }
    };

    int frameSkip = 5;
    int frameCount = 0;


    void Start()
    {
        runtimeModel = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        webcam = new WebCamTexture(1280, 720);
        cameraView.texture = webcam;
        cameraView.rectTransform.sizeDelta = canvasRect.sizeDelta;
        webcam.Play();

        resizedTexture = new Texture2D(640, 640);
    }

    void Update()
    {
        frameCount++;

        if (webcam.didUpdateThisFrame && frameCount % frameSkip == 0)
        {
            // Reuse resizedTexture instead of creating new
            resizedTexture.SetPixels32(webcam.GetPixels32());
            resizedTexture.Apply();

            RunYOLO(resizedTexture);
        }
    }


    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.ReleaseTemporary(rt);
        RenderTexture.active = null;

        return result;
    }

    void RunYOLO(Texture2D image)
    {
        Tensor input = Preprocess(image);
        worker.Execute(input);
        Tensor output = worker.PeekOutput();
        Postprocess(output);
        input.Dispose();
        output.Dispose();
    }

    Tensor Preprocess(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        float[] floatValues = new float[pixels.Length * 3];

        for (int i = 0; i < pixels.Length; ++i)
        {
            Color32 pixel = pixels[i];
            floatValues[i * 3 + 0] = pixel.r / 255f;
            floatValues[i * 3 + 1] = pixel.g / 255f;
            floatValues[i * 3 + 2] = pixel.b / 255f;
        }

        return new Tensor(1, texture.height, texture.width, 3, floatValues);
    }

    void Postprocess(Tensor output)
    {
        ClearLabels();

        int numBoxes = output.shape.width;  // 8400
        int features = output.shape.channels; // 64
        float confThreshold = 0.5f;

        int detectedCount = 0;
        string debugInfo = $"Output shape: {output.shape}\n";

        for (int i = 0; i < numBoxes; i++)
        {
            float objConfidence = output[0, 0, i, 4];
            if (objConfidence < confThreshold) continue;

            float cx = output[0, 0, i, 0];
            float cy = output[0, 0, i, 1];
            float w = output[0, 0, i, 2];
            float h = output[0, 0, i, 3];

            int classId = 0;
            float maxClassScore = 0f;
            for (int j = 5; j < features; j++)
            {
                float score = output[0, 0, i, j];
                if (score > maxClassScore)
                {
                    maxClassScore = score;
                    classId = j - 5;
                }
            }

            float finalConfidence = objConfidence * maxClassScore;
            if (finalConfidence < confThreshold) continue;

            detectedCount++;

            string label = classLabels.ContainsKey(classId) ? classLabels[classId] : $"ID:{classId}";

            debugInfo += $"[{detectedCount}] {label} - Conf: {finalConfidence:F2}\n";

            // Optional: draw labels if detection found
            float x = cx - w / 2;
            float y = cy - h / 2;

            Vector2 position = new Vector2(
                (x / 640f) * canvasRect.rect.width,
                (1f - y / 640f) * canvasRect.rect.height
            );

            Vector2 size = new Vector2(
                (w / 640f) * canvasRect.rect.width,
                (h / 640f) * canvasRect.rect.height
            );

            CreateLabel(label, position, size);
        }

        debugText.text = debugInfo;

        if (detectedCount == 0)
            Debug.Log("✅ YOLO ran, but no objects detected.");
        else
            Debug.Log($"✅ Detected {detectedCount} object(s).");
    }


    void CreateLabel(string labelText, Vector2 position, Vector2 size)
    {
        GameObject label = Instantiate(labelPrefab, canvasRect);
        label.GetComponentInChildren<TextMeshProUGUI>().text = labelText;

        RectTransform rt = label.GetComponent<RectTransform>();
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        currentLabels.Add(label);
    }

    void ClearLabels()
    {
        foreach (var obj in currentLabels)
            Destroy(obj);
        currentLabels.Clear();
    }

    void OnDestroy()
    {
        if (webcam != null && webcam.isPlaying)
            webcam.Stop();

        worker.Dispose();
    }
}



// Optional: Rotate image for mobile camera (uncomment if needed)
/*
Color32[] RotateImage(Color32[] pixels, int width, int height)
{
    Color32[] rotated = new Color32[pixels.Length];
    for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
            rotated[x * height + (height - y - 1)] = pixels[y * width + x];
    return rotated;
}
*/



//using UnityEngine;
//using TMPro;
//using Unity.Barracuda;

//public class YOLOController : MonoBehaviour
//{
//    public NNModel modelAsset;               // Assign in Inspector
//    private Model runtimeModel;
//    private IWorker worker;

//    public Texture2D inputImage;             // Assign an image
//    public RenderTexture outputTexture;      // Optional: for visualization
//    public TextMeshProUGUI debugText;


//    void Start()
//    {
//        runtimeModel = ModelLoader.Load(modelAsset);
//        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

//        // Resize image to 640x640 before running YOLO
//        Texture2D resizedImage = ResizeTexture(inputImage, 640, 640);
//        RunYOLO(resizedImage);
//    }

//    Texture2D ResizeTexture(Texture2D source, int width, int height)
//    {
//        RenderTexture rt = RenderTexture.GetTemporary(width, height);
//        RenderTexture.active = rt;
//        Graphics.Blit(source, rt);

//        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
//        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        result.Apply();

//        RenderTexture.ReleaseTemporary(rt);
//        RenderTexture.active = null;

//        return result;
//    }

//    void RunYOLO(Texture2D image)
//    {
//        Tensor input = Preprocess(image);
//        worker.Execute(input);
//        Tensor output = worker.PeekOutput();

//        Postprocess(output);
//        input.Dispose();
//        output.Dispose();
//    }

//    Tensor Preprocess(Texture2D texture)
//    {
//        Color32[] pixels = texture.GetPixels32();
//        float[] floatValues = new float[pixels.Length * 3];
//        for (int i = 0; i < pixels.Length; ++i)
//        {
//            Color32 pixel = pixels[i];
//            floatValues[i * 3 + 0] = pixel.r / 255.0f;
//            floatValues[i * 3 + 1] = pixel.g / 255.0f;
//            floatValues[i * 3 + 2] = pixel.b / 255.0f;
//        }

//        return new Tensor(1, texture.height, texture.width, 3, floatValues);
//    }

//    void Postprocess(Tensor output)
//    {
//        Debug.Log($"Output shape: {output.shape}");  // Log shape
//        Debug.Log($"Output length: {output.length}");
//        Debug.Log($"Sample value: {output[0]}");

//        string summary = $"Output shape: {output.shape}\n";
//        summary += $"Output length: {output.length}\n";
//        summary += $"Sample[0]: {output[0]}\n";
//        summary += $"Sample[1]: {output[1]}\n";
//        summary += $"Sample[2]: {output[2]}\n";
//        summary += $"Sample[3]: {output[3]}\n";
//        debugText.text = summary;

//        int numBoxes = output.shape.width; // 8400
//        int features = output.shape.channels; // 64

//        float confThreshold = 0.5f;

//        for (int i = 0; i < numBoxes; i++)
//        {
//            float objConfidence = output[0, 0, i, 4]; // objectness score

//            if (objConfidence < confThreshold)
//                continue;

//            // Extract bbox center x, y, width, height
//            float cx = output[0, 0, i, 0];
//            float cy = output[0, 0, i, 1];
//            float w = output[0, 0, i, 2];
//            float h = output[0, 0, i, 3];

//            // Get class index with highest score
//            int classId = 0;
//            float maxClassScore = 0f;
//            for (int j = 5; j < features; j++)
//            {
//                float score = output[0, 0, i, j];
//                if (score > maxClassScore)
//                {
//                    maxClassScore = score;
//                    classId = j - 5;
//                }
//            }

//            float finalConfidence = objConfidence * maxClassScore;
//            if (finalConfidence < confThreshold)
//                continue;

//            // Convert (cx, cy, w, h) to (x1, y1, x2, y2)
//            float x1 = cx - w / 2;
//            float y1 = cy - h / 2;
//            float x2 = cx + w / 2;
//            float y2 = cy + h / 2;

//            // Optional: scale back to original resolution
//            Debug.Log($"Box: ({x1}, {y1}) - ({x2}, {y2}), class: {classId}, conf: {finalConfidence}");

//            // TODO: Draw box in Unity (e.g., with GUI or RectTransform)
//        }
//    }

//    void OnDestroy()
//    {
//        worker.Dispose();
//    }
//}


