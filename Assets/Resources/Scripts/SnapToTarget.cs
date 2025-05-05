using UnityEngine;

public class SnapToTarget : MonoBehaviour
{
    public Transform snapTarget;
    public float snapDistance = 0.1f;
    private bool isSnapped = false;

    void Update()
    {
        if (!isSnapped && Vector3.Distance(transform.position, snapTarget.position) < snapDistance)
        {
            transform.position = snapTarget.position;
            isSnapped = true;

            // Optional: lock further dragging
            if (TryGetComponent<Draggable>(out var drag))
                drag.enabled = false;

            // Optional: Play snap sound or animation
            Debug.Log($"{gameObject.name} snapped to {snapTarget.name}");
        }
    }
}
