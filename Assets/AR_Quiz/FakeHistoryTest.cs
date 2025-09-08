using UnityEngine;
using System.Collections.Generic;

public class FakeHistoryTest : MonoBehaviour
{
    void Start()
    {
        // only add fake data if history is empty
        if (QuizHistoryManager.Instance != null &&
            QuizHistoryManager.Instance.GetHistory().Count == 0)
        {
            var entry = new QuizHistoryEntry
            {
                modelId = "keyboard",
                sessionId = "test-session-001",
                date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                score = 2,
                totalQuestions = 3,
                items = new List<QuizHistoryItem>()
            };

            // Fake Q1 (MCQ - wrong)
            entry.items.Add(new QuizHistoryItem
            {
                type = "mcq",
                prompt = "Where is the Caps Lock key?",
                options = new[] { "Left Shift", "Caps Lock", "Tab", "Ctrl" },
                correct = 1,
                chosen = 2,
                isCorrect = false
            });

            // Fake Q2 (Pick3D - correct)
            entry.items.Add(new QuizHistoryItem
            {
                type = "pick3d",
                prompt = "Tap the Windows key on the keyboard.",
                pickedName = "Windows",
                isCorrect = true
            });

            // Fake Q3 (MCQ - correct)
            entry.items.Add(new QuizHistoryItem
            {
                type = "mcq",
                prompt = "Which port is used for a monitor?",
                options = new[] { "USB", "HDMI", "Ethernet", "Audio Jack" },
                correct = 1,
                chosen = 1,
                isCorrect = true
            });

            QuizHistoryManager.Instance.AddHistory(entry);
            Debug.Log("[FakeHistoryTest] Added a fake quiz session to history.");
        }
    }
}
