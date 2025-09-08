using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class OcrClient_New
{
    /// <summary>
    /// Posts PNG bytes to your OCR endpoint. The endpoint should return JSON matching OcrResponse_New.
    /// Content-Type: application/octet-stream (or change per your server).
    /// </summary>
    public static IEnumerator PostPng(string url, byte[] pngBytes, System.Action<string> onDone, System.Action<string> onError)
    {
        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(pngBytes);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
                onError?.Invoke($"[OCR] HTTP {req.responseCode} {req.error}");
            else
                onDone?.Invoke(req.downloadHandler.text);
        }
    }
}
