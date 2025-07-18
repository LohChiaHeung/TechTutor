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

using UnityEngine;
using Unity.Barracuda;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class YoloKeyboardDetector : MonoBehaviour
{
    [Header("YOLO Model")]
    public NNModel onnxModel;
    public float confidenceThreshold = 0.5f;

    [Header("UI")]
    public RawImage cameraDisplay;
    public GameObject keyboardDetectedPanel;

    [Header("Detection Settings")]
    public float detectionInterval = 0.5f;

    private Model model;
    private IWorker worker;
    private WebCamTexture webcam;
    private float detectionTimer = 0f;
    private bool hasSwitchedScene = false;

    private const int INPUT_SIZE = 416;

    void Start()
    {
        // Load YOLO model
        model = ModelLoader.Load(onnxModel);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, model);

        // Start webcam
        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("❌ No webcam found.");
            return;
        }

        string camName = devices[0].name;
        webcam = new WebCamTexture(camName, 1280, 720);
        webcam.Play();

        // Display original camera feed (full res)
        cameraDisplay.texture = webcam;

        keyboardDetectedPanel.SetActive(false);
    }

    void Update()
    {
        if (webcam == null || !webcam.didUpdateThisFrame)
            return;

        detectionTimer += Time.deltaTime;
        if (detectionTimer < detectionInterval)
            return;

        detectionTimer = 0f;

        // Grab current camera frame
        Texture2D frame = new Texture2D(webcam.width, webcam.height);
        frame.SetPixels(webcam.GetPixels());
        frame.Apply();

        // Resize only for inference
        Texture2D resized = ResizeTexture(frame, INPUT_SIZE, INPUT_SIZE);
        Tensor inputTensor = new Tensor(resized, 3);

        worker.Execute(new Dictionary<string, Tensor>
        {
            { "images", inputTensor }
        });

        Tensor output = worker.PeekOutput("output0");

        var boxes = ParseDetections(output);

        bool detected = boxes.Count > 0;
        keyboardDetectedPanel.SetActive(detected);

        if (detected)
        {
            Debug.Log("✅ It is a keyboard!");
        }
        else
        {
            Debug.Log("❌ No keyboard detected.");
        }

        inputTensor.Dispose();
        output.Dispose();
        Destroy(frame);
        Destroy(resized);
    }

    public void OnKeyboardTutorialButtonClicked()
    {
        SceneManager.LoadScene("AR_Keyboard_Tutorial");
    }


    private List<YoloBox> ParseDetections(Tensor output)
    {
        List<YoloBox> boxes = new List<YoloBox>();

        int numBoxes = output.shape.channels;
        int numAttributes = output.shape.width;

        for (int i = 0; i < numBoxes; i++)
        {
            float x_raw = output[0, 0, 0, i];
            float y_raw = output[0, 0, 1, i];
            float w_raw = output[0, 0, 2, i];
            float h_raw = output[0, 0, 3, i];
            float objConf = Sigmoid(output[0, 0, 4, i]);

            float classConf = Sigmoid(output[0, 0, 5, i]);
            float finalConf = objConf * classConf;

            if (finalConf > confidenceThreshold)
            {
                boxes.Add(new YoloBox
                {
                    confidence = finalConf
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
    }
}