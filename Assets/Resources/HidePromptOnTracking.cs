using UnityEngine;
using Vuforia;

public class HidePromptOnTracking : MonoBehaviour
{
    public GameObject rotatePromptUI;        // "Rotate your phone" canvas
    public GameObject instructionPromptUI;   // "Point your phone to calculator" canvas

    private ObserverBehaviour observer;
    private bool isTracked = false; // ✅ Lock once target is tracked

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        // Initial state
        if (rotatePromptUI) rotatePromptUI.SetActive(true);
        if (instructionPromptUI) instructionPromptUI.SetActive(false);
    }

    void Update()
    {
        if (rotatePromptUI == null || instructionPromptUI == null) return;

        // ✅ Don't show instructionPrompt again if already tracked
        if (isTracked) return;

        // Logic for showing prompt
        if (Screen.width > Screen.height)
        {
            rotatePromptUI.SetActive(false);
            instructionPromptUI.SetActive(true);
        }
        else
        {
            rotatePromptUI.SetActive(true);
            instructionPromptUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (observer)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            Debug.Log("Target tracked — hiding all prompts.");
            if (rotatePromptUI) rotatePromptUI.SetActive(false);
            if (instructionPromptUI) instructionPromptUI.SetActive(false);
            isTracked = true; // ✅ This prevents Update() from showing it again
        }
    }
}
