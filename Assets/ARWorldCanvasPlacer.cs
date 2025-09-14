using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARWorldCanvasPlacer : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public ARRaycastManager raycaster;     // must be on AR Session Origin
    public Camera arCamera;                // your AR Camera
    public GameObject worldCanvasPrefab;   // your WorldCanvas_ARUI.prefab

    [Header("Placement")]
    public float placeDistance = 1.0f;     // meters from camera
    public bool faceCamera = true;

    static readonly List<ARRaycastHit> hits = new();

    GameObject _spawned;
    public AR_RedboxRunner runner; 
    void Awake()
    {
        // Try to auto-find if not set
        if (!raycaster)
        {
            raycaster = GetComponent<ARRaycastManager>();
            if (!raycaster)
                Debug.LogError("[Placer] Missing ARRaycastManager. Put this script on AR Session Origin and add ARRaycastManager.");
        }
        if (!arCamera)
        {
            arCamera = Camera.main;
            if (!arCamera)
                Debug.LogError("[Placer] Missing AR Camera reference. Assign your AR Camera.");
        }
        if (!worldCanvasPrefab)
        {
            Debug.LogWarning("[Placer] worldCanvasPrefab not assigned. Nothing will spawn until you set it.");
        }
    }

    void Update()
    {
        // Don’t run if something critical is missing
        if (!raycaster || !arCamera || !worldCanvasPrefab) return;

#if UNITY_EDITOR
        // Allow mouse click to test in Editor Game view
        if (Input.GetMouseButtonDown(0))
        {
            TryPlace(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
                TryPlace(t.position);
        }
#endif
    }

    void TryPlace(Vector2 screenPos)
    {
        if (!raycaster.Raycast(screenPos, hits, TrackableType.PlaneWithinPolygon))
            return;

        var pose = hits[0].pose;

        if (_spawned == null)
        {
            _spawned = Instantiate(worldCanvasPrefab, pose.position, pose.rotation);

            // push to fixed distance in front of camera for better scale feeling
            Vector3 camPos = arCamera.transform.position;
            Vector3 dir = (pose.position - camPos).normalized;
            _spawned.transform.position = camPos + dir * placeDistance;

            if (faceCamera)
                FaceFlatToCamera(_spawned.transform);
        }
        else
        {
            _spawned.transform.SetPositionAndRotation(pose.position, pose.rotation);
            if (faceCamera)
                FaceFlatToCamera(_spawned.transform);
        }
    }

    System.Collections.IEnumerator Co_BootRunnerNextFrame()
    {
        yield return null; // one frame
        runner.OnPlacedAndReady();
    }

    void FaceFlatToCamera(Transform t)
    {
        Vector3 camPos = arCamera.transform.position;
        Vector3 look = t.position - camPos;
        look.y = 0f; // yaw only
        if (look.sqrMagnitude > 0.0001f)
            t.rotation = Quaternion.LookRotation(look.normalized, Vector3.up);
    }
}
