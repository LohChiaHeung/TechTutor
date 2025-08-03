//using UnityEngine;
//using Unity.Barracuda;
//using System.Collections.Generic;
//using UnityEngine.UI;
//using System.Linq;

//public class YoloKeyboardDetector : MonoBehaviour
//{
//    public NNModel onnxModel;
//    public Texture2D testImage;
//    public float confidenceThreshold = 0.5f;
//    public float nmsIoUThreshold = 0.45f;

//    public RawImage imageDisplay;

//    private Model model;
//    private IWorker worker;
//    private const int INPUT_SIZE = 416;

//    private readonly string[] classLabels = new string[]
//    {
//        "keyboard"
//    };

//    private readonly float[] anchors_8 = { 10, 13, 16, 30, 33, 23 };
//    private readonly float[] anchors_16 = { 30, 61, 62, 45, 59, 119 };
//    private readonly float[] anchors_32 = { 116, 90, 156, 198, 373, 326 };

//    void Start()
//    {
//        if (onnxModel == null || testImage == null)
//        {
//            Debug.LogError("Assign ONNX model and test image in inspector!");
//            return;
//        }

//        model = ModelLoader.Load(onnxModel);
//        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);

//        Texture2D resized = ResizeTexture(testImage, INPUT_SIZE, INPUT_SIZE);
//        imageDisplay.texture = resized;

//        Debug.Log($"✅ Resized image: {resized.width} x {resized.height}");

//        Tensor inputTensor = new Tensor(resized, 3);
//        worker.Execute(new Dictionary<string, Tensor> { { "images", inputTensor } });

//        Tensor output = worker.PeekOutput("output0");
//        Debug.Log($"✅ Inference completed. Output shape: {output.shape}");

//        int numBoxes = output.shape.channels;
//        int numAttributes = output.shape.width;

//        List<YoloBox> boxes = new List<YoloBox>();

//        for (int i = 0; i < numBoxes; i++)
//        {
//            float x_raw = output[0, 0, 0, i];
//            float y_raw = output[0, 0, 1, i];
//            float w_raw = output[0, 0, 2, i];
//            float h_raw = output[0, 0, 3, i];
//            float objConf = Sigmoid(output[0, 0, 4, i]);

//            float maxClassConf = 0f;
//            int classId = -1;

//            for (int c = 5; c < numAttributes; c++)
//            {
//                float classScore = Sigmoid(output[0, 0, c, i]);
//                if (classScore > maxClassConf)
//                {
//                    maxClassConf = classScore;
//                    classId = c - 5;
//                }
//            }

//            float finalConf = objConf * maxClassConf;

//            if (finalConf > confidenceThreshold)
//            {
//                int anchorIndex = i % 3;
//                float stride;
//                float anchorW, anchorH;
//                int gridW, gridH;
//                int cellIndex;

//                if (i < 8112) // stride 8
//                {
//                    stride = 8;
//                    gridW = 52;
//                    gridH = 52;
//                    anchorW = anchors_8[anchorIndex * 2];
//                    anchorH = anchors_8[anchorIndex * 2 + 1];
//                    cellIndex = i / 3;
//                }
//                else if (i < 10140) // stride 16
//                {
//                    stride = 16;
//                    gridW = 26;
//                    gridH = 26;
//                    anchorW = anchors_16[anchorIndex * 2];
//                    anchorH = anchors_16[anchorIndex * 2 + 1];
//                    cellIndex = (i - 8112) / 3;
//                }
//                else // stride 32
//                {
//                    stride = 32;
//                    gridW = 13;
//                    gridH = 13;
//                    anchorW = anchors_32[anchorIndex * 2];
//                    anchorH = anchors_32[anchorIndex * 2 + 1];
//                    cellIndex = (i - 10140) / 3;
//                }

//                int grid_x = cellIndex % gridW;
//                int grid_y = cellIndex / gridW;

//                float x_center = (Sigmoid(x_raw) + grid_x) * stride;
//                float y_center = (Sigmoid(y_raw) + grid_y) * stride;
//                float w = Mathf.Pow(2 * Sigmoid(w_raw), 2) * anchorW;
//                float h = Mathf.Pow(2 * Sigmoid(h_raw), 2) * anchorH;

//                w = Mathf.Min(w, INPUT_SIZE);
//                h = Mathf.Min(h, INPUT_SIZE);

