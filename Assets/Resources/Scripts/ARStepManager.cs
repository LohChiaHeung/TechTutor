using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

public class ARStepManager : MonoBehaviour
{
    public GameObject[] steps;                // Assign your tutorial steps here
    public GameObject promptForNextTarget;    // Assign the “Now point your phone…” prompt
    private int currentStep = 0;
    private bool showingPrompt = false;
    public TMPro.TextMeshProUGUI progressText;

    void Start()
    {
        ShowStep(currentStep);
    }

    public void NextStep()
    {
        // If last step reached, show prompt
        if (currentStep == steps.Length - 1 && promptForNextTarget != null)
        {
            steps[currentStep].SetActive(false);
            promptForNextTarget.SetActive(true);
            showingPrompt = true;
            return;
        }

        if (currentStep < steps.Length - 1)
        {
            steps[currentStep].SetActive(false);
            currentStep++;
            ShowStep(currentStep);
        }

        return;
    }

    public void BackStep()
    {
        // If the prompt is currently showing, go back to the last tutorial step
        if (showingPrompt)
        {
            promptForNextTarget.SetActive(false);
            steps[currentStep].SetActive(true);
            showingPrompt = false;
            return;
        }

        if (currentStep > 0)
        {
            steps[currentStep].SetActive(false);
            currentStep--;
            ShowStep(currentStep);
        }

        return;
    }

    public void SetStep(int index)
    {
        if (index >= 0 && index < steps.Length)
        {
            currentStep = index;
            ShowStep(currentStep);
        }
        else
        {
            Debug.LogWarning($"[ARStepManager] Invalid step index: {index}");
        }
    }

    public int GetCurrentStep()
    {
        return currentStep;
    }

    //private void ShowStep(int index)
    //{
    //    for (int i = 0; i < steps.Length; i++)
    //        steps[i].SetActive(i == index);

    //    if (promptForNextTarget != null)
    //    {
    //        promptForNextTarget.SetActive(false);
    //        showingPrompt = false;
    //    }

    //    // ✅ Show % only between step 1 and 15
    //    if (progressText != null)
    //    {
    //        if (index >= 1 && index <= 15)
    //        {
    //            int totalSteps = 15; // steps 1 to 15
    //            float percent = ((float)index / totalSteps) * 100f;
    //            progressText.text = $"{Mathf.Clamp(Mathf.RoundToInt(percent), 0, 100)}%";
    //            progressText.gameObject.SetActive(true);
    //        }
    //        else
    //        {
    //            // Hide on welcome (0) and final (16)
    //            progressText.gameObject.SetActive(false);
    //        }
    //    }
    //}

    private void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i].SetActive(i == index);

        if (promptForNextTarget != null)
        {
            promptForNextTarget.SetActive(false);
            showingPrompt = false;
        }

        // ✅ Show % dynamically based on step count
        if (progressText != null)
        {
            // Only show progress between first and last content step (not Welcome or Final)
            int firstContentStep = 1;
            int lastContentStep = steps.Length - 2; // Exclude welcome (0) and final (steps.Length - 1)

            if (index >= firstContentStep && index <= lastContentStep)
            {
                int totalContentSteps = lastContentStep - firstContentStep + 1;
                int currentContentStep = index - firstContentStep + 1;

                float percent = ((float)currentContentStep / totalContentSteps) * 100f;
                progressText.text = $"{Mathf.Clamp(Mathf.RoundToInt(percent), 0, 100)}%";
                progressText.gameObject.SetActive(true);
            }
            else
            {
                // Hide on welcome or final steps
                progressText.gameObject.SetActive(false);
            }
        }
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene("ARLearn_Scene");
    }


}