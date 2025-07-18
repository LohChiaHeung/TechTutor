using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KeyInteraction : MonoBehaviour
{
    public GameObject canvasRoot;               // ✅ New: assign your Canvas here
    public GameObject infoPanel;
    public TextMeshProUGUI keyNameText;
    public TextMeshProUGUI keyFunctionText;
    public Image keySymbolImage; // Reference to the Image component


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                string keyName = hit.transform.name;
                Debug.Log("Clicked on: " + keyName);

                switch (keyName)
                {
                    case "ControlKey":
                        ShowPanel("Control", "Performs control commands with other keys.");
                        break;
                    case "ArrowKey":
                        ShowPanel("Arrow Keys", "Navigate through text or UI.");
                        break;
                    case "WindowKey":
                        ShowPanel("Windows Key", "Opens Start menu.");
                        break;
                    default:
                        Debug.Log("No panel setup for: " + keyName);
                        break;
                }
            }
        }
    }

    void ShowPanel(string keyName, string function)
    {
        Debug.Log($"[ShowPanel] Showing info for: {keyName} - {function}");

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(true);  // ✅ Show the entire Canvas
        }

        if (infoPanel != null)
        {
            infoPanel.SetActive(true);   // ✅ Show the panel inside it
        }

        if (keyNameText != null)
        {
            keyNameText.text = keyName;
        }

        if (keyFunctionText != null)
        {
            keyFunctionText.text = function;
        }

        if (keySymbolImage != null)
        {
            Sprite symbol = LoadKeySpriteByName(keyName);
            if (symbol != null)
            {
                keySymbolImage.sprite = symbol;
                keySymbolImage.enabled = true;
                Debug.Log($"[ShowPanel] Successfully loaded image for: {keyName}");
            }
            else
            {
                keySymbolImage.enabled = false;
                Debug.LogWarning($"[ShowPanel] ⚠️ No image found for key: {keyName}");
            }
        }
        else
        {
            Debug.LogWarning("[ShowPanel] ⚠️ keySymbolImage is not assigned in Inspector!");
        }
    }

    private Sprite LoadKeySpriteByName(string keyName)
    {
        string path = "KeyIcons/" + keyName;
        Sprite sprite = Resources.Load<Sprite>(path);
        Debug.Log($"[LoadKeySpriteByName] Trying to load from: Resources/{path}");

        return sprite;
    }


    public void HidePanel()
    {
        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(false);  // ✅ Hide entire Canvas again
        }
    }
}
