using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class PanelManipulator : MonoBehaviour
{
    public float dragLerp = 0.15f, rotateSpeed = 0.2f, scaleSpeed = 0.003f;
    public float minScale = 0.6f, maxScale = 1.8f;
    Vector3 targetPos; Quaternion targetRot; float targetScale; bool dragging; Vector3 dragOffset;

    void Start() { targetPos = transform.position; targetRot = transform.rotation; targetScale = transform.localScale.x; }
    void Update()
    {
        HandleTouch();
        transform.position = Vector3.Lerp(transform.position, targetPos, dragLerp);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 0.15f);
        float s = Mathf.Lerp(transform.localScale.x, targetScale, 0.2f);
        transform.localScale = Vector3.one * s;
    }
    void HandleTouch()
    {
        if (Input.touchCount == 1)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && RaycastPanel(t.position, out var hit)) { dragging = true; dragOffset = targetPos - hit.point; }
            else if (t.phase == TouchPhase.Moved && dragging && RaycastToPlane(t.position, out var p)) { targetPos = p + dragOffset; }
            else if (t.phase == TouchPhase.Ended) { dragging = false; }
        }
        else if (Input.touchCount == 2)
        {
            var t0 = Input.GetTouch(0); var t1 = Input.GetTouch(1);
            Vector2 prevDir = (t0.position - t0.deltaPosition) - (t1.position - t1.deltaPosition);
            Vector2 currDir = t0.position - t1.position;
            float ang = Vector2.SignedAngle(prevDir, currDir);
            targetRot = Quaternion.AngleAxis(ang * rotateSpeed, Vector3.up) * targetRot;
            float prevDist = prevDir.magnitude, currDist = currDir.magnitude;
            targetScale = Mathf.Clamp(targetScale + (currDist - prevDist) * scaleSpeed, minScale, maxScale);
        }
    }
    bool RaycastPanel(Vector2 screen, out RaycastHit hit) { var ray = Camera.main.ScreenPointToRay(screen); return Physics.Raycast(ray, out hit, 20f); }
    bool RaycastToPlane(Vector2 screen, out Vector3 point)
    {
        var ray = Camera.main.ScreenPointToRay(screen);
        var plane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));
        if (plane.Raycast(ray, out float enter)) { point = ray.GetPoint(enter); return true; }
        point = default; return false;
    }
}
