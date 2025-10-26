using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class ChatHistoryDetailPanel : MonoBehaviour
{
    [Header("Wiring")]
    public GameObject root;           // this GameObject (panel root)
    public RawImage heroImage;        // big image at top
    public TMP_Text titleText;        // question (short)
    public TMP_Text timeText;         // timestamp
    public TMP_Text answerText;       // full AI reply
    public Button closeButton;

    void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(() => Show(false));
        //Show(false);
    }

    public void OpenById(string id)
    {
        Show(true);

        var data = ChatHistoryStore.Instance?.Data?.items?.FirstOrDefault(x => x.id == id);
        if (data == null) return;

        // Question
        if (titleText) titleText.text = data.question ?? "";

        // Time
        if (timeText)
        {
            var local = System.DateTimeOffset.FromUnixTimeSeconds(data.createdAtUnix).ToLocalTime();
            timeText.text = local.ToString("yyyy-MM-dd HH:mm");
        }

        // Image (default if none)
        if (heroImage) heroImage.texture = ChatHistoryStore.Instance.LoadImageOrDefault(data.imagePath);

        // Full answer
        if (answerText) answerText.text = data.answer ?? "";

        Show(true);
    }

    public void Show(bool on)
    {
        if (root) root.SetActive(on);
        else gameObject.SetActive(on);
    }
}
