using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatHistoryItemView : MonoBehaviour
{
    [Header("Wiring (match your HistoryItemView UI)")]
    public RawImage thumbnail;
    public TextMeshProUGUI titleText;    // Question
    public TextMeshProUGUI summaryText;  // Answer (preview)
    public TextMeshProUGUI timeText;     // Timestamp
    public Button viewButton;            // Optional: open full detail
    public Button deleteButton;          // Optional

    ChatQAPair data;

    public Action<string> onView;   // id
    public Action<string> onDelete; // id

    const int PreviewChars = 220;

    public void Bind(ChatQAPair qa, Texture2D thumb)
    {
        data = qa;

        if (thumbnail) thumbnail.texture = thumb;
        if (titleText)
        {
            titleText.text = qa.question ?? string.Empty;
            //titleText.alignment = TMPro.TextAlignmentOptions.Center;
        }

        // Hide answer preview, just show time
        if (summaryText) summaryText.gameObject.SetActive(false);

        if (timeText)
        {
            var local = DateTimeOffset.FromUnixTimeSeconds(qa.createdAtUnix).ToLocalTime();
            timeText.text = local.ToString("yyyy-MM-dd HH:mm");
            //timeText.alignment = TMPro.TextAlignmentOptions.Center;
            timeText.color = Color.gray;
            timeText.gameObject.SetActive(true);
        }

        if (viewButton)
        {
            viewButton.onClick.RemoveAllListeners();
            viewButton.onClick.AddListener(() => onView?.Invoke(qa.id));
            viewButton.gameObject.SetActive(true);
        }

        if (deleteButton)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => onDelete?.Invoke(qa.id));
        }
    }

}
