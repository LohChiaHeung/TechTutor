using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OcrPosTestController_New : MonoBehaviour
{
    [Header("UI & Prefabs")]
    public RawImage screenshotImage;           // assign ScreenshotImage
    public RectTransform boxesOverlay;         // assign BoxesOverlay
    public RectTransform boxPrefab;            // assign BoxPrefab_New
    public UiBoxDrawer_New drawer;             // assign

    [Header("Image Source")]
    public Texture2D inputTexture;             // assign your screenshot here
    public bool autoSetNativeSize = true;

    [Header("OCR Source")]
    public bool useLocalJson = true;           // toggle between local JSON or remote endpoint
    [TextArea(5, 15)] public string localOcrJson; // paste OcrResponse_New JSON here
    public string ocrEndpointUrl = "http://127.0.0.1:5000/ocr"; // your server (edit to your actual)
    public float requestDelay = 0.2f;          // small delay to ensure UI is laid out

    private void Start()
    {
        // show image
        if (inputTexture != null)
        {
            screenshotImage.texture = inputTexture;
            if (autoSetNativeSize) screenshotImage.SetNativeSize();
        }

        // ensure drawer has refs
        if (!drawer)
        {
            drawer = gameObject.AddComponent<UiBoxDrawer_New>();
            drawer.screenshotImage = screenshotImage;
            drawer.overlay = boxesOverlay;
            drawer.boxPrefab = boxPrefab;
        }

        StartCoroutine(RunOnce());
    }

    IEnumerator RunOnce()
    {
        // Wait a frame so Layout/Canvas sizes are correct
        yield return null;
        yield return new WaitForSeconds(requestDelay);

        drawer.ClearBoxes();

        if (useLocalJson)
        {
            TryParseAndDraw(localOcrJson);
        }
        else
        {
            // encode texture as PNG
            var tex = screenshotImage.texture as Texture2D;
            if (tex == null)
            {
                Debug.LogError("[OCR] No Texture2D assigned to ScreenshotImage.");
                yield break;
            }
            byte[] png = tex.EncodeToPNG();

            yield return StartCoroutine(OcrClient_New.PostPng(
                ocrEndpointUrl, png,
                onDone: (json) => TryParseAndDraw(json),
                onError: (err) => Debug.LogError(err)
            ));
        }
    }

    void TryParseAndDraw(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            Debug.LogError("[OCR] Empty JSON.");
            return;
        }

        OcrResponse_New data = null;
        try
        {
            data = JsonUtility.FromJson<OcrResponse_New>(json);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[OCR] JSON parse error: " + ex.Message + "\n" + json);
            return;
        }

        if (data == null || data.words == null)
        {
            Debug.LogError("[OCR] Invalid data.");
            return;
        }

        // Draw all words
        foreach (var w in data.words)
        {
            drawer.DrawWordBox(data.width, data.height, w.x, w.y, w.w, w.h);
        }

        Debug.Log($"[OCR] Drew {data.words.Count} boxes ({data.width}x{data.height}).");
    }
}
