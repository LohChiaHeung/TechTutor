using UnityEngine;
using UnityEngine.UI;

public class OCRDemoEditor : MonoBehaviour
{
    public RawImage sourceImage;   // assign your test image
    public Text resultText;        // output goes here

    public void RunOCR()
    {
        var tex = sourceImage?.texture as Texture2D;
        if (tex == null)
        {
            Debug.LogError("No Texture2D on RawImage.");
            return;
        }

        Texture2D readable = MakeReadable(tex);
        string text = TesseractEditorRunner.Recognize(readable, "eng", "--psm 6");
        resultText.text = string.IsNullOrEmpty(text) ? "(no text found)" : text;
        Debug.Log("[OCR] " + text);
    }

    private Texture2D MakeReadable(Texture2D src)
    {
        RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0);
        Graphics.Blit(src, rt);
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);
        return tex;
    }
}
