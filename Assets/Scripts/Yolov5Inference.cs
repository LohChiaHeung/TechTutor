using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Barracuda;
using System.IO;

public class YOLOv5Inference : MonoBehaviour
{
    [Header("Model Configuration")]
    public NNModel modelAsset;
    public Texture2D inputImage;
    public float confidenceThreshold = 0.5f;
    public float nmsThreshold = 0.4f;

    [Header("Visualization")]
    public GameObject boundingBoxPrefab;
    public Canvas canvas;
    public Color[] classColors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.magenta };

    private Model model;
    private IWorker worker;
    private int inputWidth = 416;
    private int inputHeight = 416;
    private string[] classNames;

    // Store detection results
    private List<Detection> detections = new List<Detection>();
    private List<GameObject> boundingBoxes = new List<GameObject>();

    [System.Serializable]
    public class Detection
    {
        public float x, y, width, height;
        public float confidence;
        public int classId;
        public string className;
    }

    void Start()
    {
        InitializeModel();
        InitializeClassNames();

        if (inputImage != null)
        {
            RunInference();
        }
    }

    void InitializeModel()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Model asset is not assigned!");
            return;
        }

        model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);

        // Get input dimensions from model
        var inputShape = model.inputs[0].shape;
        if (inputShape.Length >= 4)
        {
            inputHeight = inputShape[2];
            inputWidth = inputShape[3];
        }

        Debug.Log($"Model loaded successfully. Input size: {inputWidth}x{inputHeight}");
    }

    void InitializeClassNames()
    {
        // Define your class names based on your trained model
        // Replace these with your actual class names
        classNames = new string[]
        {
            "keyboard", "key", "spacebar", "enter", "shift", "ctrl", "alt"
            // Add more classes as needed based on your model
        };
    }

    void RunInference()
    {
        if (worker == null || inputImage == null)
        {
            Debug.LogError("Worker or input image is null!");
            return;
        }

        // Clear previous detections
        ClearBoundingBoxes();
        detections.Clear();

        // Preprocess image
        Tensor inputTensor = PreprocessImage(inputImage);

        // Run inference
        worker.Execute(inputTensor);

        // Get output
        Tensor outputTensor = worker.PeekOutput();

        // Post-process results
        ProcessOutput(outputTensor);

        // Visualize results
        VisualizeDetections();

        // Clean up
        inputTensor.Dispose();
        outputTensor.Dispose();

        Debug.Log($"Inference completed. Found {detections.Count} detections.");
    }

    Tensor PreprocessImage(Texture2D image)
    {
        // Resize image to model input size
        Texture2D resizedImage = ResizeTexture(image, inputWidth, inputHeight);

        // Convert to tensor (normalized to 0-1 range)
        float[] imageData = new float[inputWidth * inputHeight * 3];
        Color[] pixels = resizedImage.GetPixels();

        for (int i = 0; i < pixels.Length; i++)
        {
            imageData[i * 3] = pixels[i].r;     // R channel
            imageData[i * 3 + 1] = pixels[i].g; // G channel
            imageData[i * 3 + 2] = pixels[i].b; // B channel
        }

        // Create tensor with shape [1, 3, height, width] for YOLOv5
        Tensor inputTensor = new Tensor(1, 3, inputHeight, inputWidth, imageData);

        return inputTensor;
    }

    Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;

        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(newWidth, newHeight);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    void ProcessOutput(Tensor output)
    {
        // YOLOv5 output format: [batch_size, num_detections, 5 + num_classes]
        // Each detection: [x, y, width, height, confidence, class_scores...]

        int batchSize = output.shape[0];
        int numDetections = output.shape[1];
        int numFeatures = output.shape[2];
        int numClasses = numFeatures - 5;

        List<Detection> rawDetections = new List<Detection>();

        // Convert tensor to flat array for easier access
        float[] outputData = output.ToReadOnlyArray();

        for (int i = 0; i < numDetections; i++)
        {
            // Calculate flat index for this detection
            int detectionOffset = i * numFeatures;

            float confidence = outputData[detectionOffset + 4];

            if (confidence < confidenceThreshold)
                continue;

            // Find best class
            float maxClassScore = 0f;
            int bestClassId = 0;

            for (int c = 0; c < numClasses; c++)
            {
                float classScore = outputData[detectionOffset + 5 + c];
                if (classScore > maxClassScore)
                {
                    maxClassScore = classScore;
                    bestClassId = c;
                }
            }

            float finalConfidence = confidence * maxClassScore;

            if (finalConfidence < confidenceThreshold)
                continue;

            Detection detection = new Detection
            {
                x = outputData[detectionOffset + 0],
                y = outputData[detectionOffset + 1],
                width = outputData[detectionOffset + 2],
                height = outputData[detectionOffset + 3],
                confidence = finalConfidence,
                classId = bestClassId,
                className = bestClassId < classNames.Length ? classNames[bestClassId] : "unknown"
            };

            rawDetections.Add(detection);
        }

        // Apply Non-Maximum Suppression
        detections = ApplyNMS(rawDetections);
    }

    List<Detection> ApplyNMS(List<Detection> detections)
    {
        // Sort by confidence (descending)
        detections.Sort((a, b) => b.confidence.CompareTo(a.confidence));

        List<Detection> result = new List<Detection>();

        while (detections.Count > 0)
        {
            Detection best = detections[0];
            result.Add(best);
            detections.RemoveAt(0);

            // Remove overlapping detections
            for (int i = detections.Count - 1; i >= 0; i--)
            {
                if (CalculateIoU(best, detections[i]) > nmsThreshold)
                {
                    detections.RemoveAt(i);
                }
            }
        }

        return result;
    }

    float CalculateIoU(Detection a, Detection b)
    {
        float x1 = Mathf.Max(a.x - a.width / 2, b.x - b.width / 2);
        float y1 = Mathf.Max(a.y - a.height / 2, b.y - b.height / 2);
        float x2 = Mathf.Min(a.x + a.width / 2, b.x + b.width / 2);
        float y2 = Mathf.Min(a.y + a.height / 2, b.y + b.height / 2);

        float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);
        float areaA = a.width * a.height;
        float areaB = b.width * b.height;
        float union = areaA + areaB - intersection;

        return intersection / union;
    }

    void VisualizeDetections()
    {
        if (canvas == null || boundingBoxPrefab == null)
            return;

        foreach (var detection in detections)
        {
            GameObject bbox = Instantiate(boundingBoxPrefab, canvas.transform);
            boundingBoxes.Add(bbox);

            // Convert normalized coordinates to screen coordinates
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 canvasSize = canvasRect.sizeDelta;

            float x = detection.x * canvasSize.x;
            float y = (1 - detection.y) * canvasSize.y; // Flip Y coordinate
            float width = detection.width * canvasSize.x;
            float height = detection.height * canvasSize.y;

            RectTransform bboxRect = bbox.GetComponent<RectTransform>();
            bboxRect.anchoredPosition = new Vector2(x, y);
            bboxRect.sizeDelta = new Vector2(width, height);

            // Set color based on class
            var image = bbox.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = classColors[detection.classId % classColors.Length];
            }

            // Add label
            var text = bbox.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null)
            {
                text.text = $"{detection.className} {detection.confidence:F2}";
            }

            Debug.Log($"Detection: {detection.className} ({detection.confidence:F2}) at ({detection.x:F2}, {detection.y:F2})");
        }
    }

    void ClearBoundingBoxes()
    {
        foreach (var bbox in boundingBoxes)
        {
            if (bbox != null)
                DestroyImmediate(bbox);
        }
        boundingBoxes.Clear();
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    // Public method to run inference from UI button
    public void RunInferenceButton()
    {
        RunInference();
    }

    // Method to load new image
    public void LoadImageFromFile(string path)
    {
        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(fileData);
            inputImage = texture;
            RunInference();
        }
    }
}