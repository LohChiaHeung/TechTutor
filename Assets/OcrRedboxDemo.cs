using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

public class OcrRedboxDemo : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public OcrRedboxOverlayController overlay;

    // Your server (matches ocr_server.py)
    //public string serverUrl = "http://127.0.0.1:5000/ocr";
    public string serverUrl = " http://172.18.25.5:5000/ocr"; 

    // Test image (put file in Assets/StreamingAssets/)
    public string testImageFile = "test_screenshot.jpg";

    [ContextMenu("Run OCR")]
    public void RunOcr() { StartCoroutine(Co_Run()); }

    IEnumerator Co_Run()
    {
        // 1) Load image bytes
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, testImageFile);
        byte[] imgBytes;
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var www = UnityWebRequest.Get(path))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) {
                Debug.LogError("[Redbox] Load fail: " + www.error); yield break;
            }
            imgBytes = www.downloadHandler.data;
        }
#else
        try { imgBytes = System.IO.File.ReadAllBytes(path); }
        catch (Exception e) { Debug.LogError("[Redbox] Read fail: " + e.Message); yield break; }
#endif
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(imgBytes);
        overlay.SetTexture(tex);

        // 2) POST to server
        string b64 = Convert.ToBase64String(imgBytes);
        var payload = "{\"image_base64\":\"" + b64 + "\"}";
        var req = new UnityWebRequest(serverUrl, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[Redbox] Server error: " + req.error); yield break;
        }

        // 3) Parse and draw
        var resp = JsonUtility.FromJson<OcrRedboxResponse>(req.downloadHandler.text);
        if (resp == null || resp.words == null) { Debug.LogError("[Redbox] No boxes parsed"); yield break; }
        overlay.DrawWords(resp.words, resp.width, resp.height);
    }
}
