using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]

public class TutorialSteps
{
    [Header("Canvas")]
    public int panelIndex = -1;

    [Header("Screen")]
    public int screenMaterialIndex = -1;

    [Header("Highlights / Visibility")]
    public GameObject[] objectsToShow;
}

public class TutorialStepManager : MonoBehaviour
{
    [Header("Controllers")]
    public CanvasPanelSwitcher canvasPanels;
    public ScreenMaterialManager screen;
    public VisibilityGroupController visibility;
    public NarrationPlayerPersistent narrator; // assign in Inspector

    [Header("UI References")]
    public TextMeshProUGUI progressText; // Assign in Inspector
    public Button backButton;            // Assign in Inspector
    public Button nextButton;            // Assign in Inspector

    [Header("Steps")]
    public TutorialSteps[] steps;
    public int index = 0;

    [Header("State Sync")]
    public bool broadcastToStepState = true;   // <- NEW: enable/disable broadcasting
    private bool _applyingFromState = false;   // <- NEW: prevents feedback loops

    bool _needsRefreshOnEnable;
    void OnEnable() // <- NEW: listen to StepState & sync when this manager becomes active
    {
        if (StepState.I != null)
        {
            StepState.I.OnStepChanged.AddListener(OnExternalStepChanged);
            // Immediately sync to the current step so this manager shows the right panel
            OnExternalStepChanged(StepState.I.CurrentStep);
        }
    }

    void OnDisable() // <- NEW
    {
        if (StepState.I != null)
            StepState.I.OnStepChanged.RemoveListener(OnExternalStepChanged);
    }

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[TutorialStepManager] No steps assigned.");
            return;
        }

        // If StepState exists, prefer it as the source of truth on first show
        if (StepState.I != null)
            index = Mathf.Clamp(StepState.I.CurrentStep, 0, steps.Length - 1);

        ApplyStep(index);
    }

    public void Next()
    {
        if (steps == null || steps.Length == 0) return;
        index = Mathf.Min(index + 1, steps.Length - 1);
        ApplyStep(index);
    }

    public void Back()
    {
        if (steps == null || steps.Length == 0) return;
        index = Mathf.Max(index - 1, 0);
        ApplyStep(index);
    }

    // <- NEW: invoked when some other UI (desk or clone) updates StepState
    private void OnExternalStepChanged(int i)
    {
        if (steps == null || steps.Length == 0) return;

        // Avoid redundant work
        int clamped = Mathf.Clamp(i, 0, steps.Length - 1);
        //if (clamped == index && gameObject.activeInHierarchy && isActiveAndEnabled) return;

        _applyingFromState = true;   // suppress broadcasting back to StepState
        SetStep(clamped);
        _applyingFromState = false;
    }

    //void ApplyStep(int i)
    //{

    //    var s = steps[i];

    //    GameObject activePanel = null;
    //    if (canvasPanels && s.panelIndex >= 0)
    //    {
    //        canvasPanels.ShowPanel(s.panelIndex);
    //        if (canvasPanels.panels != null &&
    //            s.panelIndex >= 0 && s.panelIndex < canvasPanels.panels.Count)
    //        {
    //            activePanel = canvasPanels.panels[s.panelIndex];
    //        }
    //    }

    //    if (screen && s.screenMaterialIndex >= 0)
    //        screen.SetScreen(s.screenMaterialIndex);

    //    if (visibility)
    //        visibility.ShowOnly(s.objectsToShow);

    //    UpdateProgressUI();

    //    // 🔊 Speak (persistent cache to disk)
    //    if (narrator && activePanel)
    //    {
    //        var pn = activePanel.GetComponent<PanelNarration>();
    //        if (pn != null)
    //        {
    //            StartCoroutine(SpeakAfterFrame(pn));
    //        }
    //    }

    //    // --- NEW: broadcast to StepState so BOTH canvases stay in sync
    //    if (broadcastToStepState && !_applyingFromState && StepState.I != null)
    //        StepState.I.CurrentStep = i;

    //    Debug.Log($"[TutorialStepManager] Step {i + 1}/{steps.Length} applied.");
    //}

    void ApplyStep(int i)
    {
        var s = steps[i];

        GameObject activePanel = null;

        // ✅ Default to global index if panelIndex is -1
        if (canvasPanels)
        {
            int pi = (s.panelIndex >= 0) ? s.panelIndex : i;
            canvasPanels.ShowPanel(pi);

            if (canvasPanels.panels != null &&
                pi >= 0 && pi < canvasPanels.panels.Count)
            {
                activePanel = canvasPanels.panels[pi];
            }

            // Optional: quick log to verify mapping
            // Debug.Log($"[Tutorial] global={i} -> panelIndex={pi} -> {activePanel?.name}");
        }

        if (screen && s.screenMaterialIndex >= 0)
            screen.SetScreen(s.screenMaterialIndex);

        if (visibility)
            visibility.ShowOnly(s.objectsToShow);

        UpdateProgressUI();

        if (narrator && activePanel)
        {
            var pn = activePanel.GetComponent<PanelNarration>();
            if (pn != null) StartCoroutine(SpeakAfterFrame(pn));
        }

        // If your buttons now call StepState directly, consider turning this off in Inspector
        if (broadcastToStepState && !_applyingFromState && StepState.I != null)
            StepState.I.CurrentStep = i;

        Debug.Log($"[TutorialStepManager] Step {i + 1}/{steps.Length} applied.");
    }


    System.Collections.IEnumerator SpeakAfterFrame(PanelNarration pn)
    {
        yield return null; // small delay helps during panel transitions
        if (narrator) narrator.SpeakFromPanel(pn);
    }

    void UpdateProgressUI()
    {
        bool isIntro = index == 0;
        bool isLast = steps != null && index == steps.Length - 1;

        // Hide progress % text on intro and last panels
        if (progressText != null)
            progressText.gameObject.SetActive(!isIntro && !isLast);

        if (!isIntro && !isLast && steps != null && steps.Length >= 2) // Only update % in middle steps
        {
            int totalProgressSteps = Mathf.Max(steps.Length - 2, 1); // exclude intro & last
            int currentProgressStep = Mathf.Clamp(index - 1, 0, totalProgressSteps);
            float percent = ((float)currentProgressStep / totalProgressSteps) * 100f;

            progressText.text = $"{Mathf.RoundToInt(percent)}%";
        }

        // Disable Back button on intro
        if (backButton != null)
            backButton.interactable = !isIntro;

        // Disable Next button on last step
        if (nextButton != null)
            nextButton.interactable = !isLast;
    }

    public void SetStep(int i)
    {
        if (steps == null || steps.Length == 0) return;
        index = Mathf.Clamp(i, 0, steps.Length - 1);
        // call your existing private ApplyStep(int)
        var m = GetType().GetMethod("ApplyStep", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (m != null) m.Invoke(this, new object[] { index });
    }

    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.Space)) Next();
    //}
}
