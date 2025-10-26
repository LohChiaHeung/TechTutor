using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class QuizSessionMeta
{
    public string sessionId;           // e.g. keyboard_2025-09-07T14-11-20
    public string modelId;             // keyboard/mouse/monitor/...
    public string modelName;           // display name
    public string createdAtIsoUtc;     // ISO-8601
    public QuizPayload payload;        // YOUR schema (QuizPayload/QuizItem)
}

public static class AIQuizLocalStore
{
    private static string Root => Path.Combine(Application.persistentDataPath, "TechTutorQuizzes");

    public static string MakeSessionId(string modelId)
    {
        string stamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH-mm-ss");
        return $"{modelId}_{stamp}";
    }

    public static void Save(QuizSessionMeta session)
    {
        try
        {
            if (session == null || session.payload == null) { Debug.LogError("[AIQuizLocalStore] Null session/payload."); return; }
            string folder = Path.Combine(Root, session.modelId);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, session.sessionId + ".json");
            string json = JsonUtility.ToJson(session, true);
            File.WriteAllText(path, json);
            Debug.Log("[AIQuizLocalStore] Saved: " + path);
        }
        catch (Exception ex) { Debug.LogError("[AIQuizLocalStore] Save failed: " + ex); }
    }

    public static List<QuizSessionMeta> LoadAll(string modelId)
    {
        var list = new List<QuizSessionMeta>();
        try
        {
            string folder = Path.Combine(Root, modelId);
            if (!Directory.Exists(folder)) return list;
            foreach (var f in Directory.GetFiles(folder, "*.json"))
            {
                try { list.Add(JsonUtility.FromJson<QuizSessionMeta>(File.ReadAllText(f))); }
                catch { /* skip bad files */ }
            }
        }
        catch (Exception ex) { Debug.LogError("[AIQuizLocalStore] LoadAll failed: " + ex); }
        return list;
    }

    public static QuizSessionMeta LoadLatest(string modelId)
    {
        var all = LoadAll(modelId);
        if (all.Count == 0) return null;
        all.Sort((a, b) => string.Compare(b.createdAtIsoUtc, a.createdAtIsoUtc, StringComparison.Ordinal));
        return all[0];
    }
}
