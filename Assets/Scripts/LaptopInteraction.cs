using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LaptopInteraction : MonoBehaviour
{
    public GameObject canvasRoot;
    public GameObject infoPanel;
    public TextMeshProUGUI partNameText;
    public TextMeshProUGUI partFunctionText;
    public Image partSymbolImage;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            HandleRaycast(Camera.main.ScreenPointToRay(Input.mousePosition));
        }
#else
        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            HandleRaycast(Camera.main.ScreenPointToRay(Input.GetTouch(0).position));
        }
#endif
    }

    void HandleRaycast(Ray ray)
    {
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            string partName = hit.transform.name;
            Debug.Log("Clicked on: " + partName);

            switch (partName)
            {
                case "PowerButton(Laptop)":
                    ShowPanel("Power Button (Laptop)",
          "• Used to turn the laptop ON or OFF.\n" +
          "• Usually found near the top-right of the keyboard or on the side.");
                    break;
                case "TouchPad":
                    ShowPanel("Touchpad",
           "• Replaces the mouse for moving the cursor.\n" +
           "• Supports tapping, clicking, and scrolling with fingers.\n" +
           "• Common gestures: two-finger scroll, pinch to zoom.");
                    break;
                case "InternalKeyboard":
                    ShowPanel("Internal Keyboard",
             "• Built-in keyboard for typing text and commands.\n" +
             "• Includes letters, numbers, symbols, and function keys (F1–F12).\n" +
             "• Some keys have shortcuts like brightness or volume.");
                    break;
                case "USBPort":
                    ShowPanel("USB Port",
             "• Allows you to connect devices like a mouse, keyboard, or USB drive.\n" +
             "• Most laptops have 2–4 USB ports.\n" +
             "• Some newer laptops include USB-C ports for faster data and charging.");
                    break;
                case "Webcam":
                    ShowPanel("Webcam",
            "• Captures video and images.\n" +
            "• Used for online meetings or video calls.\n" +
            "• Usually located at the top center of the screen.");
                    break;
                case "ChargingPort":
                    ShowPanel("Charging Port",
           "• Used to plug in the charger and provide power to the laptop.\n" +
           "• Usually located on the side or back of the laptop.\n" +
           "• The laptop battery charges when plugged in.");
                    break;
                default:
                    Debug.Log("No panel configured for: " + partName);
                    break;
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
            Sprite symbol = Resources.Load<Sprite>("LaptopIcons/" + name);
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
