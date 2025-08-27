using UnityEngine;
using UnityEngine.EventSystems;

public class CanvasTapHandler : MonoBehaviour, IPointerClickHandler
{
    [Header("UI panel with adjustment buttons")]
    public GameObject adjustPanel;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!adjustPanel) return;

        // If the click was on a UI element inside the panel, ignore it
        if (eventData.pointerEnter != null && eventData.pointerEnter.transform.IsChildOf(adjustPanel.transform))
            return;

        bool newState = !adjustPanel.activeSelf;
        adjustPanel.SetActive(newState);

        Debug.Log($"[CanvasTapHandler] Adjust panel {(newState ? "shown" : "hidden")}.");
    }
}
