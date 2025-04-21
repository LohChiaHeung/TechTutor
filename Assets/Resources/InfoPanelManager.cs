using UnityEngine;

public class InfoPanelManager : MonoBehaviour
{

    [System.Serializable]
    public class PanelAudioPair
    {
        public GameObject panel;
        public AudioSource audio;
    }

    public PanelAudioPair[] panelAudioPairs;

    public void ShowOnly(GameObject targetPanel)
    {
        foreach (var pair in panelAudioPairs)
        {
            bool isTarget = pair.panel == targetPanel;
            pair.panel.SetActive(isTarget);

            if (pair.audio != null)
            {
                if (isTarget)
                    pair.audio.Play();
                else
                    pair.audio.Stop();
            }
        }
    }

    public void HideAll()
    {
        foreach (var pair in panelAudioPairs)
        {
            pair.panel.SetActive(false);
            if (pair.audio != null)
                pair.audio.Stop();
        }
    }

    public void HidePanel(GameObject targetPanel)
    {
        foreach (var pair in panelAudioPairs)
        {
            if (pair.panel == targetPanel)
            {
                pair.panel.SetActive(false);
                if (pair.audio != null)
                    pair.audio.Stop();
            }
        }
    }
}
