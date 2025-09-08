using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatHistoryList : MonoBehaviour
{
    [Header("Wiring")]
    public Transform contentRoot;          // ScrollView/Viewport/Content
    public ChatHistoryItemView rowPrefab;  // Prefab using the script above
    public TMP_InputField searchInput;     // Optional search box
    public Button clearAllButton;          // Optional clear
    public Button refreshButton;           // Optional refresh/manual
    public GameObject historyPanel;

    [Header("Detail Panel")]
    public ChatHistoryDetailPanel detailPanel;

    public void ShowHistoryPanel()
    {
        historyPanel.SetActive(true);
    }
    public void HideHistoryPanel()
    {
        historyPanel.SetActive(false);
    }
    
    void OnEnable()
    {
        if (ChatHistoryStore.Instance != null)
            ChatHistoryStore.Instance.OnChanged += Refresh;

        if (clearAllButton) clearAllButton.onClick.AddListener(() => ChatHistoryStore.Instance.ClearAll());
        if (refreshButton) refreshButton.onClick.AddListener(Refresh);
        if (searchInput) searchInput.onValueChanged.AddListener(_ => Refresh());

        Refresh();
    }

    void OnDisable()
    {
        if (ChatHistoryStore.Instance != null)
            ChatHistoryStore.Instance.OnChanged -= Refresh;

        if (clearAllButton) clearAllButton.onClick.RemoveAllListeners();
        if (refreshButton) refreshButton.onClick.RemoveAllListeners();
        if (searchInput) searchInput.onValueChanged.RemoveAllListeners();
    }

    public void Refresh()
    {
        if (ChatHistoryStore.Instance == null) return;

        // Clear
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            Destroy(contentRoot.GetChild(i).gameObject);

        string filter = searchInput ? searchInput.text.Trim().ToLower() : string.Empty;

        var items = ChatHistoryStore.Instance.Data.items;
        var query = string.IsNullOrEmpty(filter)
            ? items
            : items.Where(i =>
                (i.question?.ToLower().Contains(filter) ?? false) ||
                (i.answer?.ToLower().Contains(filter) ?? false));

        foreach (var qa in query.Reverse())
        {
            Texture2D thumb = ChatHistoryStore.Instance.LoadImageOrDefault(qa.imagePath);
            var row = Instantiate(rowPrefab, contentRoot);

            row.onView = OpenFullDetail;
            row.onDelete = id => ChatHistoryStore.Instance.Delete(id);
            row.Bind(qa, thumb);  // your existing bind

            // Force row to match Content width
            //var rt = row.GetComponent<RectTransform>();
            //rt.anchorMin = new Vector2(0f, 1f);
            //rt.anchorMax = new Vector2(1f, 1f);
            //rt.pivot = new Vector2(0.5f, 1f);
            //rt.offsetMin = new Vector2(0f, rt.offsetMin.y); // left = 0
            //rt.offsetMax = new Vector2(0f, rt.offsetMax.y); // right = 0
            //rt.sizeDelta = new Vector2(0f, rt.sizeDelta.y); // no fixed width
            var le = row.GetComponent<LayoutElement>();
            if (!le) le = row.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 290f;
            le.preferredHeight = 290f;
            le.flexibleHeight = 0f;

        }
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot as RectTransform);


    }
    void OpenFullDetail(string id)
    {

        // Close history list
        if (historyPanel) historyPanel.SetActive(false);
        if (detailPanel) detailPanel.OpenById(id);
        else Debug.LogWarning("[ChatHistoryList] Detail panel not assigned.");
    }

    //void OpenFullDetail(string id)
    //{
    //    var qa = ChatHistoryStore.Instance.Data.items.FirstOrDefault(x => x.id == id);
    //    if (qa == null) return;

    //    // Minimal popup: log or route to your existing info panel
    //    Debug.Log($"[ChatHistoryList] View: {qa.question}\n{qa.answer}");
    //    // You can open your existing info panel here and fill full content.
    //}
}
