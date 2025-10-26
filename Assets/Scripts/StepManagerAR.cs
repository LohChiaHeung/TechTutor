//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class StepManagerAR : MonoBehaviour
//{
//    public GameObject[] steps; // Drag Step1–Step12 objects here
//    public TextMeshProUGUI stepText;
//    public Button nextButton;
//    public Button prevButton;

//    private int currentStep = 0;

//    public void BeginTutorial()
//    {
//        currentStep = 0;
//        ShowStep(currentStep);
//        nextButton.onClick.AddListener(NextStep);
//        prevButton.onClick.AddListener(PreviousStep);
//    }

//    void ShowStep(int index)
//    {
//        for (int i = 0; i < steps.Length; i++)
//            steps[i].SetActive(i == index);

//        stepText.text = $"Step {index + 1} / {steps.Length}";
//        prevButton.interactable = (index > 0);
//        nextButton.interactable = (index < steps.Length - 1);
//    }

//    void NextStep()
//    {
//        if (currentStep < steps.Length - 1)
//        {
//            currentStep++;
//            ShowStep(currentStep);
//        }
//    }

//    void PreviousStep()
//    {
//        if (currentStep > 0)
//        {
//            currentStep--;
//            ShowStep(currentStep);
//        }
//    }
//}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StepManagerAR : MonoBehaviour
{
    public GameObject[] steps; // Drag Step1–Step12 objects here
    public TextMeshProUGUI stepText;
    public Button nextButton;
    public Button prevButton;
    public Button homeButton; // 👈 new button (drag from Canvas)

    private int currentStep = 0;

    public void BeginTutorial()
    {
        currentStep = 0;
        ShowStep(currentStep);

        nextButton.onClick.AddListener(NextStep);
        prevButton.onClick.AddListener(PreviousStep);
        homeButton.onClick.AddListener(OnHomeButtonClicked);

        homeButton.gameObject.SetActive(false); // hide home initially
    }

    void ShowStep(int index)
    {
        for (int i = 0; i < steps.Length; i++)
            steps[i].SetActive(i == index);

        stepText.text = $"Step {index + 1} / {steps.Length}";
        prevButton.interactable = (index > 0);

        // Handle last step button change
        bool isLast = (index == steps.Length - 1);
        nextButton.gameObject.SetActive(!isLast);
        homeButton.gameObject.SetActive(isLast);
    }

    void NextStep()
    {
        if (currentStep < steps.Length - 1)
        {
            currentStep++;
            ShowStep(currentStep);
        }
    }

    void PreviousStep()
    {
        if (currentStep > 0)
        {
            currentStep--;
            ShowStep(currentStep);
        }
    }

    void OnHomeButtonClicked()
    {
        Debug.Log("[StepManagerAR] 🏠 Home button clicked.");

        // TODO: Replace this with your own logic
        // For example, load a scene:
        // SceneManager.LoadScene("MainMenu");

        // Or close the tutorial panel:
        this.gameObject.SetActive(false);
    }
}
