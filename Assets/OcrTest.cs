using UnityEngine;

public class OcrTest : MonoBehaviour
{
    public EasyOcrClient ocrClient;
    public Texture2D testImage;        // assign a screenshot
    public RectTransform screenshotRT; // assign ScreenshotImage RectTransform
    public OcrOverlay overlay;         // assign OcrOverlay component

    void Start()
    {
        StartCoroutine(ocrClient.Run(
            testImage,
            (resp) => {
                Debug.Log($"OCR OK: {resp.words.Length} words");
                overlay.Render(resp); // draw boxes
            },
            (err) => Debug.LogError(err)
        ));
    }
}
