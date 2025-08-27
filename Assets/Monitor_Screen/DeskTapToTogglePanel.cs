using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class DeskTapToTogglePanel : MonoBehaviour
{
    public Camera arCamera;
    public DeskAdjustPanelController panelController;

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
        if (!panelController) panelController = GetComponentInChildren<DeskAdjustPanelController>(true);
    }

    void Update()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        Ray ray = arCamera.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                if (panelController == null) return;

                bool willShow = !(panelController.gameObject.activeInHierarchy &&
                                  panelController.GetComponent<DeskAdjustPanelController>().enabled &&
                                  panelController.GetComponent<DeskAdjustPanelController>().panel &&
                                  panelController.GetComponent<DeskAdjustPanelController>().panel.activeSelf);

                if (willShow) panelController.ShowPanel();
                else panelController.HidePanel();
            }
        }
    }
}
