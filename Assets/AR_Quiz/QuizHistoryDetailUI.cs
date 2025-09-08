using UnityEngine;
using TMPro;

public class QuizHistoryDetailUI : MonoBehaviour
{
    [Header("Header")]
    public TextMeshProUGUI titleText;     // e.g., "Keyboard — 2025-09-07 — 4/5"

    [Header("List")]
    public Transform listParent;          // ScrollView/Viewport/Content
    public GameObject itemPrefab;         // A prefab with a TMP_Text to show each Q/A

    void OnEnable()
    {
        var entry = QuizHistorySelection.Selected;
        if (entry == null) { Clear(); return; }

        if (titleText) titleText.text = $"{entry.modelId} — {entry.date} — {entry.score}/{entry.totalQuestions}";

        foreach (Transform c in listParent) Destroy(c.gameObject);

        int qn = 1;
        foreach (var it in entry.items)
        {
            var go = Instantiate(itemPrefab, listParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (!txt) continue;

            if (it.type == "mcq")
            {
                string chosenLabel = it.chosen >= 0 && it.chosen < 4 ? ((char)('A' + it.chosen)).ToString() : "-";
                string correctLabel = it.correct >= 0 && it.correct < 4 ? ((char)('A' + it.correct)).ToString() : "-";
                string opts =
                    (it.options != null && it.options.Length >= 4)
                    ? $"A. {it.options[0]}\nB. {it.options[1]}\nC. {it.options[2]}\nD. {it.options[3]}"
                    : "";

                txt.text =
$@"Q{qn}. {it.prompt}
{opts}
Your answer: {chosenLabel}
Correct: {correctLabel}
Result: {(it.isCorrect ? "Correct" : "Wrong")}";
            }
            else // pick3d
            {
                txt.text =
$@"Q{qn}. {it.prompt}
Picked: {(string.IsNullOrEmpty(it.pickedName) ? "-" : it.pickedName)}
Result: {(it.isCorrect ? "Correct" : "Wrong")}";
            }

            qn++;
        }
    }

    public void Close() => gameObject.SetActive(false);

    private void Clear()
    {
        if (titleText) titleText.text = "No selection";
        foreach (Transform c in listParent) Destroy(c.gameObject);
    }
}