//                float left = Mathf.Max(x_center - w / 2f, 0);
//                float top = Mathf.Max(y_center - h / 2f, 0);
//                float right = Mathf.Min(left + w, INPUT_SIZE);
//                float bottom = Mathf.Min(top + h, INPUT_SIZE);

//                w = right - left;
//                h = bottom - top;

//                x_center = left + w / 2f;
//                y_center = top + h / 2f;

//                if (float.IsNaN(w) || float.IsInfinity(w)) w = anchorW;
//                if (float.IsNaN(h) || float.IsInfinity(h)) h = anchorH;

//                Debug.Log($"📦 Box {i}: x_center={x_center}, y_center={y_center}, w={w}, h={h}, obj_conf={objConf:F2}, class_conf={maxClassConf:F2}, final_conf={finalConf:F2}");

//                boxes.Add(new YoloBox
//                {
//                    x = x_center,
//                    y = y_center,
//                    w = w,
//                    h = h,
//                    label = classLabels[classId >= 0 && classId < classLabels.Length ? classId : 0],
//                    confidence = finalConf
//                });
//            }
//        }

//        Debug.Log($"✅ Total raw detections: {boxes.Count}");

//        // Apply NMS
//        var finalDetections = ApplyNMS(boxes, nmsIoUThreshold);
//        Debug.Log($"✅ Detections after NMS: {finalDetections.Count}");

//        foreach (var b in finalDetections)
//        {
//            Debug.Log($"✅ NMS box: center=({b.x:F1},{b.y:F1}) size=({b.w:F1},{b.h:F1}) label={b.label} conf={b.confidence:F2}");
//        }

//        inputTensor.Dispose();
//        output.Dispose();
//        worker.Dispose();
//    }

//    private float Sigmoid(float x)
//    {
//        return 1f / (1f + Mathf.Exp(-x));
//    }

//    private Texture2D ResizeTexture(Texture2D src, int width, int height)
//    {
//        RenderTexture rt = RenderTexture.GetTemporary(width, height);
//        Graphics.Blit(src, rt);
//        RenderTexture.active = rt;
//        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
//        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        result.Apply();
//        RenderTexture.active = null;
//        RenderTexture.ReleaseTemporary(rt);
//        return result;
//    }

//    private List<YoloBox> ApplyNMS(List<YoloBox> boxes, float iouThreshold)
//    {
//        var keep = new List<YoloBox>();
//        var sorted = boxes.OrderByDescending(b => b.confidence).ToList();

//        while (sorted.Count > 0)
//        {
//            var best = sorted[0];
//            keep.Add(best);
//            sorted.RemoveAt(0);

//            sorted.RemoveAll(box => IoU(best, box) > iouThreshold);
//        }
//        return keep;
//    }

//    private float IoU(YoloBox a, YoloBox b)
//    {
//        float x1 = Mathf.Max(a.x - a.w / 2, b.x - b.w / 2);
//        float y1 = Mathf.Max(a.y - a.h / 2, b.y - b.h / 2);
//        float x2 = Mathf.Min(a.x + a.w / 2, b.x + b.w / 2);
//        float y2 = Mathf.Min(a.y + a.h / 2, b.y + b.h / 2);

//        float interW = Mathf.Max(0, x2 - x1);
//        float interH = Mathf.Max(0, y2 - y1);
//        float interArea = interW * interH;

//        float areaA = a.w * a.h;
//        float areaB = b.w * b.h;
//        float union = areaA + areaB - interArea;

//        return union > 0 ? interArea / union : 0;
//    }

//    [System.Serializable]
//    public class YoloBox
//    {
//        public float x;
//        public float y;
//        public float w;
//        public float h;
//        public string label;
//        public float confidence;
//    }
//}

//using UnityEngine;
//using Unity.Barracuda;
//using UnityEngine.UI;
//using System.Collections.Generic;
//using UnityEngine.SceneManagement;

//public class YoloKeyboardDetector : MonoBehaviour
//{
//    [Header("YOLO Models")]
//    public NNModel keyboardModel;         // Model 1: Keyboard-only
//    public NNModel multiClassModel;       // Model 2: All 5 classes
//    public float confidenceThreshold = 0.55f;

