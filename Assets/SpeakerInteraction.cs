using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeakerInteraction : MonoBehaviour
{
    public GameObject canvasRoot;               // The full canvas object (to be enabled/disabled)
    public GameObject infoPanel;                // The panel inside the canvas
    public TextMeshProUGUI partNameText;        // Top title text
    public TextMeshProUGUI partFunctionText;    // Description text
    public Image partSymbolImage;               // Optional icon

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
            Debug.Log("[SpeakerInteraction] Clicked on: " + partName);

            switch (partName)
            {
                case "SpeakerDriver":
                    ShowPanel("Speaker Driver", "🔊 Converts electrical signals into sound. It's the core sound-producing component.");
                    break;
                case "VolumeKnob":
                    ShowPanel("Volume Knob", "🎚 Adjusts the loudness of the speaker output.");
                    break;
                case "BassPort":
                    ShowPanel("Bass Reflex Port", "🔉 Enhances low-frequency sounds (bass) for deeper audio.");
                    break;
                case "PowerButton":
                    ShowPanel("Power Button", "⚡️ Turns the speaker on or off.");
                    break;
                default:
                    Debug.Log("[SpeakerInteraction] No panel setup for: " + partName);
                    break;
            }
        }
    }

    void ShowPanel(string name, string function)
    {
        if (canvasRoot != null)
            canvasRoot.SetActive(true);

        if (infoPanel != null)
            infoPanel.SetActive(true);

        if (partNameText != null)
            partNameText.text = name;

        if (partFunctionText != null)
            partFunctionText.text = function;

        if (partSymbolImage != null)
        {
            Sprite symbol = Resources.Load<Sprite>("SpeakerIcons/" + name);
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
        if (canvasRoot != null)
            canvasRoot.SetActive(false);

        if (infoPanel != null)
            infoPanel.SetActive(false);
    }
}
