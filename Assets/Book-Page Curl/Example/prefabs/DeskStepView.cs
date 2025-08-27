using UnityEngine;

public class DeskStepView : MonoBehaviour, ITutorialStepView
{
    public TutorialStepManager stepManager;

    void OnEnable()
    {
        if (StepState.I != null)
        {
            StepState.I.OnStepChanged.AddListener(ApplyStep);
            ApplyStep(StepState.I.CurrentStep);
        }
    }

    void OnDisable()
    {
        if (StepState.I != null)
            StepState.I.OnStepChanged.RemoveListener(ApplyStep);
    }

    public void ApplyStep(int index)
    {
        if (stepManager != null)
        {
            // update the index
            stepManager.index = index;

            // ✅ call the same logic your Start()/Next/Back buttons use
            // if that logic is inside Update(), it will auto-refresh on next frame
            // OR, if you have a method like RefreshStep() inside stepManager, call it here
            var method = stepManager.GetType().GetMethod("RefreshStep",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (method != null)
                method.Invoke(stepManager, null);
        }
    }
}
