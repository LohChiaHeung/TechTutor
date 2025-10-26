using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MonitorInteraction : MonoBehaviour
{
    public GameObject canvasRoot;
    public GameObject infoPanel;
    public TextMeshProUGUI partNameText;
    public TextMeshProUGUI partFunctionText;
    public Image partSymbolImage;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string partName = hit.transform.name;
                Debug.Log("Clicked on: " + partName);

                switch (partName)
                {
                    case "PowerButton":
                        ShowPanel("Power Button", 
            "• Usually located at the bottom-right of the monitor bezel. \n" +
            "• Used to turn the monitor ON or OFF. \n" +
            "• May have a light indicator when powered on.");
                        break;
                    case "BrandLabel":
                        ShowPanel("Brand Label",
           "• Displays the manufacturer name (e.g., ASUS, Dell, etc). \n" +
           "• Helps users identify the monitor model and brand. \n" +
           "• Usually centered on the bottom bezel.");
                        break;
                    case "HDMILabel":
                        ShowPanel("HDMI Label",
         "• Indicates the current input type (e.g., HDMI). \n" +
         "• Usually found near the bezel or on-screen display. \n" +
         "• HDMI transmits both high-definition video and audio.");
                        break;
                    case "HDMIPort":
                        ShowPanel("HDMI Port",
            "• Located on the back or underside of the monitor. \n" +
            "• Allows you to connect devices like laptops, PCs, or consoles. \n" +
            "• Supports both video and audio transmission.");
                        break;
                    case "PowerPlug":
                        ShowPanel("Power Plug",
           "• Located on the back of the monitor. \n" +
           "• Connects the monitor to a power source using a 3-pin plug. \n" +
           "• Required to supply electricity for the monitor to function.");
                        break;
                    default:
                        Debug.Log("No panel configured for: " + partName);
                        break;
                }
            }
        }
    }

    void ShowPanel(string name, string function)
    {
        if (canvasRoot != null) canvasRoot.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(true);
        if (partNameText != null) partNameText.text = name;
        if (partFunctionText != null) partFunctionText.text = function;

        if (partSymbolImage != null)
        {
            Sprite symbol = Resources.Load<Sprite>("MonitorIcons/" + name);
            if (symbol != null)
            {
                partSymbolImage.sprite = symbol;
                partSymbolImage.enabled = true;
            }
            else
            {
                partSymbolImage.enabled = false;
            }
        }
    }

    public void HidePanel()
    {
        if (canvasRoot != null) canvasRoot.SetActive(false);
        if (infoPanel != null) infoPanel.SetActive(false);
    }
}
