using System.Collections.Generic;
using UnityEngine;

public class CanvasPanelSwitcher : MonoBehaviour
{
    [Tooltip("Assign all step panels in order. Background image stays separate/unchanged.")]
    public List<GameObject> panels = new();

    public void ShowPanel(int index)
    {
        if (panels == null || panels.Count == 0) return;

        for (int i = 0; i < panels.Count; i++)
            if (panels[i]) panels[i].SetActive(i == index);

        Debug.Log($"[CanvasPanelSwitcher] Showing panel index {index}");
    }
}
