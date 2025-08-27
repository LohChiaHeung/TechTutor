// WorldCanvasReuser.cs
using UnityEngine;

public class WorldCanvasReuser : MonoBehaviour
{
    [Header("Existing world-space canvas")]
    public GameObject tutorialCanvas;

    [Header("Placement")]
    public float distance = 1.0f;   // further so it won’t be “in your face”
    public float yOffset = -0.05f;
    public float minDistance = 0.5f;
    public float maxDistance = 2.0f;
    public bool faceCamera = true;

    public bool HasSpawned { get; private set; }

    public void Show()
    {
        if (!tutorialCanvas) { Debug.LogWarning("[WorldCanvasReuser] No tutorialCanvas assigned."); return; }
        tutorialCanvas.SetActive(true);
        var cnv = tutorialCanvas.GetComponentInChildren<Canvas>(true);
        if (cnv && cnv.renderMode == RenderMode.WorldSpace && Camera.main)
            cnv.worldCamera = Camera.main;

        // Re-apply current step if view exists
        var view = tutorialCanvas.GetComponentInChildren<ITutorialStepView>(true);
        if (view != null && StepState.I != null)
            view.ApplyStep(StepState.I.CurrentStep);

        Debug.Log($"[WorldCanvasReuser] Canvas shown (spawned={HasSpawned}).");
    }

    public void Hide()
    {
        if (!tutorialCanvas) return;
        tutorialCanvas.SetActive(false);
        Debug.Log("[WorldCanvasReuser] Canvas hidden.");
    }

    // Call this from Tap-to-Spawn
    public void PlaceAtCamera(bool allowMoveIfAlreadyPlaced)
    {
        // inside PlaceAtCamera(...)
        if (!tutorialCanvas.activeSelf) tutorialCanvas.SetActive(true);

        if (!tutorialCanvas) { Debug.LogWarning("[WorldCanvasReuser] No tutorialCanvas assigned."); return; }
        var cam = Camera.main; if (!cam) { Debug.LogWarning("[WorldCanvasReuser] No MainCamera."); return; }

        if (HasSpawned && !allowMoveIfAlreadyPlaced)
        {
            Debug.Log("[WorldCanvasReuser] Already placed; move not allowed.");
            return;
        }

        float d = Mathf.Clamp(distance, minDistance, maxDistance);
        Vector3 pos = cam.transform.position + cam.transform.forward * d + Vector3.up * yOffset;
        tutorialCanvas.transform.position = pos;

        if (faceCamera)
        {
            Vector3 lookDir = tutorialCanvas.transform.position - cam.transform.position;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude < 1e-4f) lookDir = cam.transform.forward;
            tutorialCanvas.transform.rotation = Quaternion.LookRotation(lookDir, Vector3.up);
        }

        HasSpawned = true;

        // Make sure UI raycasts use camera
        var cnv = tutorialCanvas.GetComponentInChildren<Canvas>(true);
        if (cnv && cnv.renderMode == RenderMode.WorldSpace) cnv.worldCamera = cam;

        // Re-apply step again for safety
        var view = tutorialCanvas.GetComponentInChildren<ITutorialStepView>(true);
        if (view != null && StepState.I != null)
            view.ApplyStep(StepState.I.CurrentStep);

        Debug.Log($"[WorldCanvasReuser] Placed at camera. Step={(StepState.I ? StepState.I.CurrentStep : 0)}.");
    }
}
