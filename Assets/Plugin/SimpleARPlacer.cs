using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class SimpleARPlacer : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycaster;

    [Header("Prefab to place")]
    public GameObject prefabToPlace;   // e.g. your ComputerEnvironment (monitor+keyboard+mouse)

    [Header("Settings")]
    public bool placeOnce = true;      // if false, each tap moves it
    public float spawnScale = 0.2f;    // << Scale factor (1 = original size, 0.5 = half size, etc.)

    static readonly List<ARRaycastHit> hits = new();
    GameObject placed;


    void Update()
    {
        if (Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began) return;

        Debug.Log("[SimpleARPlacer] Touch detected at: " + touch.position);

        if (raycaster.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            var pose = hits[0].pose;
            Debug.Log("[SimpleARPlacer] ✅ Plane hit detected at: " + pose.position);

            if (placed == null)
            {
                placed = Instantiate(prefabToPlace, pose.position, pose.rotation);

                // Set scale smaller
                placed.transform.localScale = Vector3.one * spawnScale;

                // Nudge slightly up to avoid z-fighting
                placed.transform.position += Vector3.up * 0.01f;

                Debug.Log("[SimpleARPlacer] ✅ Prefab instantiated successfully at: " + placed.transform.position +
                          " with scale: " + placed.transform.localScale);
            }
            else if (!placeOnce)
            {
                placed.transform.SetPositionAndRotation(pose.position, pose.rotation);
                Debug.Log("[SimpleARPlacer] 🔄 Prefab moved to: " + placed.transform.position);
            }
            else
            {
                Debug.Log("[SimpleARPlacer] ⚠️ Prefab already placed, 'placeOnce' is true — tap ignored.");
            }
        }
        else
        {
            Debug.LogWarning("[SimpleARPlacer] ❌ No plane detected at tap position.");
        }
    }
}
