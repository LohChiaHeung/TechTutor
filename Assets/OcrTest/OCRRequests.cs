using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using System;

public class OCRRequests : MonoBehaviour
{
    public OCROverlayMapper overlayMapper;
    public RawImage screenshotImage;

    [Serializable]
    public class WordBox
    {
        public string text;
        public float x, y, w, h, conf;
    }

    [Serializable]
    public class OCRResponse
    {
        public int width;
        public int height;
        public List<WordBox> words;
    }

    [Serializable]
    private class ImagePayload
    {
        public string image_base64;
    }

    public void StartOCR(Texture2D tex)
    {
        Debug.Log("[OCR] Starting OCR...");
        screenshotImage.texture = tex;

        if (screenshotImage.TryGetComponent(out AspectRatioFitter fitter))
        {
            fitter.aspectRatio = (float)tex.width / tex.height;
        }

        string base64 = Convert.ToBase64String(tex.EncodeToPNG());
        StartCoroutine(SendOCRRequest(base64));
    }

    IEnumerator SendOCRRequest(string base64)
    {
        string url = "http://127.0.0.1:5000/ocr"; // 🔁 CHANGE to your PC/server IP
        Debug.Log("[OCR] Sending request to: " + url);

        ImagePayload payload = new ImagePayload { image_base64 = base64 };
        string json = JsonUtility.ToJson(payload);

        UnityWebRequest req = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[OCR] Request failed: " + req.error);
            yield break;
        }

        OCRResponse ocrResult = JsonUtility.FromJson<OCRResponse>(req.downloadHandler.text);
        List<OCROverlayMapper.OcrBox> boxes = new List<OCROverlayMapper.OcrBox>();
        foreach (var word in ocrResult.words)
        {
            boxes.Add(new OCROverlayMapper.OcrBox
            {
                x = word.x,
                y = word.y,
                w = word.w,
                h = word.h,
                text = word.text
            });
        }

        overlayMapper.RenderBoxes(boxes);
    }
}
