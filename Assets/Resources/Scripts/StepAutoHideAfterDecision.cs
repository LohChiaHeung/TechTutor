using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class StepAutoHideAfterDecision : MonoBehaviour
{
    public GameObject stepToHide; // Assign the instruction you want to hide (e.g., Step1)
    public float hideDelay = 1.5f; // Delay in seconds

    private ObserverBehaviour observer;
    private float timer = 0f;
    private bool isHidingStarted = false;

    void Start()
    {
        observer = GetComponent<ObserverBehaviour>();
    }

    void Update()
    {
        if (observer != null && observer.TargetStatus.Status == Status.TRACKED)
        {
            timer += Time.deltaTime;

            if (!isHidingStarted && timer >= hideDelay)
            {
                if (stepToHide != null)
                {
                    stepToHide.SetActive(false); // Hide the step
                }

                isHidingStarted = true;
            }
        }
    }
}
