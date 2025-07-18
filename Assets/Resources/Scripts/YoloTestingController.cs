using UnityEngine;
using Unity.Barracuda;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class YoloTestingController : MonoBehaviour
{
    [Header("Model Settings")]
    public NNModel modelAsset;

    [Header("UI Components")]
    public RawImage cameraView;
    public RectTransform canvasRect;
    public GameObject labelPrefab;

    [Header("Detection Settings")]
    [Range(0.1f, 1f)]
    public float confidenceThreshold = 0.25f;
    [Range(0.1f, 1f)]
    public float nmsThreshold = 0.45f;

    private Model runtimeModel;
    private IWorker worker;
    private const int INPUT_SIZE = 640;

    string[] classNames = new string[] {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "a", "accent", "ae", "alt-left", "altgr-right", "b", "c", "caret", "comma", "d",
        "del", "e", "enter", "f", "g", "h", "hash", "i", "j", "k",
        "keyboard", "l", "less", "m", "minus", "n", "o", "oe", "p", "plus",
        "point", "q", "r", "s", "shift-left", "shift-lock", "shift-right", "space", "ss", "strg-left",
        "strg-right", "t", "tab", "u", "ue", "v", "w", "x", "y", "z"
    };

    void Start()
    {
        Debug.Log("✅ Start() called");

        if (!ValidateComponents())
            return;

        InitializeModel();
        LoadAndProcessTestImage();
    }

    bool ValidateComponents()
    {
        if (modelAsset == null)
        {
            Debug.LogError("❌ Model asset is NULL. Please assign in Inspector.");
            return false;
        }

        if (cameraView == null || canvasRect == null || labelPrefab == null)
        {
            Debug.LogError("❌ Missing UI components. Please assign all required components in Inspector.");
            return false;
        }

        return true;
    }

    void InitializeModel()
    {
        try
        {
            // Load model with explicit worker type that's more compatible
            runtimeModel = ModelLoader.Load(modelAsset);

            // Log model information
            Debug.Log($"📋 Model inputs: {string.Join(", ", runtimeModel.inputs.Select(i => $"{i.name}:{i.shape}"))}");
            Debug.Log($"📋 Model outputs: {string.Join(", ", runtimeModel.outputs)}");

            // Use ComputePrecompiled for better compatibility with complex models
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, runtimeModel);
            Debug.Log("✅ Model loaded and worker created");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Failed to initialize model: {e.Message}");
        }
    }

    void LoadAndProcessTestImage()
    {
        Texture2D testImage = Resources.Load<Texture2D>("key_test");
        if (testImage == null)
        {
            Debug.LogError("❌ Test image not found! Place 'key_test.png' in Assets/Resources/");
            return;
        }

        Texture2D resized = ResizeTexture(testImage, INPUT_SIZE, INPUT_SIZE);
        cameraView.texture = resized;
        Debug.Log("✅ Test image loaded and resized");

        RunYOLO(resized);
    }

    float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

    void RunYOLO(Texture2D inputTex)
    {
        if (worker == null)
        {
            Debug.LogError("❌ Worker is null, cannot run inference");
            return;
        }

        Tensor input = null;
        try
        {
            input = TransformInput(inputTex);
            Debug.Log($"📤 Input tensor shape: {input.shape}");

            // Get the correct input name from the model
            string inputName = runtimeModel.inputs[0].name;
            Debug.Log($"📤 Using input name: {inputName}");

            // Execute model with proper input dictionary
            var inputs = new Dictionary<string, Tensor> { { inputName, input } };
            worker.Execute(inputs);

            // Get output tensor
            string outputName = runtimeModel.outputs[0];
            using var output = worker.PeekOutput(outputName);
            Debug.Log($"📦 Output shape: {output.shape}");

            ProcessDetections(output);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error during YOLO inference: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            input?.Dispose();
        }
    }

    void ProcessDetections(Tensor output)
    {
        // Clear previous detections
        foreach (Transform child in canvasRect)
            Destroy(child.gameObject);

        // Handle different possible output formats
        if (output.shape.rank == 2)
        {
            // Format: [num_detections, 65] - already processed
            ProcessDetectionsFormat1(output);
        }
        else if (output.shape.rank == 3 && output.shape[0] == 1)
        {
            // Format: [1, num_detections, 65] - common YOLOv5 format
            ProcessDetectionsFormat2(output);
        }
        else
        {
            Debug.LogError($"❌ Unsupported output format: {output.shape}");
        }
    }

    void ProcessDetectionsFormat1(Tensor output)
    {
        int numBoxes = output.shape[0];
        int numFeatures = output.shape[1];

        if (numFeatures < 5 + classNames.Length)
        {
            Debug.LogError($"❌ Insufficient features in output: {numFeatures}, expected at least {5 + classNames.Length}");
            return;
        }

        ProcessDetectionData(output.ToReadOnlyArray(), numBoxes, numFeatures);
    }

    void ProcessDetectionsFormat2(Tensor output)
    {
        int numBoxes = output.shape[1];
        int numFeatures = output.shape[2];

        if (numFeatures < 5 + classNames.Length)
        {
            Debug.LogError($"❌ Insufficient features in output: {numFeatures}, expected at least {5 + classNames.Length}");
            return;
        }

        ProcessDetectionData(output.ToReadOnlyArray(), numBoxes, numFeatures);
    }

    void ProcessDetectionData(float[] data, int numBoxes, int numFeatures)
    {
        var detections = new List<Detection>();

        for (int i = 0; i < numBoxes; i++)
        {
            int offset = i * numFeatures;

            // Extract box coordinates (center format)
            float x = data[offset + 0];
            float y = data[offset + 1];
            float w = data[offset + 2];
            float h = data[offset + 3];
            float objConf = Sigmoid(data[offset + 4]);

            // Skip low confidence detections early
            if (objConf < confidenceThreshold)
                continue;

            // Find best class
            float maxClassConf = 0f;
            int classId = -1;

            for (int c = 0; c < classNames.Length && c < (numFeatures - 5); c++)
            {
                float classConf = Sigmoid(data[offset + 5 + c]);
                if (classConf > maxClassConf)
                {
                    maxClassConf = classConf;
                    classId = c;
                }
            }

            float score = objConf * maxClassConf;

            if (score > confidenceThreshold && classId >= 0 && classId < classNames.Length)
            {
                detections.Add(new Detection
                {
                    x = x,
                    y = y,
                    w = w,
                    h = h,
                    confidence = score,
                    classId = classId
                });
            }
        }

        // Apply Non-Maximum Suppression
        var finalDetections = ApplyNMS(detections, nmsThreshold);

        // Draw detections
        DrawDetections(finalDetections);

        Debug.Log($"🟩 Total Detections after NMS: {finalDetections.Count}");
    }

    List<Detection> ApplyNMS(List<Detection> detections, float nmsThreshold)
    {
        var result = new List<Detection>();
        var sorted = detections.OrderByDescending(d => d.confidence).ToList();

        while (sorted.Count > 0)
        {
            var best = sorted[0];
            result.Add(best);
            sorted.RemoveAt(0);

            for (int i = sorted.Count - 1; i >= 0; i--)
            {
                if (CalculateIoU(best, sorted[i]) > nmsThreshold)
                {
                    sorted.RemoveAt(i);
                }
            }
        }

        return result;
    }

    float CalculateIoU(Detection a, Detection b)
    {
        float x1 = Mathf.Max(a.x - a.w / 2, b.x - b.w / 2);
        float y1 = Mathf.Max(a.y - a.h / 2, b.y - b.h / 2);
        float x2 = Mathf.Min(a.x + a.w / 2, b.x + b.w / 2);
        float y2 = Mathf.Min(a.y + a.h / 2, b.y + b.h / 2);

        float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float union = a.w * a.h + b.w * b.h - intersection;

        return union > 0 ? intersection / union : 0;
    }

    void DrawDetections(List<Detection> detections)
    {
        foreach (var detection in detections)
        {
            Debug.Log($"🔍 DETECTED → class={classNames[detection.classId]}, score={detection.confidence:F2}");

            // Convert from normalized coordinates to canvas coordinates
            float xMin = (detection.x - detection.w / 2f) * canvasRect.rect.width;
            float yMin = (detection.y - detection.h / 2f) * canvasRect.rect.height;
            float boxWidth = detection.w * canvasRect.rect.width;
            float boxHeight = detection.h * canvasRect.rect.height;

            // Flip Y coordinate for Unity UI
            float yFlipped = canvasRect.rect.height - yMin - boxHeight;

            GameObject label = Instantiate(labelPrefab, canvasRect);
            RectTransform rt = label.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(xMin, yFlipped);
            rt.sizeDelta = new Vector2(boxWidth, boxHeight);

            TextMeshProUGUI labelText = label.GetComponentInChildren<TextMeshProUGUI>();
            if (labelText != null)
            {
                labelText.text = $"{classNames[detection.classId]} ({detection.confidence:F2})";
            }
        }
    }

    Tensor TransformInput(Texture2D tex)
    {
        // Ensure texture is correct size
        // Ensure texture is correct size
        // Ensure texture is correct size
        if (tex.width != INPUT_SIZE || tex.height != INPUT_SIZE)
        {
            tex = ResizeTexture(tex, INPUT_SIZE, INPUT_SIZE);
        }

        Color32[] pixels = tex.GetPixels32();
        float[] input = new float[3 * INPUT_SIZE * INPUT_SIZE];

        // Convert to CHW format (Channels, Height, Width)
        for (int c = 0; c < 3; c++)
        {
            for (int y = 0; y < INPUT_SIZE; y++)
            {
                for (int x = 0; x < INPUT_SIZE; x++)
                {
                    int pixelIndex = y * INPUT_SIZE + x;
                    int inputIndex = c * INPUT_SIZE * INPUT_SIZE + y * INPUT_SIZE + x;

                    Color32 pixel = pixels[pixelIndex];

                    // Normalize to [0, 1] range
                    switch (c)
                    {
                        case 0: input[inputIndex] = pixel.r / 255f; break;
                        case 1: input[inputIndex] = pixel.g / 255f; break;
                        case 2: input[inputIndex] = pixel.b / 255f; break;
                    }
                }
            }
        }

        // Create tensor with correct shape for YOLOv5: [batch, channels, height, width]
        return new Tensor(new TensorShape(1, 3, INPUT_SIZE, INPUT_SIZE), input);
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        RenderTexture.active = rt;

        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        rt.Release();

        return result;
    }

    void OnDestroy()
    {
        worker?.Dispose();
        worker = null;
    }

    [System.Serializable]
    public class Detection
    {
        public float x, y, w, h;
        public float confidence;
        public int classId;
    }
}