using UnityEngine;
using UnityEngine.UI;

public class TutorialModeToggleClone : MonoBehaviour
{
    public GameObject deskRoot;           // "Desk Simulator"
    public CanvasCloneManager cloneMgr;
    public TapToSpawnClone tapToSpawn;
    public Text buttonLabel;
    public bool startInDeskMode = true;

    bool isCanvasMode;

    void Start()
    {
        // Force starting state
        isCanvasMode = !startInDeskMode;

        // If starting in Canvas mode, disable deskRoot immediately so its manager is off
        if (!startInDeskMode && deskRoot)
            deskRoot.SetActive(false);

        ApplyMode(isCanvasMode);
    }
    public void ToggleMode()
    {
        isCanvasMode = !isCanvasMode;
        ApplyMode(isCanvasMode);
    }



    void ApplyMode(bool switchToCanvas)
    {
        //if (switchToCanvas)
        //{
        //    if (deskRoot) deskRoot.SetActive(false);     // hides models, keeps pose
        //    //if (cloneMgr) cloneMgr.CreateCloneIfNeeded();
        //    if (cloneMgr)
        //    {
        //        cloneMgr.CreateCloneIfNeeded();                 // clone exists (maybe inactive)
        //        cloneMgr.ActivateCloneManagerAndResync();       // enable manager
        //        cloneMgr.PlaceCloneAtCamera(true);              // activate & resync next frame
        //    }
        //    if (tapToSpawn) tapToSpawn.enabled = true;   // wait for user tap
        //    Debug.Log($"[ModeToggle] Canvas mode @ step {(StepState.I ? StepState.I.CurrentStep : 0)}");
        //    if (buttonLabel) buttonLabel.text = "Desk";
        //    cloneMgr.ActivateCloneManager(); // enable clone's ARStepManager 
        //}
        //else
        //{
        //    if (tapToSpawn) tapToSpawn.enabled = false;
        //    if (cloneMgr) cloneMgr.DestroyCloneIfAny();  // destroy clone, keep progress via StepState
        //    if (deskRoot) deskRoot.SetActive(true);
        //    Debug.Log($"[ModeToggle] Desk mode @ step {(StepState.I ? StepState.I.CurrentStep : 0)}");
        //    if (buttonLabel) buttonLabel.text = "Canvas";
        //}

        // TutorialModeToggleClone.ApplyMode
        if (switchToCanvas)
        {
            if (deskRoot) deskRoot.SetActive(false);   // Desk off (and its manager)
            if (tapToSpawn) tapToSpawn.enabled = true; // Wait for user tap
            if (buttonLabel) buttonLabel.text = "Desk";
            Debug.Log($"[ModeToggle] Canvas mode (waiting for tap) @ step {(StepState.I ? StepState.I.CurrentStep : 0)}");
        }
        else
        {
            if (tapToSpawn) tapToSpawn.enabled = false;
            if (cloneMgr) cloneMgr.DestroyCloneIfAny();  // remove clone + its manager
            if (deskRoot) deskRoot.SetActive(true);      // Desk back on
            if (buttonLabel) buttonLabel.text = "Canvas";
            Debug.Log($"[ModeToggle] Desk mode @ step {(StepState.I ? StepState.I.CurrentStep : 0)}");
        }

    }


}
