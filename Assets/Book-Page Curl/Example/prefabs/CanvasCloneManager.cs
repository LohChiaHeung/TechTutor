using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class CanvasCloneManager : MonoBehaviour
{
    public GameObject sourceCanvas;
    public float distance = 1.0f;
    public float yOffset = -0.05f;
    public bool faceCamera = true;

    public float viewHeightPercent = 0.35f;  // how tall (screen %) the canvas should look
    public float spawnMeters = 1.8f;         // nominal distance from camera (meters)
    public Vector2 canvasPixels = new Vector2(800, 500); // your design size
    public float pixelToMeter = 0.001f;      // 1000 px ≈ 1 m

    private bool _repositionArmed = false;
    public bool IsRepositionArmed => _repositionArmed;

    [SerializeField] private TapToSpawnClone tapSpawner; // drag in Inspector
    [SerializeField] private bool hideDuringReset = false; // optional toggle


    [Header("UI References")]
    public Button resetButton;
    public GameObject CloneInstance { get; private set; }

    void OnEnable()
    {
        if (resetButton) resetButton.gameObject.SetActive(false); // start hidden
    }
    public void CreateCloneIfNeeded()
    {
        if (!sourceCanvas) { Debug.LogWarning("[CloneMgr] Source canvas not assigned."); return; }
        if (CloneInstance) return;
        CloneInstance = Instantiate(sourceCanvas);
        CloneInstance.name = sourceCanvas.name + "_CLONE";
        CloneInstance.transform.SetParent(null, true);
        CloneInstance.SetActive(false);

        //var cnv = CloneInstance.GetComponentInChildren<Canvas>(true);
        //if (cnv && cnv.renderMode == RenderMode.WorldSpace && Camera.main)
        //    cnv.worldCamera = Camera.main;

        //// ensure view is present & synced
        //if (!CloneInstance.GetComponentInChildren<Canvas3DStepView>(true))
        //    CloneInstance.AddComponent<Canvas3DStepView>(); // will auto-bind
        //if (StepState.I) CloneInstance.GetComponentInChildren<ITutorialStepView>(true)?.ApplyStep(StepState.I.CurrentStep);

        //Debug.Log("[CloneMgr] Clone created.");
    }

    //public void DestroyCloneIfAny()
    //{
    //    if (CloneInstance) { Destroy(CloneInstance); CloneInstance = null; Debug.Log("[CloneMgr] Clone destroyed."); }
    //}

    public void DestroyCloneIfAny()
    {
        if (CloneInstance)
        {
            Destroy(CloneInstance);
            CloneInstance = null;
        }
        if (resetButton) resetButton.gameObject.SetActive(false); // hide again when leaving canvas mode
    }
    //public void PlaceCloneAtCamera(bool allowMoveIfAlreadyPlaced = false)
    //{
    //    if (!CloneInstance) { Debug.LogWarning("[CloneMgr] No clone to place."); return; }
    //    if (!allowMoveIfAlreadyPlaced && CloneInstance.activeSelf) return;

    //    var cam = Camera.main; if (!cam) { Debug.LogWarning("[CloneMgr] No MainCamera."); return; }
    //    CloneInstance.SetActive(true);

    //    float d = Mathf.Clamp(distance, 0.5f, 2.5f);
    //    Vector3 pos = cam.transform.position + cam.transform.forward * d + Vector3.up * yOffset;
    //    Quaternion rot = faceCamera ? Quaternion.LookRotation((pos - cam.transform.position).normalized, Vector3.up) : CloneInstance.transform.rotation;

    //    CloneInstance.transform.SetPositionAndRotation(pos, rot);

    //    var cnv = CloneInstance.GetComponentInChildren<Canvas>(true);
    //    if (cnv) { cnv.overrideSorting = true; cnv.sortingOrder = 5000; if (cnv.renderMode == RenderMode.WorldSpace) cnv.worldCamera = cam; }

    //    // keep size sane
    //    var rt = CloneInstance.GetComponent<RectTransform>();
    //    if (rt && (rt.rect.width > 10f || rt.rect.height > 10f)) rt.sizeDelta = new Vector2(0.7f, 0.4f);
    //    if (rt && rt.localScale == Vector3.zero) rt.localScale = Vector3.one;

    //    if (StepState.I)
    //        CloneInstance.GetComponentInChildren<ITutorialStepView>(true)
    //            ?.ApplyStep(StepState.I.CurrentStep);
    //    Debug.Log($"[CloneMgr] Clone placed at step {StepState.I?.CurrentStep}.");

    //    CloneInstance.SetActive(true);
    //    StartCoroutine(ResyncNextFrame());
    //}

    public void PlaceCloneAtCamera(bool allowMoveIfAlreadyPlaced = false)
    {
        if (!CloneInstance) { Debug.LogWarning("[CloneMgr] No clone to place."); return; }
        if (!allowMoveIfAlreadyPlaced && CloneInstance.activeSelf) return;

        var cam = Camera.main;
        if (!cam) { Debug.LogWarning("[CloneMgr] No MainCamera."); return; }

        // 1) Position in front of camera
        Vector3 pos = cam.transform.position + cam.transform.forward * spawnMeters;
        Quaternion rot = Quaternion.LookRotation((pos - cam.transform.position).normalized, Vector3.up);
        CloneInstance.transform.SetPositionAndRotation(pos, rot);

        // 2) Apply fixed size + scale
        var rt = CloneInstance.GetComponent<RectTransform>();
        if (rt)
        {
            rt.sizeDelta = canvasPixels;                       // set pixel size
            rt.localScale = Vector3.one * pixelToMeter;        // scale to meters
        }

        // 3) World-space canvas settings
        var cnv = CloneInstance.GetComponentInChildren<Canvas>(true);
        if (cnv)
        {
            if (cnv.renderMode == RenderMode.WorldSpace) cnv.worldCamera = cam;
            cnv.overrideSorting = true;
            cnv.sortingOrder = 5000;
        }

        // 4) Activate and apply current step
        CloneInstance.SetActive(true);
        if (StepState.I)
            CloneInstance.GetComponentInChildren<ITutorialStepView>(true)
                ?.ApplyStep(StepState.I.CurrentStep);

        Debug.Log($"[CloneMgr] Clone placed at step {StepState.I?.CurrentStep}.");

        // Safety: reapply next frame in case of enable-order race
        StartCoroutine(ResyncNextFrame());

        if (resetButton) resetButton.gameObject.SetActive(true);
    }

    private IEnumerator ResyncNextFrame()
    {
        yield return null; // wait 1 frame so all OnEnable have fired
        var view = CloneInstance.GetComponentInChildren<Canvas3DStepView>(true);
        if (view && StepState.I != null)
            view.ApplyStep(StepState.I.CurrentStep);
        Debug.Log("[CloneMgr] Re-applied step after activation.");
    }
    // CanvasCloneManager.cs
    public void ActivateCloneManager()
    {
        if (!CloneInstance) return;
        if (!CloneInstance) { Debug.LogWarning("[CloneMgr] No clone to activate."); return; }

        var mgr = CloneInstance.GetComponentInChildren<ARStepManager>(true);
        if (mgr != null && !mgr.enabled)
        {
            mgr.enabled = true;

            // Optional: immediately sync to current step
            if (StepState.I != null)
                mgr.SetStep(StepState.I.CurrentStep);

            Debug.Log("[CloneMgr] Activated ARStepManager on clone.");
        }
    }

    public void ActivateCloneManagerAndResync()
    {
        if (!CloneInstance) { Debug.LogWarning("[CloneMgr] No clone to activate."); return; }

        // 1) Ensure the manager component is enabled
        var mgr = CloneInstance.GetComponentInChildren<TutorialStepManager>(true);
        if (mgr && !mgr.enabled) mgr.enabled = true;

        // 2) If the clone isn't active yet, we'll resync after activation below
    }

    public void ResetPlacement()  // <- this will appear in the Button dropdown
    {
        CreateCloneIfNeeded();        // ensure clone exists
        _repositionArmed = true;      // arm reposition for next tap

        if (hideDuringReset && CloneInstance)
            CloneInstance.SetActive(false);   // optional: hide until re-tap

        if (tapSpawner) tapSpawner.enabled = true;  // re‑enable tap-to-spawn
    }

    //public void BeginReposition(TapToSpawnClone spawner, bool hideDuringReset = false)
    //{
    //    CreateCloneIfNeeded();                  // ensure we have an instance
    //    _repositionArmed = true;                // arm reposition

    //    if (hideDuringReset && CloneInstance)   // optional: hide while waiting for new tap
    //        CloneInstance.SetActive(false);

    //    if (spawner) spawner.enabled = true;    // re-enable tap script to capture next tap
    //}

    /// <summary>Call after the new tap places the clone.</summary>
    public void CompleteReposition(bool showAfter = true)
    {
        _repositionArmed = false;
        if (showAfter && CloneInstance && !CloneInstance.activeSelf)
            CloneInstance.SetActive(true);
    }



}
