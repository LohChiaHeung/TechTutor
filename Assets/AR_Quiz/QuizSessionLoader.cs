using System.Collections.Generic;
using UnityEngine;

public class QuizSessionLoader : MonoBehaviour
{
    [Tooltip("Model ID: keyboard / mouse / monitor / laptop / speaker / default")]
    public string modelId = "keyboard";

    public List<QuizSessionMeta> LoadAll()
    {
        return AIQuizLocalStore.LoadAll(modelId);  // returns List<QuizSessionMeta>
    }

    public QuizSessionMeta LoadLatest()
    {
        var all = AIQuizLocalStore.LoadAll(modelId);
        if (all == null || all.Count == 0) return null;
        all.Sort((a, b) => string.Compare(b.createdAtIsoUtc, a.createdAtIsoUtc));
        return all[0];
    }
}
