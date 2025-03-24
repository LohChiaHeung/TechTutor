using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

public class ARStepManager : MonoBehaviour
{
    public GameObject[] steps;                // Assign your tutorial steps here
    public GameObject promptForNextTarget;    // Assign the “Now point your phone…” prompt
    private int currentStep = 0;
    private bool showingPrompt = false;

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
    }

    public int GetCurrentStep()
    {
        return currentStep;
    }

    private void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
        {
            steps[i].SetActive(i == index);
        }

        if (promptForNextTarget != null)
        {
            promptForNextTarget.SetActive(false);
            showingPrompt = false;
        }
    }

    public void ReturnToMainMenu()
    {
        //VuforiaApplication.Instance.Deinit();  
        SceneManager.LoadScene("MainMenu"); 
    }
}