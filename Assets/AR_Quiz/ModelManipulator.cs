using UnityEngine;
using UnityEngine.EventSystems;

public class ModelManipulator : MonoBehaviour
{
    public VuforiaImageSwipeSelector_ExistingChildren selector;
    public Camera arCamera;

    [Header("Rotate / Scale")]
    public bool enableRotate = true;
    public bool enableScale = true;
    public float rotateSpeed = 0.3f;  // degrees per pixel
    public float minScale = 0.2f;
    public float maxScale = 3.0f;

    private float _lastPinchDist;

    GameObject Target => selector ? selector.CurrentModelGO : null;

    void Start() { if (!arCamera) arCamera = Camera.main; }

    void Update()
    {
        if (!selector || !selector.IsSwipeLocked) return; // only after Choose
        var go = Target; if (!go) return;
        if (IsTouchOverUI()) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (enableRotate && Input.GetMouseButton(0))
        {
            float dx = Input.GetAxis("Mouse X");
            go.transform.Rotate(0f, -dx * rotateSpeed * 180f, 0f, Space.Self);
        }
        if (enableScale && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f) ApplyScale(go, 1f + scroll);
        }
#else
        if (Input.touchCount == 1 && enableRotate)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Moved)
            {
                float dx = t.deltaPosition.x;
                go.transform.Rotate(0f, -dx * rotateSpeed, 0f, Space.Self);
            }
        }
        else if (Input.touchCount == 2 && enableScale)
        {
            var t0 = Input.GetTouch(0);
            var t1 = Input.GetTouch(1);
            float d = Vector2.Distance(t0.position, t1.position);
            if (t1.phase == TouchPhase.Began || t0.phase == TouchPhase.Began) _lastPinchDist = d;
            else if (Mathf.Abs(_lastPinchDist) > 0.01f)
            {
                float scaleFactor = 1f + (d - _lastPinchDist) / 400f;
                ApplyScale(go, scaleFactor);
                _lastPinchDist = d;
            }
        }
#endif
    }

    void ApplyScale(GameObject go, float f)
    {
        var s = go.transform.localScale * Mathf.Clamp(f, 0.5f, 1.5f);
        float k = Mathf.Clamp(s.x, minScale, maxScale) / s.x;
        go.transform.localScale = s * k;
    }

    bool IsTouchOverUI()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current && EventSystem.current.IsPointerOverGameObject();
#else
        if (!EventSystem.current) return false;
        for (int i = 0; i < Input.touchCount; i++)
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(i).fingerId))
                return true;
        return false;
#endif
    }
}
