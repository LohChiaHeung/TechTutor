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

    void Start()
    {
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[TutorialStepManager] No steps assigned.");
            return;
        }
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

    void ApplyStep(int i)
    {
        var s = steps[i];

        GameObject activePanel = null;
        if (canvasPanels && s.panelIndex >= 0)
        {
            canvasPanels.ShowPanel(s.panelIndex);
            if (canvasPanels.panels != null &&
                s.panelIndex >= 0 && s.panelIndex < canvasPanels.panels.Count)
            {
                activePanel = canvasPanels.panels[s.panelIndex];
            }
        }

        if (screen && s.screenMaterialIndex >= 0)
            screen.SetScreen(s.screenMaterialIndex);

        if (visibility)
            visibility.ShowOnly(s.objectsToShow);

        UpdateProgressUI();

        // 🔊 Speak (persistent cache to disk)
        if (narrator && activePanel)
        {
            var pn = activePanel.GetComponent<PanelNarration>();
            if (pn != null)
            {
                StartCoroutine(SpeakAfterFrame(pn));
            }
        }

        Debug.Log($"[TutorialStepManager] Step {i + 1}/{steps.Length} applied.");
    }

    System.Collections.IEnumerator SpeakAfterFrame(PanelNarration pn)
    {
        yield return null; // small delay helps during panel transitions
        narrator.SpeakFromPanel(pn);
    }


    void UpdateProgressUI()
    {
        bool isIntro = index == 0;
        bool isLast = index == steps.Length - 1;

        // Hide progress % text on intro and last panels
        if (progressText != null)
            progressText.gameObject.SetActive(!isIntro && !isLast);

        if (!isIntro && !isLast) // Only update % in middle steps
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



    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) Next();
    }
}
