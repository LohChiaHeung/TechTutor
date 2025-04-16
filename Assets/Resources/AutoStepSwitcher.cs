using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;

public class AutoStepSwitcher : MonoBehaviour
{
    public GameObject stepToHide;    // Current step (UI or canvas element)
    public GameObject stepToShow;    // Next step to show (UI or canvas element)

    private ObserverBehaviour observer;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
        if (observer != null)
        {
            observer.OnTargetStatusChanged += OnTargetStatusChanged;
        }
    }

    private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED) 
        {
            if (stepToHide != null) stepToHide.SetActive(false);
            if (stepToShow != null) stepToShow.SetActive(true);
        }
    }

    void OnDestroy()
    {
        if (observer != null)
        {
            observer.OnTargetStatusChanged -= OnTargetStatusChanged;
        }
    }
}