using UnityEngine;

public class LaptopManipulator : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private bool isScaling = false;

    private Quaternion defaultRotation;
    private Vector3 defaultScale;
    private Vector3 defaultPosition;

    public float rotationSpeed = 0.2f;
    public float minScale = 0.1f;
    public float maxScale = 0.3f;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultScale = transform.localScale;
        defaultPosition = transform.position;
    }

    void Update()
    {
        // 🔍 Two-finger pinch to scale
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (!isScaling)
            {
                initialDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = transform.localScale;
                isScaling = true;
            }
            else
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                if (Mathf.Approximately(initialDistance, 0)) return;

                float scaleFactor = currentDistance / initialDistance;
                Vector3 newScale = initialScale * scaleFactor;
                newScale = Vector3.Max(Vector3.one * minScale, Vector3.Min(newScale, Vector3.one * maxScale));
                transform.localScale = newScale;
            }
        }
        else
        {
            isScaling = false;
        }

        // 👉 One-finger swipe to rotate freely
        if (Input.touchCount == 1 && !isScaling)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float deltaX = touch.deltaPosition.x * rotationSpeed;
                float deltaY = touch.deltaPosition.y * rotationSpeed;

                transform.Rotate(deltaY, -deltaX, 0f, Space.Self); // Local rotation
            }
        }
    }

    // 🎮 Button controls
    public void RotateLeft()
    {
        transform.Rotate(0f, -10f, 0f, Space.Self);
    }

    public void RotateRight()
    {
        transform.Rotate(0f, 10f, 0f, Space.Self);
    }

    public void MoveUp()
    {
        transform.position += Vector3.up * 0.05f;
    }

    public void MoveDown()
    {
        transform.position += Vector3.down * 0.05f;
    }
    public void ResetModelTransform()
    {
        transform.rotation = defaultRotation;
        transform.localScale = defaultScale;
        transform.position = defaultPosition;
    }
}
