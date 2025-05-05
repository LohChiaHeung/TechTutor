using UnityEngine;

public class LaptopClickHandler : MonoBehaviour
{
    public GameObject infoPanel;
    public AudioSource narrationAudio;
    public InfoPanelManager panelManager;

    private bool isPanelVisible = false;

    void Start()
    {
        if (infoPanel != null)
            infoPanel.SetActive(false);

        if (narrationAudio != null)
            narrationAudio.Stop();
    }

    void OnMouseDown()
    {
        isPanelVisible = !isPanelVisible;

        if (isPanelVisible)
        {
            panelManager.ShowOnly(infoPanel); // Show this, hide others
            if (narrationAudio != null) narrationAudio.Play();
        }
        else
        {
            infoPanel.SetActive(false);
            if (narrationAudio != null) narrationAudio.Stop();
        }
    }
}
