using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class DetachCanvasAfterTracking : MonoBehaviour
{
    public List<GameObject> persistentSteps; // Drag Step7-10 here in Inspector
    private ObserverBehaviour observer;
    private bool hasTrackedOnce = false;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }

        foreach (GameObject step in persistentSteps)
        {
            step.SetActive(false);
        }
    }

    void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (!hasTrackedOnce && (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED))
        {
            hasTrackedOnce = true;

            foreach (GameObject step in persistentSteps)
            {
                step.transform.SetParent(null);     // Detach from target
                step.SetActive(true);               // Show it
            }
        }
    }
}
