//using UnityEngine;

//public class LaptopManipulator : MonoBehaviour
//{
//    private float initialDistance;
//    private Vector3 initialScale;
//    private bool isScaling = false;

//    private float rotationSpeed = 0.2f;

//    private Quaternion defaultRotation;
//    private Vector3 defaultScale;

//    void Start()
//    {
//        defaultRotation = transform.rotation;
//        defaultScale = transform.localScale;
//    }

//    void Update()
//    {
//        // Pinch to scale
//        if (Input.touchCount == 2)
//        {
//            Touch touch0 = Input.GetTouch(0);
//            Touch touch1 = Input.GetTouch(1);

//            if (!isScaling)
//            {
//                initialDistance = Vector2.Distance(touch0.position, touch1.position);
//                initialScale = transform.localScale;
//                isScaling = true;
//            }
//            else
//            {
//                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
//                if (Mathf.Approximately(initialDistance, 0)) return;

//                float scaleFactor = currentDistance / initialDistance;
//                transform.localScale = initialScale * scaleFactor;
//            }
//        }
//        else
//        {
//            isScaling = false;
//        }

//        // One-finger drag to rotate
//        if (Input.touchCount == 1)
//        {
//            Touch touch = Input.GetTouch(0);
//            if (touch.phase == TouchPhase.Moved)
//            {
//                float rotationY = touch.deltaPosition.x * rotationSpeed;
//                transform.Rotate(0, -rotationY, 0, Space.Self);
//            }
//        }
//    }

//    public void RotateLeft() => transform.Rotate(0f, -10f, 0f, Space.Self);
//    public void RotateRight() => transform.Rotate(0f, 10f, 0f, Space.Self);
//    public void ResetModelTransform()
//    {
//        transform.rotation = defaultRotation;
//        transform.localScale = defaultScale;
//    }
//}

using UnityEngine;

public class LaptopManipulator : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private bool isScaling = false;

    private Quaternion defaultRotation;
    private Vector3 defaultScale;

    public float rotationSpeed = 0.2f;
    public float minScale = 0.1f;
    public float maxScale = 0.3f;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultScale = transform.localScale;
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

    public void ResetModelTransform()
    {
        transform.rotation = defaultRotation;
        transform.localScale = defaultScale;
    }
}
