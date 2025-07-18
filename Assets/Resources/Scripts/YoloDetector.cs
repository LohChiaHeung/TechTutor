using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using System.Linq;

public class YoloDetector : MonoBehaviour
{
    public NNModel yoloModelAsset;
    private Model runtimeModel;
    private IWorker worker;

    private readonly string[] classLabels = new string[]
    {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "a", "accent", "ae", "alt-left", "altgr-right", "b", "c", "caret", "comma", "d",
        "del", "e", "enter", "f", "g", "h", "hash", "i", "j", "k",
        "keyboard", "l", "less", "m", "minus", "n", "o", "oe", "p", "plus",
        "point", "q", "r", "s", "shift-left", "shift-lock", "shift-right", "space", "ss", "strg-left",
        "strg-right", "t", "tab", "u", "ue", "v", "w", "x", "y", "z"
};

    public void Init()
    {
        if (worker == null)
        {
            runtimeModel = ModelLoader.Load(yoloModelAsset);
            worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);
            Debug.Log("✅ YoloDetector initialized.");
        }
    }


    public List<YoloBox> Detect(Texture2D inputTex)
    {
        Tensor inputTensor = FindObjectOfType<ARPhotoCapture>().TransformImageToTensor(inputTex);

        worker.Execute(new Dictionary<string, Tensor> {
        { "images", inputTensor }
    });

        Tensor output = worker.PeekOutput("output0");

        var boxes = ParseDetections(output);

        inputTensor.Dispose();
        output.Dispose();

        return boxes;
    }


    List<YoloBox> ParseDetections(Tensor output)
    {
        List<YoloBox> boxes = new List<YoloBox>();

        int numBoxes = output.shape.channels;
        int numValues = output.shape.width;

        float Sigmoid(float x) => 1f / (1f + Mathf.Exp(-x));

        for (int i = 0; i < numBoxes; i++)
        {
            float x = output[0, 0, 0, i];
            float y = output[0, 0, 1, i];
            float w = output[0, 0, 2, i];
            float h = output[0, 0, 3, i];
            float objConf = Sigmoid(output[0, 0, 4, i]);

            float maxClassConf = 0f;
            int classId = -1;

            for (int c = 5; c < numValues; c++)
            {
                float classScore = Sigmoid(output[0, 0, c, i]);
                if (classScore > maxClassConf)
                {
                    maxClassConf = classScore;
                    classId = c - 5;
                }
            }

            float finalConf = objConf * maxClassConf;

            //if (finalConf > 0.3f)
            //{
            //    string label = (classId >= 0 && classId < classLabels.Length) ? classLabels[classId] : $"class_{classId}";
            //    boxes.Add(new YoloBox
            //    {
            //        x = x,
            //        y = y,
            //        w = w,
            //        h = h,
            //        label = label,
            //        confidence = finalConf
            //    });
            //}

            if (finalConf > 0.3f)
            {
                string label = (classId >= 0 && classId < classLabels.Length)
                    ? classLabels[classId]
                    : $"class_{classId}";

                if (label == "keyboard")
                {
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

        }

        return boxes;
    }

    public List<YoloBox> ApplyNMS(List<YoloBox> detections, float nmsThreshold = 0.5f)
    {
        var result = new List<YoloBox>();
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

    private float CalculateIoU(YoloBox a, YoloBox b)
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

    void OnDestroy()
    {
        worker?.Dispose();
    }
}
