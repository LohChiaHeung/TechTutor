using System.Collections.Generic;
using UnityEngine;

public class QuizHistoryManager : MonoBehaviour
{
    public static QuizHistoryManager Instance;
    private List<QuizHistoryEntry> history = new List<QuizHistoryEntry>();
    private const string SaveKey = "QuizHistory";

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); Load(); }
        else Destroy(gameObject);
    }

    public void AddHistory(QuizHistoryEntry entry)
    {
        history.Add(entry);
        Save();
    }

    public List<QuizHistoryEntry> GetHistory() => history;

    public void ClearHistory()
    {
        history.Clear();
        PlayerPrefs.DeleteKey(SaveKey);
    }

    private void Save()
    {
        string json = JsonUtility.ToJson(new Wrapper { list = history }, prettyPrint: false);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        if (PlayerPrefs.HasKey(SaveKey))
        {
            string json = PlayerPrefs.GetString(SaveKey);
            var w = JsonUtility.FromJson<Wrapper>(json);
            history = (w != null && w.list != null) ? w.list : new List<QuizHistoryEntry>();
        }
        else history = new List<QuizHistoryEntry>();
    }

    [System.Serializable] private class Wrapper { public List<QuizHistoryEntry> list; }
}
