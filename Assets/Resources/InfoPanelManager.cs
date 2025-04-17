using UnityEngine;

public class InfoPanelManager : MonoBehaviour
{
    public GameObject[] allPanels;

    public void ShowOnly(GameObject targetPanel)
    {
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(panel == targetPanel); // Show only the one we want
        }
    }

    public void HideAll()
    {
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(false);
        }
    }
}
