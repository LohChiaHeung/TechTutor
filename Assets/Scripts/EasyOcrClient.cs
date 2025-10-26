//using System.Collections;
//using System.Text;
//using UnityEngine;
//using UnityEngine.Networking;

//[System.Serializable]
//public class OcrItem
//{
//    public string text;
//    public float x, y, w, h, conf;
//}
//[System.Serializable]
//public class OcrResponse
//{
//    public int width;
//    public int height;
//    public OcrItem[] words;
//}

//public class EasyOcrClient : MonoBehaviour
//{
//    [Header("Replace with your PC LAN IP")]
//    [SerializeField] string serverUrl = "http://192.168.xx.xx:5000/ocr"; // e.g. 172.18.20.148

//    public IEnumerator Run(Texture2D image, System.Action<OcrResponse> onDone, System.Action<string> onError = null)
//    {
//        if (image == null) { onError?.Invoke("Texture2D is null"); yield break; }

//        // ✅ Always produce PNG bytes from any texture
//        var pngBytes = TextureUtils.ToPngBytes(image);
//        if (pngBytes == null || pngBytes.Length == 0)
//        {
//            onError?.Invoke("Failed to convert texture to PNG (null/empty).");
//            yield break;
//        }

//        var b64 = System.Convert.ToBase64String(pngBytes);
//        var json = "{\"image_base64\":\"data:image/png;base64," + b64 + "\"}";

//        using var req = new UnityWebRequest(serverUrl, "POST");
//        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
//        req.uploadHandler = new UploadHandlerRaw(bodyRaw);
//        req.downloadHandler = new DownloadHandlerBuffer();
//        req.SetRequestHeader("Content-Type", "application/json");

//        yield return req.SendWebRequest();

//        if (req.result != UnityWebRequest.Result.Success)
//            onError?.Invoke($"OCR request failed: {req.error}");
//        else
//            onDone?.Invoke(JsonUtility.FromJson<OcrResponse>(req.downloadHandler.text));
//    }

//}

// Assets/Resources/Scripts/EasyOcrClient.cs
// Assets/Scripts/EasyOcrClient.cs
using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#region OCR Models (same shape you used before)
[Serializable]
public class OcrItem
{
    public string text;
    public float x, y, w, h, conf;
}

[Serializable]
public class OcrResponse
{
    public int width;
    public int height;
    public OcrItem[] words;
}
#endregion

public class EasyOcrClient : MonoBehaviour
{
    [Header("Server (set in Inspector)")]
    [Tooltip("Used in the Unity Editor, e.g. http://localhost:5000")]
    public string serverBase_Editor = "http://localhost:5000";

    [Tooltip("Used on device builds (Android/iOS), e.g. http://192.168.1.23:5000")]
    public string serverBase_Device = "http://192.168.1.23:5000";

    [Tooltip("If Development Build on Android, use adb reverse tcp:5000 -> 127.0.0.1:5000")]
    public bool useAdbReverseInDevBuild = true;

    [Header("Endpoint")]
    [Tooltip("Relative path to OCR endpoint. Use '/ocr' if your Flask route is @app.post('/ocr'). Use '/' or blank for root.")]
    public string ocrEndpoint = "/ocr";

    [Header("Payload")]
    [Tooltip("Send Base64 with 'data:image/png;base64,' prefix (most Flask examples expect this).")]
    public bool sendAsDataUrl = true;

    [Header("Encoding (optional)")]
    [Tooltip("Downscale very large images before upload (saves bandwidth).")]
    public bool downscaleBeforeSend = false;
    [Tooltip("Max width/height if downscaling is enabled.")]
    public int maxDimension = 1280;

    [Header("Networking")]
    [Range(1, 60)] public int timeoutSeconds = 15;
    public bool verboseLogs = true;

