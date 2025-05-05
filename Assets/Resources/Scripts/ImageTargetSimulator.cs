using UnityEngine;
using Vuforia;

public class ImageTargetVisibility : MonoBehaviour
{
    public GameObject arObject; // Your Cube

    private void OnEnable()
    {
        var observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged += OnStatusChanged;
        }
    }

    private void OnDisable()
    {
        var observer = GetComponent<ObserverBehaviour>();
        if (observer)
        {
            observer.OnTargetStatusChanged -= OnStatusChanged;
        }
    }

    private void OnStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
    {
        if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
        {
            arObject.SetActive(true);
        }
        else
        {
            arObject.SetActive(false);
        }
    }

    private void Start()
    {
#if UNITY_EDITOR
        arObject.SetActive(true); // Show object in Editor for layout preview
#else
        arObject.SetActive(false); // Hide until detected on phone
#endif
    }
}
