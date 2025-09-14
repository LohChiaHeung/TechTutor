using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizHistoryUI : MonoBehaviour
{
    [Header("UI")]
    public Transform listParent;        // ScrollView/Viewport/Content
    public GameObject rowPrefab;        // A prefab containing a Button + TMP_Text
    public GameObject detailPanel;      // Assign a panel (inactive by default)

    void OnEnable() { Refresh(); }

    //public void Refresh()
    //{
    //    foreach (Transform c in listParent) Destroy(c.gameObject);

    //    var hist = QuizHistoryManager.Instance?.GetHistory();
    //    if (hist == null) return;

    //    for (int i = 0; i < hist.Count; i++)
    //    {
    //        var e = hist[i];
    //        var go = Instantiate(rowPrefab, listParent);
    //        var txt = go.GetComponentInChildren<TextMeshProUGUI>();
    //        if (txt) txt.text = $"{e.date} | {e.modelId} | {e.score}/{e.totalQuestions}";

    //        var btn = go.GetComponentInChildren<Button>();
    //        int index = i;
    //        if (btn) btn.onClick.AddListener(() =>
    //        {
    //            QuizHistorySelection.Selected = hist[index]; // pass selection
    //            if (detailPanel) detailPanel.SetActive(true);
    //        });
    //    }
    //}
    public void Refresh()
    {
        foreach (Transform c in listParent) Destroy(c.gameObject);

        var hist = QuizHistoryManager.Instance?.GetHistory();
        if (hist == null) return;

        for (int i = hist.Count - 1; i >= 0; i--)   //  reverse order
        {
            var e = hist[i];
            var go = Instantiate(rowPrefab, listParent);
            var txt = go.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = $"{e.date} | {e.modelId} | {e.score}/{e.totalQuestions}";

            var btn = go.GetComponentInChildren<Button>();
            int index = i;
            if (btn) btn.onClick.AddListener(() =>
            {
                QuizHistorySelection.Selected = hist[index];
                if (detailPanel) detailPanel.SetActive(true);
            });
        }
    }


    public void ClearAll()
    {
        QuizHistoryManager.Instance?.ClearHistory();
        Refresh();
    }
}

// simple static pipe to pass selected entry to detail panel
public static class QuizHistorySelection
{
    public static QuizHistoryEntry Selected;
}
