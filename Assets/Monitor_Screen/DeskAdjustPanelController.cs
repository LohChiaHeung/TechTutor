using UnityEngine;

public class DeskAdjustPanelController : MonoBehaviour
{
    [Header("Target")]
    public Transform deskRoot; // Desk Simulator root
    public DeskRepositionController reposition;

    [Header("Panel")]
    public GameObject panel;

    // Initial pose & scale
    private Vector3 _initPos;
    private Quaternion _initRot;
    private Vector3 _initScale;
    private bool _captured;

    void Start()
    {
        if (!deskRoot) deskRoot = transform.root;
        //CaptureInitialPose();
        HidePanel();
    }

    public void CaptureInitialPose()
    {
        if (!deskRoot) return;
        _initPos = deskRoot.position;
        _initRot = deskRoot.rotation;
        _initScale = deskRoot.localScale;
        _captured = true;
    }

    public void ShowPanel()
    {
        if (panel) panel.SetActive(true);
        if (!_captured) CaptureInitialPose();
        FacePanelTowardCamera();
    }

    public void HidePanel()
    {
        if (panel) panel.SetActive(false);
    }

    // --- Reset functions ---
    public void ResetPosition()
    {
        if (!_captured || !deskRoot) return;
        deskRoot.position = _initPos;
        deskRoot.rotation = _initRot;

        // Re-align baseline so buttons/drag use the same height Reset used
        var rep = reposition ? reposition : deskRoot.GetComponent<DeskRepositionController>();
        if (!rep) rep = deskRoot.GetComponentInChildren<DeskRepositionController>(true);
        if (rep) rep.SetBaselinePlaneY(deskRoot.position.y - rep.planeYOffset);

    }

    //public void ResetScale()
    //{
    //    if (!_captured || !deskRoot) return;
    //    deskRoot.localScale = _initScale;
    //}

    //public void ResetScale()
    //{
    //    if (!_captured || !deskRoot) return;
    //    deskRoot.localScale = _initScale;

    //    // if your model scales on a child, the root scale may be 1 and the child varies.
    //    // If you're using DeskRepositionController.scaleTarget, let it fix the foot:
    //    var rep = reposition ? reposition : deskRoot.GetComponentInChildren<DeskRepositionController>(true);
    //    if (rep)
    //    {
    //        // ensure baseline matches current Y (usually already true)
    //        rep.SetBaselinePlaneY(deskRoot.position.y - rep.planeYOffset);

    //        // replant to baseline in case bounds changed
    //        // (we can expose a public method or reuse movement to snap)
    //        deskRoot.position = new Vector3(
    //            deskRoot.position.x,
    //            rep.transform.position.y, // unchanged
    //            deskRoot.position.z
    //        );

    //        // Ask the controller to replant via ApplyMove
    //        // (ApplyMove enforces Y and ReplantFootToBaseline handles bottom)
    //        rep.SendMessage("ReplantFootToBaseline", SendMessageOptions.DontRequireReceiver);
    //        rep.SendMessage("ApplyMove", deskRoot.position, SendMessageOptions.DontRequireReceiver);
    //    }
    //}
    public void ResetScale()
    {
        if (!_captured || !deskRoot) return;

        // Ask mover to restore original child scales and replant
        var rep = reposition ? reposition : deskRoot.GetComponentInChildren<DeskRepositionController>(true);
        if (rep)
        {
            rep.ResetScaledBranches();
            // Keep baseline consistent with current plane height
            rep.SetBaselinePlaneY(deskRoot.position.y - rep.planeYOffset);
        }
    }


    private void FacePanelTowardCamera()
    {
        if (!panel) return;
        var cam = Camera.main;
        if (!cam) return;

        if (panel.GetComponent<Canvas>() && panel.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace)
        {
            var t = panel.transform;
            Vector3 dir = (t.position - cam.transform.position).normalized;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                t.rotation = Quaternion.LookRotation(dir, Vector3.up);
        }
    }

    // Optional: UI slider hook
    public void OnStepSizeChanged(float meters)
    {
        if (reposition) reposition.SetMoveStep(meters);
    }

    // Button hooks for Close
    public void OnClickClose() { HidePanel(); }
}
