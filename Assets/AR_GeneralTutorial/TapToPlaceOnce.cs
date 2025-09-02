using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class TapToPlaceOnce : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public Transform contentRoot; // AR_TutorRoot
    bool placed; static List<ARRaycastHit> hits = new();

    void Update()
    {
        if (placed || Input.touchCount == 0) return;
        var t = Input.GetTouch(0); if (t.phase != TouchPhase.Began) return;
        if (raycastManager.Raycast(t.position, hits, TrackableType.PlaneWithinPolygon))
        {
            var pose = hits[0].pose;
            contentRoot.position = pose.position;
            Vector3 f = Camera.main.transform.forward; f.y = 0; f.Normalize();
            contentRoot.rotation = Quaternion.LookRotation(f);
            placed = true;
        }
    }
}
