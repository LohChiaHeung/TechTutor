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
    private float clickCooldown = 0.1f;
    private GameObject lastHitObject = null;
    private float lastClickTime = 0f;
    private static bool panelIsOpen = false;


    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray);

            GameObject hitObject = null;

            foreach (var hit in hits)
            {
                string name = hit.transform.name;

                // Skip ARPlane or unrelated objects
                if (name.Contains("ARPlane") || name.Contains("Plane")) continue;

                hitObject = hit.transform.gameObject;
                Debug.Log("Raycast hit: " + name);
                break; // ✅ Use only the first valid hit
            }

            if (hitObject == null) return;

            // ✅ Prevent duplicate panel show
            if (hitObject == lastHitObject && Time.time - lastClickTime < clickCooldown)
            {
                Debug.Log("[KeyInteraction] Ignored duplicate hit on: " + hitObject.name);
                return;
            }

            lastHitObject = hitObject;
            lastClickTime = Time.time;

            string keyName = hitObject.name;
            Debug.Log("Clicked on: " + keyName);

            switch (keyName)
            {
                case "ControlKey":
                    ShowPanel("Control Key (Crtl)", "• Position: Bottom-left & bottom-right corners of the keyboard. \n• It is used together with other keys to <b><u>perform shortcuts</u></b>. \n• Common Use: Crtl + C --> Copy, Crtl + V --> Paste");
                    break;
                case "ArrowKey":
                    ShowPanel("Arrow Keys", "• Position: Bottom-right area of the keyboard, arranged in an inverted T shape. \n• Used to <b><u>move the cursor</b></u> or navigate through text, menus, or game elements. \n• Common Use: ↑ / ↓ --> Scroll up or down, ← / → -> Move left or right (text or menus).");
                    break;
                case "WindowKey":
                    ShowPanel("Windows Key", "• Position: Bottom-left of the keyboard, usually between Crtl and Alt. \n• Opens the Start menu and is used with other keys for <b><u>shortcuts</b></u>. \n• Common Use: Win + D --> Show Desktop, Win + L --> Lock PC.");
                    break;
                case "VolumeUpDownKey":
                    ShowPanel("Volume Key", "• Position: Usually found on the top row of the keyboard, or on the side of laptops. \n• Used to <u><b>adjust system sound level</u></b> without opening settings. \n• Common Use: Volume Up / Down --> Increase /Decrease Sound Level. \n• Tip: Useful for quickly controlling sound during video.");
                    break;
                case "CapsKey":
                    ShowPanel("Caps Key", "• Position: Left side of the keyboard. \n• Used to toggle capital letter mode. When enabled, all letter you type will be in <u><b>UPPERCASE</b></u>. \n• Common Use: Emphasizing text in documents or games. \n• Tip: Press again to turn it off.Most keyboards have a small light indicator when Caps Lock is active.");
                    break;
                case "DeleteKey":
                    ShowPanel("Delete Key", "• Position: Usually located in the top-right area of the keyboard. Often above or near the arrow keys and <u><b>Insert</b></u> key. \n• Deletes the character in front of the cursor (to the right). Not the same as <u><b>Backspace</b></u>. \n• Common Use: Delete files or folders, Clear items in forms or spreadsheets.");
                    break;
                default:
                    Debug.Log("No panel setup for: " + keyName);
                    break;
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