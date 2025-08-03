using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MouseInteraction : MonoBehaviour
{
    public GameObject canvasRoot;               // Assign your UI Canvas
    public GameObject infoPanel;                // Panel to show mouse info
    public TextMeshProUGUI partNameText;        // UI Text for part name
    public TextMeshProUGUI partFunctionText;    // UI Text for description
    public Image partSymbolImage;               // Optional icon per part

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Tap/click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string partName = hit.transform.name;
                Debug.Log("Clicked on: " + partName);

                switch (partName)
                {
                    case "LeftClick":
                        ShowPanel("Left Click", "• Use to select or open items.\n• Most commonly used button.");
                        break;
                    case "RightClick":
                        ShowPanel("Right Click", "• Opens context menu with more options.\n• Example: Copy, Paste, Rename.");
                        break;
                    case "ScrollWheel":
                        ShowPanel("Scroll Wheel", "• Scroll up/down on pages.\n• Click to open links in a new tab.");
                        break;
                    default:
                        Debug.Log("No panel setup for: " + partName);
                        break;
                }
            }
        }
    }

    void ShowPanel(string partName, string function)
    {
        if (canvasRoot != null) canvasRoot.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(true);
        if (partNameText != null) partNameText.text = partName;
        if (partFunctionText != null) partFunctionText.text = function;

        if (partSymbolImage != null)
        {
            Sprite symbol = LoadMouseSpriteByName(partName);
            if (symbol != null)
            {
                partSymbolImage.sprite = symbol;
                partSymbolImage.enabled = true;
            }
            else
            {
                partSymbolImage.enabled = false;
                Debug.LogWarning($"⚠️ No image found for: {partName}");
            }
        }
    }

    public void HidePanel()
    {
        if (canvasRoot != null) canvasRoot.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }

    private Sprite LoadMouseSpriteByName(string partName)
    {
        string path = "MouseIcons/" + partName;
        return Resources.Load<Sprite>(path);
    }
}
