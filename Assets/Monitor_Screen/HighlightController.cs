using System.Collections.Generic;
using UnityEngine;

public class VisibilityGroupController : MonoBehaviour
{
    [Header("Parent that contains all highlightable objects")]
    public Transform groupRoot; // parent of all your highlight objects

    public void ShowOnly(IEnumerable<GameObject> toShow)
    {
        if (!groupRoot)
        {
            Debug.LogWarning("[VisibilityGroupController] groupRoot not assigned.");
            return;
        }

        // Disable everyone under root
        for (int i = 0; i < groupRoot.childCount; i++)
            groupRoot.GetChild(i).gameObject.SetActive(false);

        // Enable only the ones we want this step
        if (toShow == null) return;
        foreach (var go in toShow)
            if (go) go.SetActive(true);
    }

    public void HideAll()
    {
        if (!groupRoot) return;
        for (int i = 0; i < groupRoot.childCount; i++)
            groupRoot.GetChild(i).gameObject.SetActive(false);
    }
}