//    [Header("Component Panels")]
//    public GameObject keyboardPanel;
//    public GameObject monitorPanel;
//    public GameObject mousePanel;
//    public GameObject speakerPanel;
//    public GameObject laptopPanel;

//    [Header("UI")]
//    public RawImage cameraDisplay;

//    [Header("Detection Settings")]
//    public float detectionInterval = 0.5f;

//    private IWorker keyboardWorker;
//    private IWorker multiWorker;
//    private WebCamTexture webcam;
//    private float detectionTimer = 0f;
//    private const int INPUT_SIZE = 416;
//    private Dictionary<int, GameObject> panelByClass;

//    void Start()
//    {
//        // Load models
//        keyboardWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, ModelLoader.Load(keyboardModel));
//        multiWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, ModelLoader.Load(multiClassModel));
//        Debug.Log("✅ Both models loaded.");

//        // Start webcam
//        WebCamDevice[] devices = WebCamTexture.devices;
//        if (devices.Length == 0)
//        {
//            Debug.LogError("❌ No webcam found.");
//            return;
//        }

//        webcam = new WebCamTexture(devices[0].name, 1280, 720);
//        webcam.Play();
//        cameraDisplay.texture = webcam;

//        foreach (var panel in panelByClass.Values)
//            panel.SetActive(false);
//    }

//    void Awake()
//    {
//        panelByClass = new Dictionary<int, GameObject>
//    {
//        { 0, mousePanel },
//        { 1, keyboardPanel },
//        { 2, monitorPanel },
//        { 3, speakerPanel },
//        { 4, laptopPanel }
//    };
//    }
//    void Update()
//    {
//        if (webcam == null || !webcam.didUpdateThisFrame) return;

//        detectionTimer += Time.deltaTime;
//        if (detectionTimer < detectionInterval) return;
//        detectionTimer = 0f;

//        Texture2D frame = new Texture2D(webcam.width, webcam.height);
//        frame.SetPixels(webcam.GetPixels());
//        frame.Apply();

//        Texture2D resized = ResizeTexture(frame, INPUT_SIZE, INPUT_SIZE);
//        Tensor input = new Tensor(resized, 3);

//        keyboardWorker.Execute(new Dictionary<string, Tensor> { { "images", input } });
//        Tensor kbOutput = keyboardWorker.PeekOutput("output0");
//        List<YoloBox> kbDetections = ParseKeyboardDetections(kbOutput);

//        multiWorker.Execute(new Dictionary<string, Tensor> { { "images", input } });
//        Tensor multiOutput = multiWorker.PeekOutput("output0");
//        List<YoloBox> multiDetections = ParseMultiDetections(multiOutput);

//        // Step 1: Hide all panels
//        foreach (var panel in panelByClass.Values)
//            panel.SetActive(false);

//        // Step 2: Show panels based on detections
//        // Step 2: Determine the most confident detection
//        YoloBox topDetection = null;

//        // Include keyboard from model 1 (classId = 1)
//        if (kbDetections.Count > 0)
//        {
//            topDetection = new YoloBox { confidence = kbDetections[0].confidence, classId = 1 };
//            Debug.Log("✅ Keyboard detected from Model 1");
//        }

//        // Check all detections from model 2
//        foreach (var box in multiDetections)
//        {
//            if (topDetection == null || box.confidence > topDetection.confidence)
//            {
//                topDetection = box;
//            }
//        }

//        // Step 3: Show only the top detection panel
//        if (topDetection != null && topDetection.confidence >= confidenceThreshold && panelByClass.ContainsKey(topDetection.classId))
//        {
//            panelByClass[topDetection.classId].SetActive(true);
//            string name = GetClassName(topDetection.classId);
//            Debug.Log($"⭐ Showing panel: {name} (classId: {topDetection.classId}) with confidence: {topDetection.confidence:F2}");
//        }
//        else
//        {
//            Debug.Log("❌ No confident detection found.");
//        }

//        // Cleanup
//        input.Dispose();
//        kbOutput.Dispose();
//        multiOutput.Dispose();
//        Destroy(frame);
//        Destroy(resized);
//    }



//    private string GetClassName(int classId)
//    {
//        switch (classId)
//        {
//            case 0: return "Mouse";
//            case 1: return "Keyboard";
//            case 2: return "Monitor";
//            case 3: return "Speaker";
//            case 4: return "Laptop";
//            default: return "Unknown";
//        }
//    }

