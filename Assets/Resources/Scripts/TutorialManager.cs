using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject inputPanel;
    public GameObject stepPanel;

    [Header("Input Elements")]
    public TMP_InputField topicInputField;
    public Button generateButton;

    [Header("Step UI Elements")]
    public TextMeshProUGUI stepTitleText;
    public TextMeshProUGUI stepDescriptionText;
    public Button nextButton;
    public Button backButton;

    private GPTConnector gpt;
    private TutorialList currentTutorial;
    private int currentStep = 0;

    void Start()
    {
        gpt = GetComponent<GPTConnector>();
        generateButton.onClick.AddListener(OnGenerateClicked);
        nextButton.onClick.AddListener(NextStep);
        backButton.onClick.AddListener(PreviousStep);

        // Set initial states
        inputPanel.SetActive(true);
        stepPanel.SetActive(false);
    }

    void OnGenerateClicked()
    {
        string topic = topicInputField.text.Trim();
        if (string.IsNullOrEmpty(topic))
        {
            Debug.LogWarning("⚠️ Topic input is empty!");
            return;
        }

        string prompt = $"You are an educational assistant for an AR app. Generate a beginner-friendly step-by-step tutorial on the topic: '{topic}'. " +
                        "Each step must include a short title and a clear, detailed description. " +
                        "Output as a JSON array: [{\"title\": \"Step 1 – ...\", \"description\": \"...\"}, ...]";

        StartCoroutine(gpt.SendPrompt(prompt, OnGPTResponse));
    }

    void OnGPTResponse(string rawJsonText)
    {
        try
        {
            string cleaned = ExtractJsonArray(rawJsonText);
            currentTutorial = JsonUtility.FromJson<TutorialList>("{\"steps\":" + cleaned + "}");
            currentStep = 0;
            inputPanel.SetActive(false);
            stepPanel.SetActive(true);
            ShowStep(currentStep);
        }
        catch
        {
            Debug.LogError("❌ Failed to parse GPT JSON.");
        }
    }

    private string ExtractJsonArray(string raw)
    {
        int start = raw.IndexOf("[");
        int end = raw.LastIndexOf("]");

        if (start != -1 && end != -1 && end > start)
        {
            string jsonArray = raw.Substring(start, end - start + 1);
            return jsonArray;
        }

        Debug.LogWarning("⚠️ Could not extract JSON array from GPT response.");
        return "[]";
    }


    void ShowStep(int index)
    {
        if (currentTutorial == null || index < 0 || index >= currentTutorial.steps.Length)
            return;

        stepTitleText.text = currentTutorial.steps[index].title;
        stepDescriptionText.text = currentTutorial.steps[index].description;

        backButton.interactable = (index > 0);
        nextButton.interactable = (index < currentTutorial.steps.Length - 1);
    }

    void NextStep()
    {
        if (currentStep < currentTutorial.steps.Length - 1)
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
}
