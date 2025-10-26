using System.Collections.Generic;

[System.Serializable]
public class QuizHistoryItem
{
    public string type;          // "mcq" | "pick3d"
    public string prompt;
    public string[] options;     // for mcq; may be null for pick3d
    public int correct = -1;     // mcq: 0..3 ; pick3d: -1
    public int chosen = -1;      // mcq: 0..3 ; pick3d: -1
    public string pickedName;    // pick3d: last tapped key name (canon/raw)
    public bool isCorrect;
}

[System.Serializable]
public class QuizHistoryEntry
{
    public string modelId;
    public string sessionId;
    public string date;
    public int score;
    public int totalQuestions;

    public List<QuizHistoryItem> items = new List<QuizHistoryItem>(); // ⬅️ NEW
}
