//using UnityEngine;

//public class KeyboardManipulator : MonoBehaviour
//{
//    private float initialDistance;
//    private Vector3 initialScale;
//    private bool isScaling = false;

//    private float rotationSpeed = 0.2f;

//    void Update()
//    {
//        if (Input.touchCount == 2)
//        {
//            Touch touchZero = Input.GetTouch(0);
//            Touch touchOne = Input.GetTouch(1);

//            if (!isScaling)
//            {
//                initialDistance = Vector2.Distance(touchZero.position, touchOne.position);
//                initialScale = transform.localScale;
//                isScaling = true;
//            }
//            else
//            {
//                float currentDistance = Vector2.Distance(touchZero.position, touchOne.position);
//                if (Mathf.Approximately(initialDistance, 0))
//                    return;

//                float factor = currentDistance / initialDistance;
//                transform.localScale = initialScale * factor;
//            }
//        }
//        else
//        {
//            isScaling = false;
//        }

//        if (Input.touchCount == 1)
//        {
//            Touch touch = Input.GetTouch(0);
//            if (touch.phase == TouchPhase.Moved)
//            {
//                float rotationY = touch.deltaPosition.x * rotationSpeed;
//                transform.Rotate(0, -rotationY, 0, Space.World);
//            }
//        }
//    }
//}

using UnityEngine;

public class KeyboardManipulator : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private bool isScaling = false;

    private float rotationSpeed = 0.2f;

    private Quaternion defaultRotation;
    private Vector3 defaultScale;

    void Start()
    {
        defaultRotation = transform.rotation;
        defaultScale = transform.localScale;
    }

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            if (!isScaling)
            {
                initialDistance = Vector2.Distance(touchZero.position, touchOne.position);
                initialScale = transform.localScale;
                isScaling = true;
            }
            else
            {
                float currentDistance = Vector2.Distance(touchZero.position, touchOne.position);
                if (Mathf.Approximately(initialDistance, 0)) return;

                float factor = currentDistance / initialDistance;
                transform.localScale = initialScale * factor;
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
                float rotationY = touch.deltaPosition.x * rotationSpeed;
                transform.Rotate(0, -rotationY, 0, Space.Self);
            }
        }
    }

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
