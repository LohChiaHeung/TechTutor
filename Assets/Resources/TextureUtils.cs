using UnityEngine;

public static class TextureUtils
{
    /// Returns PNG bytes for ANY Texture2D (compressed or not). Makes a temporary readable RGBA32 copy.
    public static byte[] ToPngBytes(Texture2D src)
    {
        if (src == null) return null;

        // 1) Create a temp RenderTexture and blit the source into it (works even if src is compressed / not readable)
        var rt = RenderTexture.GetTemporary(
            src.width, src.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear
        );
        Graphics.Blit(src, rt);

        // 2) Read back into a new readable RGBA32 texture
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var copy = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false, false);
        copy.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        copy.Apply();
        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        // 3) Encode to PNG (now supported)
        var png = copy.EncodeToPNG();
        Object.Destroy(copy); // cleanup

        return png;
    }
}
