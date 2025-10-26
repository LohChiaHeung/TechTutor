using TMPro;
using UnityEngine;

public class StepCardView : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descText;

    public void Set(string title, string instruction)
    {
        if (titleText) titleText.text = string.IsNullOrWhiteSpace(title) ? "Step" : title.Trim();

        if (descText)
        {
            if (string.IsNullOrWhiteSpace(instruction))
                descText.text = "";
            else
                descText.text = "→ " + instruction.Trim();
        }
    }
}
