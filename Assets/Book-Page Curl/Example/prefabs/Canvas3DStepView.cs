using UnityEngine;

public class Canvas3DStepView : MonoBehaviour, ITutorialStepView
{
    [Header("Managers (leave empty to auto-bind)")]
    public TutorialStepManager tutorialManager;
    public ARStepManager arManager;

    [Tooltip("Try to find a manager automatically in/near this canvas.")]
    public bool autoBind = true;

    void Awake() { TryAutoBind(); }

    void OnEnable()
    {
        TryAutoBind();

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

    void TryAutoBind()
    {
        if (!autoBind) return;

        // Prefer a local TutorialStepManager (clone prefab case)
        if (!tutorialManager)
        {
            tutorialManager = GetComponent<TutorialStepManager>()
                           ?? GetComponentInChildren<TutorialStepManager>(true)
                           ?? (transform.parent ? transform.parent.GetComponent<TutorialStepManager>() : null)
                           ?? (transform.parent ? transform.parent.GetComponentInChildren<TutorialStepManager>(true) : null);
        }

        // Fallback to ARStepManager (desk root case)
        if (!arManager)
        {
            arManager = GetComponent<ARStepManager>()
                    ?? GetComponentInChildren<ARStepManager>(true)
                    ?? (transform.parent ? transform.parent.GetComponent<ARStepManager>() : null)
                    ?? (transform.parent ? transform.parent.GetComponentInChildren<ARStepManager>(true) : null);
        }

        if (tutorialManager)
            Debug.Log($"[Canvas3DStepView] Bound to TutorialStepManager: {tutorialManager.name}");
        else if (arManager)
            Debug.Log($"[Canvas3DStepView] Bound to ARStepManager: {arManager.name}");
        else
            Debug.LogWarning("[Canvas3DStepView] No manager found. Add TutorialStepManager to the clone prefab or drag-assign.");
    }

    public void ApplyStep(int index)
    {
        // Prefer tutorial manager if present & active
        if (tutorialManager && tutorialManager.isActiveAndEnabled && tutorialManager.gameObject.activeInHierarchy)
        {
            tutorialManager.SetStep(index);
            return;
        }

        // Else try AR manager if present & active
        if (arManager && arManager.isActiveAndEnabled && arManager.gameObject.activeInHierarchy)
        {
            arManager.SetStep(index);
            return;
        }

        // If neither active yet, cache index
        if (tutorialManager) tutorialManager.index = index;
        Debug.LogWarning("[Canvas3DStepView] No active manager; step cached.");
    }
}
