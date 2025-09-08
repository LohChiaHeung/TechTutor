using TMPro;
using UnityEngine;

public class TopicLabelBinder : MonoBehaviour
{
    public TextMeshProUGUI label;

    public void SetTopic(string topic)
    {
        if (!label) label = GetComponent<TextMeshProUGUI>();
        if (label) label.text = topic;
    }
}
