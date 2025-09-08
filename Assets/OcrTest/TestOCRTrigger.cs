using System.Collections;
using UnityEngine;

public class TestOCRTrigger : MonoBehaviour
{
    public OCRRequests ocrRequest;

    void Start()
    {
        // Automatically run on scene start
        StartCoroutine(CaptureAndRunOCR());
    }

    IEnumerator CaptureAndRunOCR()
    {
        yield return new WaitForEndOfFrame(); // Wait for scene to finish rendering

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log("[OCR] Screenshot captured");

        ocrRequest.StartOCR(screenshot);
    }
}
