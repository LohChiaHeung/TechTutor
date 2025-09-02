using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static TechTutorAskUI;

public class TechTutorARController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text stepText;      // Link to your canvas's TMP_Text
    public Button nextButton;
    public Button backButton;

    private TutorialSpec tutorial;
    private int currentIndex = 0;

    void Start()
    {
        // Load tutorial steps from holder
        if (TutorialSpecHolder.I == null || TutorialSpecHolder.I.spec == null)
        {
            Debug.LogError("TutorialSpec not found!");
            return;
        }

        tutorial = TutorialSpecHolder.I.spec;

        nextButton.onClick.AddListener(OnNext);
        backButton.onClick.AddListener(OnBack);

        ShowStep(currentIndex);
    }

    void ShowStep(int index)
    {
        if (index < 0 || index >= tutorial.steps.Count)
            return;

        currentIndex = index;
        var step = tutorial.steps[currentIndex];

        // ✅ Combine title and description manually
        stepText.text = $"{step.title}\n{step.description}";

        Debug.Log($"Showing Step {step.id}: {step.title} → {step.description}");

        // ✅ Check if this step involves a physical target
        if (!string.IsNullOrEmpty(step.target))
        {
            Debug.Log($"This step involves target: {step.target}");
            // (Optional) enter pin mode, highlight, etc.
        }

        // ✅ Enable/disable navigation buttons
        backButton.interactable = (currentIndex > 0);
        nextButton.interactable = (currentIndex < tutorial.steps.Count - 1);
    }


    void OnNext()
    {
        ShowStep(currentIndex + 1);
    }

    void OnBack()
    {
        ShowStep(currentIndex - 1);
    }
}