//    //Links to the AR Tutorial
//    public void OnKeyboardTutorialButtonClicked() => SceneManager.LoadScene("AR_Keyboard_Tutorial");
//    public void OnMonitorTutorialButtonClicked() => SceneManager.LoadScene("AR_Monitor_Tutorial");
//    public void OnMouseTutorialButtonClicked() => SceneManager.LoadScene("AR_Mouse_Tutorial");
//    public void OnSpeakerTutorialButtonClicked() => SceneManager.LoadScene("AR_Speaker_Tutorial");
//    public void OnLaptopTutorialButtonClicked() => SceneManager.LoadScene("AR_Laptop_Tutorial");

//    private List<YoloBox> ParseKeyboardDetections(Tensor output)
//    {
//        List<YoloBox> boxes = new List<YoloBox>();
//        int numBoxes = output.shape.channels;

//        for (int i = 0; i < numBoxes; i++)
//        {
//            float objConf = Sigmoid(output[0, 0, 4, i]);
//            float classConf = Sigmoid(output[0, 0, 5, i]);
//            float finalConf = objConf * classConf;

//            if (finalConf > confidenceThreshold)
//            {
//                boxes.Add(new YoloBox { confidence = finalConf, classId = 1 });
//            }
//        }
//        return boxes;
//    }

//    private List<YoloBox> ParseMultiDetections(Tensor output)
//    {
//        List<YoloBox> boxes = new List<YoloBox>();
//        int numBoxes = output.shape.channels;
//        int numAttributes = output.shape.width; // x,y,w,h,obj + classes

//        for (int i = 0; i < numBoxes; i++)
//        {
//            float objConf = Sigmoid(output[0, 0, 4, i]);

//            float maxClassConf = 0f;
//            int maxClassIdx = -1;

//            for (int c = 5; c < numAttributes; c++)
//            {
//                float classConf = Sigmoid(output[0, 0, c, i]);
//                if (classConf > maxClassConf)
//                {
//                    maxClassConf = classConf;
//                    maxClassIdx = c - 5;
//                }
//            }

//            float finalConf = objConf * maxClassConf;
//            if (finalConf > confidenceThreshold)
//            {
//                boxes.Add(new YoloBox
//                {
//                    confidence = finalConf,
//                    classId = maxClassIdx
//                });
//            }
//        }

//        return boxes;
//    }

//    private Texture2D ResizeTexture(Texture2D tex, int width, int height)
//    {
//        RenderTexture rt = RenderTexture.GetTemporary(width, height);
//        Graphics.Blit(tex, rt);
//        RenderTexture.active = rt;
//        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
//        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//        result.Apply();
//        RenderTexture.active = null;
//        RenderTexture.ReleaseTemporary(rt);
//        return result;
//    }

//    private float Sigmoid(float x)
//    {
//        return 1f / (1f + Mathf.Exp(-x));
//    }

//    [System.Serializable]
//    public class YoloBox
//    {
//        public float confidence;
//        public int classId;
//    }

//    void OnDestroy()
//    {
//        keyboardWorker?.Dispose();
//        multiWorker?.Dispose();
//    }
//}

