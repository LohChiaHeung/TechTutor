using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ChatHistoryPayload
{
    public int version = 1;
    public List<ChatQAPair> items = new();
}

public class ChatHistoryStore : MonoBehaviour
{
    public static ChatHistoryStore Instance { get; private set; }

    [Header("Config")]
    public int maxItems = 500;
    public string fileName = "chat_history_ai.json";

    [Header("Default Image")]
    [Tooltip("Optional: assign a default Texture2D in Inspector. If null, a gray 64x64 is generated.")]
    public Texture2D defaultThumbnail;

    public ChatHistoryPayload Data { get; private set; } = new ChatHistoryPayload();

    public event Action OnChanged;

    string FilePath => Path.Combine(Application.persistentDataPath, fileName);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (defaultThumbnail == null) defaultThumbnail = MakeFallback();
        LoadFromDisk();
    }

    public void Add(string question, string answer, string imagePath = null)
    {
        Data.items.Add(new ChatQAPair(question, answer, imagePath));
        TrimIfNeeded();
        SaveToDisk();
        OnChanged?.Invoke();
    }

    public void Delete(string id)
    {
        Data.items.RemoveAll(x => x.id == id);
        SaveToDisk();
        OnChanged?.Invoke();
    }

    public void ClearAll()
    {
        Data = new ChatHistoryPayload();
        SaveToDisk();
        OnChanged?.Invoke();
    }

    public void LoadFromDisk()
    {
        try
        {
            if (!File.Exists(FilePath)) { SaveToDisk(); return; }
            var json = File.ReadAllText(FilePath, Encoding.UTF8);
            var obj = JsonUtility.FromJson<ChatHistoryPayload>(json);
            Data = obj ?? new ChatHistoryPayload();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ChatHistoryStore] Load failed: {e.Message}");
            Data = new ChatHistoryPayload();
        }
        OnChanged?.Invoke();
    }

    public void SaveToDisk()
    {
        try
        {
            var json = JsonUtility.ToJson(Data, prettyPrint: true);
            File.WriteAllText(FilePath, json, Encoding.UTF8);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ChatHistoryStore] Save failed: {e.Message}");
        }
    }

    public Texture2D LoadImageOrDefault(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return defaultThumbnail;
        try
        {
            if (!File.Exists(path)) return defaultThumbnail;
            byte[] bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes)) return tex;
        }
        catch { /* ignore */ }
        return defaultThumbnail;
    }

    Texture2D MakeFallback()
    {
        var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color[64 * 64];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color(0.85f, 0.85f, 0.85f, 1f);
        tex.SetPixels(pixels);
        tex.Apply(false, true);
        return tex;
    }

    void TrimIfNeeded()
    {
        int over = Data.items.Count - Mathf.Max(50, maxItems);
        if (over > 0) Data.items.RemoveRange(0, over);
    }
}
