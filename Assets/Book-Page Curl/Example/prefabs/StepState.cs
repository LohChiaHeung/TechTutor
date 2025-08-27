using UnityEngine;
using UnityEngine.Events;

public class StepState : MonoBehaviour
{
    public static StepState I { get; private set; }
    [System.Serializable] public class IntEvent : UnityEvent<int> { }
    public IntEvent OnStepChanged = new IntEvent();
    [SerializeField] int currentStep = 0;

    void Awake() { if (I && I != this) { Destroy(gameObject); return; } I = this; DontDestroyOnLoad(gameObject); }

    public int CurrentStep
    {
        get => currentStep;
        set
        {
            if (value == currentStep) return;
            currentStep = value;

            // DEBUG: who changed me?
            Debug.Log($"[StepState] -> {currentStep}\n{System.Environment.StackTrace}");

            OnStepChanged.Invoke(currentStep);
        }
    }

    public void NextStep() => CurrentStep = currentStep + 1;
    public void PrevStep() => CurrentStep = Mathf.Max(0, currentStep - 1);
}