using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class YoloKeyboardDetector : MonoBehaviour
{
    [Header("YOLO Models")]
    public NNModel keyboardModel;         // Model 1: Keyboard-only
    public NNModel multiClassModel;       // Model 2: All 5 classes
    public float confidenceThreshold = 0.4f;

    [Header("Component Panels")]
    public GameObject keyboardPanel;
    public GameObject monitorPanel;
    public GameObject mousePanel;
    public GameObject speakerPanel;
    public GameObject laptopPanel;

    [Header("UI")]
    public RawImage cameraDisplay;

    [Header("Detection Settings")]
    public float detectionInterval = 0.5f;

    private IWorker keyboardWorker;
    private IWorker multiWorker;
    private WebCamTexture webcam;
    private float detectionTimer = 0f;
    private const int INPUT_SIZE = 416;
    private Dictionary<int, GameObject> panelByClass;

    void Start()
    {
        // Load models
        keyboardWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, ModelLoader.Load(keyboardModel));
        multiWorker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, ModelLoader.Load(multiClassModel));
        Debug.Log("✅ Both models loaded.");

        // Start webcam
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("❌ No webcam found.");
            return;
        }

        webcam = new WebCamTexture(devices[0].name, 1280, 720);
        webcam.Play();
        cameraDisplay.texture = webcam;

        foreach (var panel in panelByClass.Values)
            panel.SetActive(false);
    }

    void Awake()
    {
        panelByClass = new Dictionary<int, GameObject>
    {
        { 0, mousePanel },
        { 1, keyboardPanel },
        { 2, monitorPanel },
        { 3, speakerPanel },
        { 4, laptopPanel }
    };
    }
    void Update()
    {
        if (webcam == null || !webcam.didUpdateThisFrame) return;

        detectionTimer += Time.deltaTime;
        if (detectionTimer < detectionInterval) return;
        detectionTimer = 0f;

        // 📸 Capture full frame from webcam
        Texture2D fullFrame = new Texture2D(webcam.width, webcam.height);
        fullFrame.SetPixels(webcam.GetPixels());
        fullFrame.Apply();

        // ✂️ Crop a 416x416 region from the center
        int cropSize = INPUT_SIZE;
        int startX = (webcam.width - cropSize) / 2;
        int startY = (webcam.height - cropSize) / 2;

        Color[] croppedPixels = fullFrame.GetPixels(startX, startY, cropSize, cropSize);
        Texture2D croppedFrame = new Texture2D(cropSize, cropSize);
        croppedFrame.SetPixels(croppedPixels);
        croppedFrame.Apply();

        // ✅ Use Barracuda's built-in tensor creation (handles normalization)
        Tensor input = new Tensor(croppedFrame, 3);

        // 🔁 Run both YOLO models
        keyboardWorker.Execute(new Dictionary<string, Tensor> { { "images", input } });
        Tensor kbOutput = keyboardWorker.PeekOutput("output0");
        List<YoloBox> kbDetections = ApplyNMS(ParseKeyboardDetections(kbOutput));

        multiWorker.Execute(new Dictionary<string, Tensor> { { "images", input } });
        Tensor multiOutput = multiWorker.PeekOutput("output0");
        List<YoloBox> multiDetections = ApplyNMS(ParseMultiDetections(multiOutput));


        // 🧼 Step 1: Hide all panels
        foreach (var panel in panelByClass.Values)
            panel.SetActive(false);

        // 🧠 Step 2: Pick top detection
        YoloBox topDetection = null;
        if (kbDetections.Count > 0)
        {
            topDetection = new YoloBox { confidence = kbDetections[0].confidence, classId = 1 };
            Debug.Log("✅ Keyboard detected from Model 1");
        }

        foreach (var box in multiDetections)
        {
            if (topDetection == null || box.confidence > topDetection.confidence)
            {
                topDetection = box;
            }
        }

        // ⭐ Step 3: Show best panel
        if (topDetection != null && topDetection.confidence >= confidenceThreshold && panelByClass.ContainsKey(topDetection.classId))
        {
            panelByClass[topDetection.classId].SetActive(true);
            string name = GetClassName(topDetection.classId);
            Debug.Log($"⭐ Showing panel: {name} (classId: {topDetection.classId}) with confidence: {topDetection.confidence:F2}");
            Debug.Log($"   🔸 Confidence: {topDetection.confidence:F2}");
        }
        else
        {
            Debug.Log("❌ No confident detection found.");
        }

        // 🧹 Cleanup
        input.Dispose();
        kbOutput.Dispose();
        multiOutput.Dispose();
        Destroy(fullFrame);
        Destroy(croppedFrame);
    }


    private string GetClassName(int classId)
    {
        switch (classId)
        {
            case 0: return "Mouse";
            case 1: return "Keyboard";
            case 2: return "Monitor";
            case 3: return "Speaker";
            case 4: return "Laptop";
            default: return "Unknown";
        }
    }

    //Links to the AR Tutorial
    public void OnKeyboardTutorialButtonClicked() => SceneManager.LoadScene("AR_Keyboard_Tutorial");
    public void OnMonitorTutorialButtonClicked() => SceneManager.LoadScene("AR_Monitor_Tutorial");
    public void OnMouseTutorialButtonClicked() => SceneManager.LoadScene("AR_Mouse_Tutorial");
    public void OnSpeakerTutorialButtonClicked() => SceneManager.LoadScene("AR_Speaker_Tutorial");
    public void OnLaptopTutorialButtonClicked() => SceneManager.LoadScene("AR_Laptop_Tutorial");

    private List<YoloBox> ParseKeyboardDetections(Tensor output)
    {
        List<YoloBox> boxes = new List<YoloBox>();
        int numBoxes = output.shape.channels;

        for (int i = 0; i < numBoxes; i++)
        {
            float objConf = Sigmoid(output[0, 0, 4, i]);
            float classConf = Sigmoid(output[0, 0, 5, i]);
            float finalConf = objConf * 0.3f * classConf;

            if (finalConf > confidenceThreshold)
            {
                boxes.Add(new YoloBox { confidence = finalConf, classId = 1,
                });
            }
        }
        return boxes;
    }

    //private List<YoloBox> ParseMultiDetections(Tensor output)
    //{
    //    List<YoloBox> boxes = new List<YoloBox>();
    //    int numBoxes = output.shape.channels;
    //    int numAttributes = output.shape.width; // x,y,w,h,obj + classes

    //    for (int i = 0; i < numBoxes; i++)
    //    {
    //        float objConf = Sigmoid(output[0, 0, 4, i]);

    //        float maxClassConf = 0f;
    //        int maxClassIdx = -1;

    //        for (int c = 5; c < numAttributes; c++)
    //        {
    //            float classConf = Sigmoid(output[0, 0, c, i]);
    //            if (classConf > maxClassConf)
    //            {
    //                maxClassConf = classConf;
    //                maxClassIdx = c - 5;
    //            }
    //        }

    //        float finalConf = objConf * maxClassConf;
    //        if (finalConf > confidenceThreshold)
    //        {
    //            boxes.Add(new YoloBox
    //            {
    //                confidence = finalConf,
    //                classId = maxClassIdx
    //            });
    //        }
    //    }

    //    return boxes;
    //}

    private List<YoloBox> ParseMultiDetections(Tensor output)
    {
        List<YoloBox> boxes = new List<YoloBox>();

        int numBoxes = output.shape.channels;  // should be 8400
        int numAttributes = output.shape.width; // should be 85

        for (int i = 0; i < numBoxes; i++)
        {
            float x = Sigmoid(output[0, 0, 0, i]); // center x
            float y = Sigmoid(output[0, 0, 1, i]); // center y
            float w = Sigmoid(output[0, 0, 2, i]); // width
            float h = Sigmoid(output[0, 0, 3, i]); // height

            float objConf = Sigmoid(output[0, 0, 4, i]);

            float maxClassConf = 0f;
            int maxClassIdx = -1;

            for (int c = 5; c < numAttributes; c++)
            {
                float classConf = Sigmoid(output[0, 0, c, i]);
                if (classConf > maxClassConf)
                {
                    maxClassConf = classConf;
                    maxClassIdx = c - 5;
                }
            }

            float finalConf = objConf * maxClassConf;

            if (finalConf > confidenceThreshold)
            {
                boxes.Add(new YoloBox
                {
                    confidence = finalConf,
                    classId = maxClassIdx,
                    x = x,
                    y = y,
                    width = w,
                    height = h,
                });
            }
        }

        return boxes;
    }


    private Texture2D ResizeTexture(Texture2D tex, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(tex, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    private float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    [System.Serializable]
    public class YoloBox
    {
        public float confidence;
        public int classId;
        public float x, y, width, height; // normalized (0–1)
    }

    public static List<YoloBox> ApplyNMS(List<YoloBox> boxes, float iouThreshold = 0.45f)
    {
        List<YoloBox> result = new List<YoloBox>();
        boxes.Sort((a, b) => b.confidence.CompareTo(a.confidence)); // high to low

        while (boxes.Count > 0)
        {
            YoloBox best = boxes[0];
            result.Add(best);
            boxes.RemoveAt(0);

            boxes.RemoveAll(box => IoU(best, box) > iouThreshold);
        }

        return result;
    }

    private static float IoU(YoloBox a, YoloBox b)
    {
        // Simplified — assuming you decode actual boxes
        return 0f; // Replace this with real IoU if you decode box coordinates
    }

    void OnDestroy()
    {
        keyboardWorker?.Dispose();
        multiWorker?.Dispose();
    }
}