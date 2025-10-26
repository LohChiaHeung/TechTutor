using UnityEngine;

public class MouseManipulator : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private bool isScaling = false;

    public float rotationSpeed = 0.1f;
    public float minScale = 0.05f;
    public float maxScale = 0.2f;

    private Quaternion defaultRotation;
    private Vector3 defaultScale;
    private Vector3 defaultPosition;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultScale = transform.localScale;
        defaultPosition = transform.position;
    }

    void Update()
    {
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

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved)
            {
                float delta = touch.deltaPosition.x;
                transform.Rotate(0f, 0f, -delta * rotationSpeed, Space.Self);
            }
        }
    }
    public void RotateLeft()
    {
        transform.Rotate(0f, 0f, -10f, Space.Self); //z, x ,y
    }

    public void RotateRight()
    {
        transform.Rotate(0f, 0f, 10f, Space.Self);
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
