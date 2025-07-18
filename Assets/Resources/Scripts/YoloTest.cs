using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using UnityEngine.UI;

public class YoloImageTest : MonoBehaviour
{
    public NNModel yoloModelAsset;       // Your trained YOLOv5 .nn model
    public Texture2D inputImage;         // Image to test

    public RawImage imageDisplay;
    private Model runtimeModel;
    private IWorker worker;

    // Optional: class names (index = class ID)
    private readonly string[] classLabels = new string[]
    {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
    "a", "accent", "ae", "alt-left", "altgr-right", "b", "c", "caret", "comma", "d",
    "del", "e", "enter", "f", "g", "h", "hash", "i", "j", "k", "keyboard", "l", "less",
    "m", "minus", "n", "o", "oe", "p", "plus", "point", "q", "r", "s", "shift-left",
    "shift-lock", "shift-right", "space", "ss", "strg-left", "strg-right", "t", "tab",
    "u", "ue", "v", "w", "x", "y", "z"
    };

    void Start()
    {
        // Load model
        runtimeModel = ModelLoader.Load(yoloModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        // Resize image to 640x640
        Texture2D resized = ResizeTexture(inputImage, 640, 640);
        // Set the image into the UI display
        imageDisplay.texture = resized; ;
        Debug.Log($"✅ Resized image to: {resized.width}x{resized.height}");

        // Create input tensor
        Tensor inputTensor = new Tensor(resized, 3);

        // Inference
        worker.Execute(new Dictionary<string, Tensor> {
        { "images", inputTensor }
    });

        Tensor output = worker.PeekOutput("output0");
        Debug.Log("✅ Inference completed. Output shape: " + string.Join("x", output.shape));

        int numBoxes = output.shape.channels; // 25200
        int numValues = output.shape.width;   // 65

        float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

        float min = float.MaxValue, max = float.MinValue;
        for (int i = 0; i < output.length; i++)
        {
            float val = output[i];
            if (val < min) min = val;
            if (val > max) max = val;
        }
        Debug.Log($"📊 Output value range: min={min}, max={max}");

        List<YoloBox> boxes = new List<YoloBox>();

        for (int i = 0; i < numBoxes; i++)
        {
            float x = output[0, 0, 0, i];
            float y = output[0, 0, 1, i];
            float w = output[0, 0, 2, i];
            float h = output[0, 0, 3, i];
            float objConf = Sigmoid(output[0, 0, 4, i]);

            float maxClassConf = 0f;
            int classId = -1;

            for (int c = 5; c < numValues; c++) // From class0 to class59
            {
                float classScore = Sigmoid(output[0, 0, c, i]);
                if (classScore > maxClassConf)
                {
                    maxClassConf = classScore;
                    classId = c - 5;
                }
            }

            float finalConf = objConf * maxClassConf;

            if (finalConf > 0.5f)
            {
                string label = (classId >= 0 && classId < classLabels.Length) ? classLabels[classId] : $"class_{classId}";
                Debug.Log($"📦 Box {i}: x={x}, y={y}, w={w}, h={h}, obj_conf={objConf:F2}, class={label}, class_conf={maxClassConf:F2}, final_conf={finalConf:F2}");

                boxes.Add(new YoloBox
                {
                    x = x,
                    y = y,
                    w = w,
                    h = h,
                    label = label,
                    confidence = finalConf
                });
            }
        }

        // ✅ Call overlay script here!
        var overlay = FindObjectOfType<YoloOverlay>();
        if (overlay != null)
        {
            overlay.DrawDetections(boxes, 640, 640);
        }
        else
        {
            Debug.LogWarning("⚠️ YoloOverlay script not found in scene!");
        }

        inputTensor.Dispose();
        output.Dispose();
        worker.Dispose();
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
