using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HistoryItemView : MonoBehaviour
{
    public RawImage thumbnail;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI summaryText;

    // NEW fields
    public TextMeshProUGUI timeText;
    public Button viewButton;

    // Optional: quick setter to keep your main code clean
    public void Set(Texture texture, string title, string summary)
    {
        if (thumbnail) thumbnail.texture = texture;
        if (titleText) titleText.text = title;
        if (summaryText) summaryText.text = summary;
    }
}
