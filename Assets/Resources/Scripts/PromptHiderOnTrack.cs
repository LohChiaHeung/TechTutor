using UnityEngine;
using Vuforia;

public class PromptHiderOnTrack : MonoBehaviour
{
    public GameObject guidePrompt;  // GuidePointToWord

    private ObserverBehaviour observer;
    private bool isTracked = false;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        // Hide the guidePrompt initially
        if (guidePrompt != null)
            guidePrompt.SetActive(false);
    }

    private void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            Debug.Log("🟢 Target tracked. Hiding prompt.");
            if (guidePrompt != null)
                guidePrompt.SetActive(false);

            isTracked = true;
        }
    }
}
