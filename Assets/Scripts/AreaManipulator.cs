using UnityEngine;

public class AreaManipulator : MonoBehaviour
{
    private Vector3 initialScale;
    private float initialDistance;
    private bool isScaling = false;

    private Vector3 initialPosition;
    private Vector2 initialTouchPosition;
    private bool isDragging = false;

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
                if (Mathf.Approximately(initialDistance, 0))
                    return;

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

            if (touch.phase == TouchPhase.Began)
            {
                initialTouchPosition = touch.position;
                initialPosition = transform.position;
                isDragging = true;
            }
            else if (touch.phase == TouchPhase.Moved && isDragging)
            {
                Vector2 delta = touch.position - initialTouchPosition;

                // Convert screen delta to world movement
                Vector3 worldDelta = new Vector3(delta.x, 0, delta.y) * 0.001f;

                transform.position = initialPosition + worldDelta;
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isDragging = false;
            }
        }
    }
}
