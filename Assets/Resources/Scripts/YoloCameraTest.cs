using UnityEngine;
using Unity.Barracuda;
using System.Collections.Generic;
using UnityEngine.UI;

public class YoloCameraTest : MonoBehaviour
{
    public NNModel yoloModelAsset;
    public RawImage imageDisplay;

    private Model runtimeModel;
    private IWorker worker;

    private WebCamTexture webcam;
    private Texture2D tex;

    private YoloOverlay overlay;

    private bool snapshotTaken = false;
    private Texture2D snapshotTexture;
    private List<YoloBox> snapshotDetections;

    void Start()
    {
        // Load YOLO model
        runtimeModel = ModelLoader.Load(yoloModelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.Auto, runtimeModel);

        // Start camera at 640x640 square
        webcam = new WebCamTexture(640, 640);
        webcam.Play();

        Debug.Log($"Requested webcam size: 640x640");
        Debug.Log($"Actual webcam size: {webcam.width} x {webcam.height}");

        imageDisplay.texture = webcam;
        overlay = FindObjectOfType<YoloOverlay>();

        tex = new Texture2D(webcam.width, webcam.height, TextureFormat.RGB24, false);
    }

    int frameSkip = 5;
    private int frameCounter = 0;

    void Update()
    {
        if (webcam.didUpdateThisFrame)
        {
            frameCounter++;
            if (frameCounter % frameSkip == 0)
            {
                ProcessFrame();
            }
        }
    }

    // Class names
    private readonly string[] classLabels = new string[]
    {
        "0", "1", "2", "3", "4", "5", "6", "7", "8", "9",
        "a", "accent", "ae", "alt-left", "altgr-right", "b", "c", "caret", "comma", "d",
        "del", "e", "enter", "f", "g", "h", "hash", "i", "j", "k", "keyboard", "l", "less",
        "m", "minus", "n", "o", "oe", "p", "plus", "point", "q", "r", "s", "shift-left",
        "shift-lock", "shift-right", "space", "ss", "strg-left", "strg-right", "t", "tab",
        "u", "ue", "v", "w", "x", "y", "z"
    };

    void ProcessFrame()
    {
        Debug.Log($"webcam.width={webcam.width}, webcam.height={webcam.height}, rotation={webcam.videoRotationAngle}, mirrored={webcam.videoVerticallyMirrored}");

        tex.SetPixels(webcam.GetPixels());
        tex.Apply();

        // Rotate and flip
        Texture2D corrected = RotateAndFlip(tex, webcam.videoRotationAngle, webcam.videoVerticallyMirrored);

        // Crop the center 480x480 region
        //Texture2D cropped = CropCenter(corrected, 480, 480);

        int cropX = (corrected.width - 480) / 2;
        int cropY = (corrected.height - 480) / 2;
        Texture2D cropped = Crop(corrected, cropX, cropY, 480, 480);

        // Show cropped texture in UI
        imageDisplay.texture = cropped;

        // Resize for YOLO input
        Texture2D resized = ResizeTexture(cropped, 640, 640);


        // Resize if needed (could skip if already 640)
        //Texture2D resized = ResizeTexture(squareTex, 640, 640);

        // Run YOLO
        Tensor inputTensor = new Tensor(resized, 3);


        worker.Execute(new Dictionary<string, Tensor>
        {
            { "images", inputTensor }
        });

        Tensor output = worker.PeekOutput("output0");

        List<YoloBox> boxes = ParseDetections(output, 640, 640);

        // Transform detections back to full camera frame (640x480)
        float scaleBack = 480f / 640f;
        float offsetX = cropX;
        float offsetY = cropY;

        foreach (var box in boxes)
        {
            box.x = box.x * scaleBack + offsetX;
            box.y = box.y * scaleBack + offsetY;
            box.w = box.w * scaleBack;
            box.h = box.h * scaleBack;
        }


        overlay.DrawDetections(boxes, 640, 480);


        inputTensor.Dispose();
        output.Dispose();
    }

    //public void OnCaptureButtonPressed()
    //{
    //    if (snapshotTaken) return; // only once
    //    snapshotTaken = true;

    //    // 1) Grab current cropped texture
    //    snapshotTexture = ResizeTexture(Crop(
    //        RotateAndFlip(tex, webcam.videoRotationAngle, webcam.videoVerticallyMirrored),
    //        (corrected.width - 480) / 2,
    //        (corrected.height - 480) / 2,
    //        480, 480
    //    ), 640, 640);

