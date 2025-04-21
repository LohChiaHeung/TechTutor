using UnityEngine;

public class ClickableObjectHandler : MonoBehaviour
{
    public GameObject infoPanel;
    public InfoPanelManager panelManager;

    void Start()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);
    }

    void OnMouseDown()
    {
        if (infoPanel.activeSelf)
        {
            // Close panel (audio will stop via manager)
            panelManager.HidePanel(infoPanel);
        }
        else
        {
            // Show only this panel (audio will auto-play via manager)
            panelManager.ShowOnly(infoPanel);
        }
    }
}
