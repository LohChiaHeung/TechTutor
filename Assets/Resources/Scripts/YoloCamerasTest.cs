using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class YoloCamerasTest : MonoBehaviour
{
    [Header("Model & UI")]
    public NNModel yoloModelAsset;
    public RawImage imageDisplay;

    [Header("AR Camera")]
    public ARCameraManager arCameraManager;

    [HideInInspector] public bool snapshotTaken = false;
    [HideInInspector] public Texture2D snapshotTexture;
    [HideInInspector] public List<YoloBox> snapshotDetections;

    private Model runtimeModel;
    private IWorker worker;

    private Texture2D cpuTexture;
    private bool captureRequested = false;

    private YoloOverlay overlay;

    [Header("Inference Settings")]
    public float detectionThreshold = 0.5f;
    private readonly string[] classLabels = new string[]
    {
        "0","1","2","3","4","5","6","7","8","9",
        "a","accent","ae","alt-left","altgr-right","b","c","caret","comma","d",
        "del","e","enter","f","g","h","hash","i","j","k","keyboard","l","less",
        "m","minus","n","o","oe","p","plus","point","q","r","s","shift-left",
        "shift-lock","shift-right","space","ss","strg-left","strg-right","t","tab",
        "u","ue","v","w","x","y","z"
    };

    void Start()
    {
        // Load and prepare the YOLO model
        runtimeModel = ModelLoader.Load(yoloModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        overlay = FindObjectOfType<YoloOverlay>();
    }

    void OnEnable()
    {
        arCameraManager.frameReceived += OnCameraFrame;
    }

    void OnDisable()
    {
        arCameraManager.frameReceived -= OnCameraFrame;
    }

    void OnDestroy()
    {
        worker?.Dispose();
    }

    /// <summary>
    /// Called by the UI Button to request a snapshot.
    /// </summary>
    public void OnCaptureButtonPressed()
    {
        if (snapshotTaken) return;
        captureRequested = true;
    }

    void OnCameraFrame(ARCameraFrameEventArgs args)
    {
        // If user tapped capture, and we haven't taken snapshot yet
        if (!captureRequested || snapshotTaken) return;

        // Try to get the latest CPU image
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            return;

        // Mark flags to avoid repeated captures
        captureRequested = false;
        snapshotTaken = true;

        // Prepare CPU texture
        if (cpuTexture == null || cpuTexture.width != image.width || cpuTexture.height != image.height)
        {
            cpuTexture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        }

        // Convert image to Texture2D
        var convParams = new XRCpuImage.ConversionParams(
            image,
            TextureFormat.RGBA32,
            XRCpuImage.Transformation.None
        );
        var rawData = cpuTexture.GetRawTextureData<byte>();
        image.Convert(convParams, rawData);
        cpuTexture.Apply();
        image.Dispose();

        // Show the snapshot in the UI (optional)
        imageDisplay.texture = cpuTexture;

        // Crop & resize for model input
        Texture2D inputTex = CropAndResize(cpuTexture, 480, 480, 640, 640);

        // Run inference
        using (var input = new Tensor(inputTex, 3))
        {
            worker.Execute(input);
            var output = worker.PeekOutput("output0");
            snapshotDetections = ParseDetections(output, 640, 640);
            output.Dispose();
        }

        // Store the snapshot texture for AR placement
        snapshotTexture = cpuTexture;

        // Optionally unsubscribe to stop processing further frames
        arCameraManager.frameReceived -= OnCameraFrame;
    }

    // Helper: crop center and resize
    Texture2D CropAndResize(Texture2D source, int cropW, int cropH, int targetW, int targetH)
    {
        int x = (source.width - cropW) / 2;
        int y = (source.height - cropH) / 2;
        Color[] pix = source.GetPixels(x, y, cropW, cropH);
        Texture2D crop = new Texture2D(cropW, cropH, source.format, false);
        crop.SetPixels(pix);
        crop.Apply();
        return ResizeTexture(crop, targetW, targetH);
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, source.format, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    List<YoloBox> ParseDetections(Tensor output, float imageWidth, float imageHeight)
    {
        var boxes = new List<YoloBox>();
        int numBoxes = output.shape.channels;
        int numVals = output.shape.width;
        for (int i = 0; i < numBoxes; i++)
        {
            float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));
            float x = output[0, 0, 0, i];
            float y = output[0, 0, 1, i];
            float w = output[0, 0, 2, i];
            float h = output[0, 0, 3, i];
            float obj = Sigmoid(output[0, 0, 4, i]);
            float bestC = 0f;
            int classId = -1;
            for (int c = 5; c < numVals; c++)
            {
                float score = Sigmoid(output[0, 0, c, i]);
                if (score > bestC) { bestC = score; classId = c - 5; }
            }
            float conf = obj * bestC;
            if (conf > detectionThreshold)
            {
                string label = (classId >= 0 && classId < classLabels.Length)
                    ? classLabels[classId]
                    : $"class_{classId}";
                boxes.Add(new YoloBox { x = x, y = y, w = w, h = h, label = label, confidence = conf });
            }
        }
        return boxes;
    }
}

// Your YoloBox definition remains the same:
//public class YoloBox
//{
//    public float x, y, w, h;
//    public string label;
//    public float confidence;
//}