    /// <summary>
    /// Sends the given Texture2D to the OCR server and parses OcrResponse.
    /// </summary>
    public IEnumerator Run(Texture2D image, Action<OcrResponse> onSuccess, Action<string> onError = null)
    {
        if (image == null)
        {
            onError?.Invoke("OCR: Texture2D is null");
            yield break;
        }

        // 1) Make a readable, uncompressed copy (fixes 'Unsupported texture format' on phone)
        Texture2D working = null;
        try
        {
            working = MakeReadableRGBA32(image);
        }
        catch (Exception ex)
        {
            onError?.Invoke("OCR: Could not make readable copy: " + ex.Message);
            yield break;
        }

        // 2) (Optional) Downscale to reduce upload size
        if (downscaleBeforeSend && Mathf.Max(working.width, working.height) > maxDimension)
        {
            try
            {
                var scaled = ScaleToMax(working, maxDimension);
                if (scaled != working) { Destroy(working); working = scaled; }
            }
            catch (Exception ex)
            {
                if (verboseLogs) Debug.LogWarning("[OCR] Downscale failed, continuing with original readable copy. " + ex.Message);
            }
        }

        // 3) Encode to PNG
        byte[] pngBytes = null;
        try
        {
            pngBytes = working.EncodeToPNG();
        }
        catch (Exception ex)
        {
            onError?.Invoke("OCR: PNG encode failed: " + ex.Message);
            if (working && working != image) Destroy(working);
            yield break;
        }
        finally
        {
            if (working && working != image) Destroy(working); // avoid leaks
        }

        if (pngBytes == null || pngBytes.Length == 0)
        {
            onError?.Invoke("OCR: Failed to convert texture to PNG (null/empty).");
            yield break;
        }

        // 4) Build URL
        string baseUrl = ResolveBase();
        string ep = string.IsNullOrEmpty(ocrEndpoint) ? "/" : (ocrEndpoint.StartsWith("/") ? ocrEndpoint : "/" + ocrEndpoint);
        string url = baseUrl.TrimEnd('/') + ep;

        // 5) Build JSON body: {"image_base64":"data:image/png;base64,...."}
        string b64 = Convert.ToBase64String(pngBytes);
        string data = sendAsDataUrl ? ("data:image/png;base64," + b64) : b64;
        var post = new OcrPost { image_base64 = data };
        string json = JsonUtility.ToJson(post);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        if (verboseLogs)
            Debug.Log($"[OCR] POST {url}  img={image.width}x{image.height}  payload={bodyRaw.Length} bytes");

        // 6) Send request
        using (var req = new UnityWebRequest(url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success;
#else
            bool ok = !req.isNetworkError && !req.isHttpError;
#endif
            if (!ok)
            {
                string err = $"HTTP {req.responseCode} {req.error}\n{req.downloadHandler?.text}";
                if (verboseLogs) Debug.LogError("[OCR] " + err);
                onError?.Invoke("OCR request failed: " + err);
                yield break;
            }

            string text = req.downloadHandler.text ?? "";
            if (verboseLogs)
            {
                int n = Mathf.Min(200, text.Length);
                Debug.Log($"[OCR] OK {req.responseCode}  preview[0..{n}]={text.Substring(0, n)}");
            }

            OcrResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<OcrResponse>(text);
            }
            catch (Exception ex)
            {
                onError?.Invoke("OCR: JSON parse error: " + ex.Message);
                yield break;
            }

            if (resp == null)
            {
                onError?.Invoke("OCR: empty/invalid response");
                yield break;
            }

            onSuccess?.Invoke(resp);
        }
    }

    // Choose the correct base URL per platform/build
    string ResolveBase()
    {
#if UNITY_ANDROID
        if (Debug.isDebugBuild && useAdbReverseInDevBuild)
            return "http://127.0.0.1:5000"; // Run: adb reverse tcp:5000 tcp:5000
        return string.IsNullOrWhiteSpace(serverBase_Device) ? serverBase_Editor : serverBase_Device;
#elif UNITY_IOS
        return string.IsNullOrWhiteSpace(serverBase_Device) ? serverBase_Editor : serverBase_Device;
#else
        return string.IsNullOrWhiteSpace(serverBase_Editor) ? "http://localhost:5000" : serverBase_Editor;
#endif
    }

    // Make a readable, uncompressed RGBA32 copy (works for compressed / non-readable textures)
    Texture2D MakeReadableRGBA32(Texture2D src)
    {
        int w = src.width, h = src.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        Graphics.Blit(src, rt);

        var prev = RenderTexture.active;
        RenderTexture.active = rt;

        var dst = new Texture2D(w, h, TextureFormat.RGBA32, false);
        dst.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        dst.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }

    // Optional: downscale to a maximum dimension (maintains aspect)
    Texture2D ScaleToMax(Texture2D srcReadableRGBA32, int maxDim)
    {
        int w = srcReadableRGBA32.width;
        int h = srcReadableRGBA32.height;
        float scale = Mathf.Min(1f, (float)maxDim / Mathf.Max(w, h));
        if (scale >= 0.999f) return srcReadableRGBA32;

        int nw = Mathf.Max(1, Mathf.RoundToInt(w * scale));
        int nh = Mathf.Max(1, Mathf.RoundToInt(h * scale));

        var rt = RenderTexture.GetTemporary(nw, nh, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        rt.filterMode = FilterMode.Bilinear;

        var prev = RenderTexture.active;
        Graphics.Blit(srcReadableRGBA32, rt);
        RenderTexture.active = rt;

        var dst = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
        dst.ReadPixels(new Rect(0, 0, nw, nh), 0, 0);
        dst.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return dst;
    }

    [Serializable]
    class OcrPost { public string image_base64; }
}
