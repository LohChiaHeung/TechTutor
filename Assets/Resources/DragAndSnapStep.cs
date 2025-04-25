using UnityEngine;

public class DragAndSnapStep : MonoBehaviour
{
    [Header("Snap Settings")]
    public Transform snapTarget;
    [SerializeField] private float snapThreshold = 0.2f;
    [SerializeField] private float verticalOffset = 0.01f;

    [Header("Visual Feedback")]
    public GameObject highlight;
    public GameObject correctMessage;

    private Camera cam;
    private Plane dragPlane;
    private Vector3 offset;
    private bool isSnapped = false;

    void Start()
    {
        cam = Camera.main;

        if (highlight) highlight.SetActive(true);
        if (correctMessage) correctMessage.SetActive(false);
        if (snapTarget == null)
            Debug.LogError("Snap Target is not assigned!");
    }

    void OnMouseDown()
    {
        if (isSnapped || cam == null) return;

        dragPlane = new Plane(Vector3.up, transform.position);
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float dist))
        {
            offset = transform.position - ray.GetPoint(dist);
        }
    }

    void OnMouseDrag()
    {
        if (isSnapped || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (dragPlane.Raycast(ray, out float dist))
        {
            Vector3 newPos = ray.GetPoint(dist) + offset;
            transform.position = newPos;

            if (Vector3.Distance(newPos, snapTarget.position) < snapThreshold)
            {
                SnapToTarget();
            }
        }
    }

    void SnapToTarget()
    {
        transform.SetParent(null); // Clear parent

        Vector3 finalPos = snapTarget.position + snapTarget.up * 0.01f;
        transform.position = finalPos;
        transform.rotation = snapTarget.rotation;

        Debug.Log("Snapped to: " + finalPos);

        isSnapped = true;
        enabled = false;

        if (highlight) highlight.SetActive(false);
        if (correctMessage) correctMessage.SetActive(true);
    }

}
