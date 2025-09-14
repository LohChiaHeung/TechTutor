using UnityEngine;
using System.IO;
public static class ARCache
{
    static string Root => Application.persistentDataPath;
    public static string GuidePath => Path.Combine(Root, "ar_guide.json");
    public static string ImagePath => Path.Combine(Root, "ar_image.jpg");

    public static void SaveGuide(AIGuide guide)
    {
        if (guide == null) return;
        var json = JsonUtility.ToJson(guide, prettyPrint: false);
        File.WriteAllText(GuidePath, json);
        Debug.Log("[ARCache] Saved guide: " + GuidePath);
    }

    public static bool LoadGuide(out AIGuide guide)
    {
        guide = null;
        if (!File.Exists(GuidePath)) return false;
        var json = File.ReadAllText(GuidePath);
        guide = JsonUtility.FromJson<AIGuide>(json);
        return guide != null && guide.steps != null && guide.steps.Length > 0;
    }

    public static string SavePngOrJpg(Texture2D tex)
    {
        if (tex == null) return null;
        // use JPG to keep size small
        var bytes = tex.EncodeToJPG(80);
        File.WriteAllBytes(ImagePath, bytes);
        Debug.Log("[ARCache] Saved image: " + ImagePath);
        return ImagePath;
    }

    public static bool LoadImage(out Texture2D tex)
    {
        tex = null;
        if (!File.Exists(ImagePath)) return false;
        var bytes = File.ReadAllBytes(ImagePath);
        tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(bytes);
        return true;
    }
}
