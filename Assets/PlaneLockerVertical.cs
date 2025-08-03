using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneLockerVertical : MonoBehaviour
{
    [Header("AR Managers")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Tutorial Prefab (Should be Inactive in Inspector)")]
    public GameObject gmailTutorialPanelPrefab;

    [Header("Debug")]
    public bool autoFaceCamera = true;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool panelPlaced = false;

    private GameObject tutorialPanelInstance;

    void Start()
    {
        if (planeManager != null)
        {
            planeManager.detectionMode = PlaneDetectionMode.Vertical;
        }

        // ✅ Make sure prefab is deactivated at start
        if (gmailTutorialPanelPrefab != null)
        {
            gmailTutorialPanelPrefab.SetActive(false);
        }
    }

    void Update()
    {
        if (panelPlaced) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            var hit = hits[0];
            Pose hitPose = hit.pose;

            Quaternion spawnRotation = autoFaceCamera
                ? Quaternion.LookRotation(-Camera.main.transform.forward)
                : Quaternion.LookRotation(-hitPose.forward);

            // ✅ Instantiate and activate the panel
            tutorialPanelInstance = Instantiate(
                gmailTutorialPanelPrefab,
                hitPose.position,
                spawnRotation
            );

            tutorialPanelInstance.transform.localScale = Vector3.one * 0.01f;
            tutorialPanelInstance.SetActive(true); // ✅ Make it visible now

            panelPlaced = true;

            planeManager.enabled = false;
            HideAllPlanes();

            Debug.Log("[PlaneLockerVertical] Gmail tutorial panel placed and activated.");
        }
    }

    void HideAllPlanes()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
    }
}
