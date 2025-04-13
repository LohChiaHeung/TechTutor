using UnityEngine;
using Vuforia;

public class HidePromptOnTracking : MonoBehaviour
{
    public GameObject rotatePromptUI;

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
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
            if (rotatePromptUI != null)
                rotatePromptUI.SetActive(false); // ✅ Hide when tracked
        }
    }
}

