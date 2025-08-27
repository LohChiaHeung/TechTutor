using UnityEngine;
using UnityEngine.EventSystems;

public class TapToSpawnClone : MonoBehaviour
{
    public CanvasCloneManager cloneMgr;

    void OnEnable()
    {
        // optional: show hint UI
    }

    //void Update()
    //{
    //    if (Input.touchCount == 0) return;
    //    var t = Input.GetTouch(0);
    //    if (t.phase != TouchPhase.Began) return;

    //    // Ignore taps on UI
    //    if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
    //        return;

    //    if (!cloneMgr)
    //    {
    //        Debug.LogWarning("[TapToSpawn] No CanvasCloneManager assigned.");
    //        return;
    //    }

    //    //    // Create, activate manager, place, resync
    //    //    cloneMgr.CreateCloneIfNeeded();
    //    //    cloneMgr.ActivateCloneManagerAndResync();           // enables TutorialStepManager/ARStepManager on the clone
    //    //    cloneMgr.PlaceCloneAtCamera(allowMoveIfAlreadyPlaced: true); // SetActive(true) + next-frame reapply
    //    //    enabled = false; // done – disable tap script until you switch modes again
    //    //    Debug.Log("[TapToSpawn] Clone spawned on tap.");
    //    //}

    //    if (t.phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
    //    {
    //        cloneMgr.CreateCloneIfNeeded();
    //        cloneMgr.ActivateCloneManagerAndResync();  // enable manager if disabled in prefab
    //        cloneMgr.PlaceCloneAtCamera(true);         // uses the updated placement above
    //        enabled = false; // disable tap-to-spawn after placing
    //    }
    //}

    void Update()
    {
        if (!cloneMgr) return;
        if (Input.touchCount == 0) return;

        var t = Input.GetTouch(0);
        if (t.phase != TouchPhase.Began) return;

        // Ignore taps on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
            return;

        // --- First time placement ---
        if (!cloneMgr.CloneInstance)
        {
            cloneMgr.CreateCloneIfNeeded();
            cloneMgr.ActivateCloneManagerAndResync();
            cloneMgr.PlaceCloneAtCamera(true);   // camera-forward (you can swap to plane raycast later)
            enabled = false;                     // disable until user hits "Reset"
            return;
        }

        // --- Reposition existing clone (after user pressed Reset) ---
        if (cloneMgr.IsRepositionArmed)
        {
            cloneMgr.ActivateCloneManagerAndResync(); // ensures managers enabled
            cloneMgr.PlaceCloneAtCamera(true);        // move to new camera-facing spot (or plane if you add raycast)
            cloneMgr.CompleteReposition(true);        // disarm & (optionally) show
            enabled = false;                          // disable until next Reset
        }
    }

}