    //    // 2) Run inference once
    //    Tensor input = new Tensor(snapshotTexture, 3);
    //    worker.Execute(new Dictionary<string, Tensor> { { "images", input } });
    //    var output = worker.PeekOutput("output0");
    //    snapshotDetections = ParseDetections(output, 640, 640);
    //    input.Dispose();
    //    output.Dispose();

    //    // 3) Stop camera updates
    //    webcam.Pause();
    //    imageDisplay.texture = snapshotTexture;
    //}

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }


    List<YoloBox> ParseDetections(Tensor output, float imageWidth, float imageHeight)
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

            if (finalConf > 0.5f)
            {
                if (w > 1f && h > 1f && x > 1f && y > 1f)
                {
                    string label = (classId >= 0 && classId < classLabels.Length)
                        ? classLabels[classId]
                        : $"class_{classId}";

                    Debug.Log($"✅ VALID DETECTION: {label} ({finalConf:F2}) at x={x}, y={y}, w={w}, h={h}");

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
                else
                {
                    Debug.Log($"⚠️ Skipping junk detection at x={x}, y={y}, w={w}, h={h}, conf={finalConf}");
                }
            }
        }

        return boxes;
    }

    Texture2D RotateAndFlip(Texture2D tex, int angle, bool verticallyMirrored)
    {
        Texture2D rotated = RotateTexture(tex, angle);

        if (verticallyMirrored)
        {
            rotated = FlipTextureVertically(rotated);
        }

        return rotated;
    }

    Texture2D RotateTexture(Texture2D tex, int angle)
    {
        if (angle == 0) return tex;

        Color32[] pixels = tex.GetPixels32();
        Color32[] rotated = new Color32[pixels.Length];
        int w = tex.width;
        int h = tex.height;

        Texture2D result;

        if (angle == 90)
        {
            result = new Texture2D(h, w, tex.format, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    rotated[x * h + (h - y - 1)] = pixels[y * w + x];
        }
        else if (angle == 270)
        {
            result = new Texture2D(h, w, tex.format, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    rotated[(w - x - 1) * h + y] = pixels[y * w + x];
        }
        else if (angle == 180)
        {
            result = new Texture2D(w, h, tex.format, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    rotated[(h - y - 1) * w + (w - x - 1)] = pixels[y * w + x];
        }
        else
        {
            return tex;
        }

        result.SetPixels32(rotated);
        result.Apply();
        return result;
    }

    Texture2D FlipTextureVertically(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        Color32[] flipped = new Color32[pixels.Length];
        int w = tex.width;
        int h = tex.height;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                flipped[(h - y - 1) * w + x] = pixels[y * w + x];
            }
        }

        Texture2D result = new Texture2D(w, h, tex.format, false);
        result.SetPixels32(flipped);
        result.Apply();
        return result;
    }

    Texture2D LetterboxTexture(Texture2D source, int targetSize)
    {
        Texture2D target = new Texture2D(targetSize, targetSize, TextureFormat.RGB24, false);

        Color[] fill = new Color[targetSize * targetSize];
        for (int i = 0; i < fill.Length; i++)
            fill[i] = Color.black;
        target.SetPixels(fill);

        float scale = Mathf.Min(
            (float)targetSize / source.width,
            (float)targetSize / source.height);

        int newWidth = Mathf.RoundToInt(source.width * scale);
        int newHeight = Mathf.RoundToInt(source.height * scale);

        Texture2D resized = ResizeTexture(source, newWidth, newHeight);

        int xOffset = (targetSize - newWidth) / 2;
        int yOffset = (targetSize - newHeight) / 2;

        target.SetPixels(xOffset, yOffset, newWidth, newHeight, resized.GetPixels());
        target.Apply();
        return target;
    }

    Texture2D CropCenter(Texture2D tex, int targetWidth, int targetHeight)
    {
        int x = (tex.width - targetWidth) / 2;
        int y = (tex.height - targetHeight) / 2;

        Color[] pixels = tex.GetPixels(x, y, targetWidth, targetHeight);
        Texture2D cropped = new Texture2D(targetWidth, targetHeight, tex.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }

    Texture2D Crop(Texture2D tex, int x, int y, int width, int height)
    {
        Color[] pixels = tex.GetPixels(x, y, width, height);
        Texture2D cropped = new Texture2D(width, height, tex.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }


    Tensor TransformImageToTensor(Texture2D tex)
    {
        float[] floats = new float[tex.width * tex.height * 3];
        Color32[] pixels = tex.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            floats[i * 3 + 0] = pixels[i].r / 255f;
            floats[i * 3 + 1] = pixels[i].g / 255f;
            floats[i * 3 + 2] = pixels[i].b / 255f;
        }

        Tensor t = new Tensor(1, tex.height, tex.width, 3, floats);
        return t;
    }



}
