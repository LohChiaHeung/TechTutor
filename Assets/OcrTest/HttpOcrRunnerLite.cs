using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable] public class OcrLiteWord { public string text; public float x, y, w, h; public float conf; }
[Serializable] public class OcrLiteReply { public int width, height; public OcrLiteWord[] words; }

public class HttpOcrRunnerLite : MonoBehaviour
{
    [Header("Server")]
    [Tooltip("Example: http://192.168.1.23:5000")]
    public string serverBase = "http://127.0.0.1:5000";

    [Header("Image I/O")]
    [Tooltip("If left empty, script will try to use previewRawImage.texture")]
    public Texture sourceTexture;
    public RawImage previewRawImage;

    [Header("Drawing")]
    [Range(1, 12)] public int lineThickness = 2;

    [ContextMenu("Run OCR Now")]
    public void RunOcrNow() => StartCoroutine(Run());

    IEnumerator Run()
    {
        if (!previewRawImage)
        {
            Debug.LogError("[OCR] Please assign previewRawImage (RawImage).");
            yield break;
        }

        var texIn = sourceTexture ? sourceTexture : previewRawImage.texture;
        if (!texIn)
        {
            Debug.LogError("[OCR] No sourceTexture and previewRawImage.texture is null.");
            yield break;
        }

        // 1) Get a readable copy (handles non-readable or compressed textures safely)
        var readable = MakeReadableCopy(texIn);
        if (!readable)
        {
            Debug.LogError("[OCR] Failed to create a readable copy.");
            yield break;
        }

        // 2) Encode → base64
        byte[] png = readable.EncodeToPNG();
        string b64 = Convert.ToBase64String(png);

        // 3) POST to server
        string url = serverBase.TrimEnd('/') + "/ocr";
        string payload = "{\"image_base64\":\"" + b64 + "\"}";

        using var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(payload));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("[OCR] HTTP error: " + req.error);
            yield break;
        }

        var reply = JsonUtility.FromJson<OcrLiteReply>(req.downloadHandler.text);
        if (reply == null || reply.words == null)
        {
            Debug.LogWarning("[OCR] Empty or invalid JSON.");
            yield break;
        }
        Debug.Log($"[OCR] words={reply.words.Length}, image={reply.width}x{reply.height}");

        // 4) Make a drawable clone (don’t draw on the original import asset)
        Texture2D drawTex = new Texture2D(readable.width, readable.height, TextureFormat.RGBA32, false);
        drawTex.SetPixels(readable.GetPixels());

        // 5) Paint rectangles (convert OCR top-left Y to Texture2D bottom-left Y)
        foreach (var w in reply.words)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(w.x), 0, drawTex.width - 1);
            int yTop = Mathf.Clamp(Mathf.RoundToInt(w.y), 0, drawTex.height - 1);
            int rw = Mathf.Clamp(Mathf.RoundToInt(w.w), 0, drawTex.width - x);
            int rh = Mathf.Clamp(Mathf.RoundToInt(w.h), 0, drawTex.height - yTop);
            if (rw <= 0 || rh <= 0) continue;

            // EasyOCR y=top-left; Texture2D y=bottom-left
            int yBottom = drawTex.height - (yTop + rh);

            PixelRectOutline.DrawRectOutline(drawTex, x, yBottom, rw, rh, lineThickness);
        }
        drawTex.Apply(false);

        // 6) Show result
        previewRawImage.texture = drawTex;

        // Optional: native size so pixel-perfect
        // previewRawImage.SetNativeSize();

        // Log a few texts for sanity
        for (int i = 0; i < Mathf.Min(5, reply.words.Length); i++)
            Debug.Log($"[OCR] {i + 1}: \"{reply.words[i].text}\" ({Mathf.RoundToInt(reply.words[i].conf * 100)}%)");
    }

    // Creates a readable RGBA32 copy from any Texture (Texture2D or RenderTexture).
    Texture2D MakeReadableCopy(Texture src)
    {
        if (src == null) return null;

        // If we already have a readable Texture2D in a compatible format, try to clone quickly
        if (src is Texture2D t2d && t2d.isReadable && (t2d.format == TextureFormat.RGBA32 || t2d.format == TextureFormat.ARGB32 || t2d.format == TextureFormat.RGB24))
        {
            var copy = new Texture2D(t2d.width, t2d.height, TextureFormat.RGBA32, false);
            copy.SetPixels(t2d.GetPixels());
            copy.Apply(false);
            return copy;
        }

        // Safe path: render to RT and ReadPixels
        int w = src.width, h = src.height;
        var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var prev = RenderTexture.active;
        Graphics.Blit(src, rt);
        RenderTexture.active = rt;

        var readable = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
        readable.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        readable.Apply(false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return readable;
    }
}
