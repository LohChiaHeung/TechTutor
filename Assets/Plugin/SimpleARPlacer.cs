using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SimpleARPlacer_UseSceneObject : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycaster;

    [Header("Object to place (already in scene, set Inactive)")]
    public GameObject objectInScene;   // drag "Desk Simulator" here

    [Header("Settings")]
    public bool placeOnce = true;      // if false, each tap moves it
    public float spawnScale = 0.2f;    // applied on first placement
    public bool deactivateOnStart = true; // safety: ensure it's hidden at start

    public ARPlaneManager planeManager;            // assign in Inspector
    public ARPointCloudManager pointCloudManager;  // optional, assign in Inspector
    private TrackableId _keptPlaneId;

    static readonly List<ARRaycastHit> hits = new();
    bool hasPlaced = false;

    void Start()
    {
        if (objectInScene == null)
        {
            Debug.LogError("[SimpleARPlacer] objectInScene is not assigned.");
            enabled = false;
            return;
        }

        if (deactivateOnStart) objectInScene.SetActive(false);
    }

    void Update()
    {
        if (placeOnce && hasPlaced) return;

        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);

        // 👇 Ignore touches that started on UI so buttons can click
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (touch.phase != TouchPhase.Began) return;

        if (!raycaster.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Debug.LogWarning("[SimpleARPlacer] ❌ No plane detected at tap position.");
            return;
        }

        var pose = hits[0].pose;
        _keptPlaneId = hits[0].trackableId;
        Debug.Log("[SimpleARPlacer] ✅ Plane hit at: " + pose.position);

        if (!hasPlaced)
        {
            hasPlaced = true;
            Debug.Log("[SimpleARPlacer] ✅ Scene object enabled & placed.");

            // first time: show and place the existing scene object
            objectInScene.transform.SetPositionAndRotation(pose.position + Vector3.up * 0.01f, pose.rotation);
            objectInScene.transform.localScale = Vector3.one * spawnScale;
            objectInScene.SetActive(true);

            if (placeOnce)
            {
                if (raycaster) raycaster.enabled = false; // stop ARFoundation raycasts
                enabled = false;                           // stop Update()
            }
            // after SetActive(true) on first placement:
            var reposition = objectInScene.GetComponent<DeskRepositionController>();
            if (!reposition) reposition = objectInScene.GetComponentInChildren<DeskRepositionController>(true);
            if (reposition)
            {
                // use the real Y we just placed at (includes your +0.01 lift), then subtract the offset
                float placedY = objectInScene.transform.position.y;
                reposition.SetBaselinePlaneY(placedY - reposition.planeYOffset);
            }


            var adjustPanel = objectInScene.GetComponentInChildren<DeskAdjustPanelController>(true);
            if (adjustPanel) adjustPanel.CaptureInitialPose();
            hasPlaced = true;
            var mover = objectInScene.GetComponentInChildren<DeskRepositionController>(true);
            if (mover) mover.CaptureInitialScales();
            Debug.Log("[SimpleARPlacer] ✅ Scene object enabled & placed.");
            FreezePlaneDetection();
        }
        else if (!placeOnce)
        {
            objectInScene.transform.SetPositionAndRotation(pose.position + Vector3.up * 0.01f, pose.rotation);
            Debug.Log("[SimpleARPlacer] 🔄 Object moved.");

            var reposition = objectInScene.GetComponent<DeskRepositionController>();
            if (!reposition) reposition = objectInScene.GetComponentInChildren<DeskRepositionController>(true);
            if (reposition)
            {
                float placedY = objectInScene.transform.position.y;
                reposition.SetBaselinePlaneY(placedY - reposition.planeYOffset);
            }

            var adjustPanel = objectInScene.GetComponentInChildren<DeskAdjustPanelController>(true);
            if (adjustPanel) adjustPanel.CaptureInitialPose();

        }
        else
        {
            Debug.Log("[SimpleARPlacer] ⚠️ Already placed and 'placeOnce' is true — tap ignored.");
        }
    }

    void FreezePlaneDetection()
    {
        if (planeManager)
        {
            // Stop creating/updating planes (AF 5.x); for older AF use: planeManager.detectionMode = PlaneDetectionMode.None;
            planeManager.requestedDetectionMode = PlaneDetectionMode.None;

            // Keep only the plane we placed on; hide the rest
            foreach (var p in planeManager.trackables)
                p.gameObject.SetActive(p.trackableId == _keptPlaneId);
            // Keep planeManager enabled so ARRaycastManager can still raycast the kept plane for dragging
        }

        if (pointCloudManager) // optional: hide feature points too
        {
            foreach (var pc in pointCloudManager.trackables)
                pc.gameObject.SetActive(false);
            pointCloudManager.enabled = false;
        }
    }


}
