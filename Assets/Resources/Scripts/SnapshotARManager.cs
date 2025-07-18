using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using System.Security.Cryptography;

public class SnapshotARManager : MonoBehaviour
{
    public ARPlaneManager planeManager;
    public GameObject quadPrefab;

    private bool placed = false;

    void Update()
    {
        var yolo = FindObjectOfType<YoloCamerasTest>();
        if (!placed && yolo != null && yolo.snapshotTaken)
        {
            foreach (var plane in planeManager.trackables)
            {
                PlaceSnapshotOnPlane(yolo, plane);
                placed = true;
                break;  // only want the first one
            }
        }
    }


    void PlaceSnapshotOnPlane(YoloCamerasTest yolo, ARPlane plane)
    {
        // Use the plane’s transform for pose
        Vector3 position = plane.transform.position;
        Quaternion rotation = plane.transform.rotation;

        var quad = Instantiate(quadPrefab, position, rotation);
        quad.GetComponent<MeshRenderer>().material.mainTexture = yolo.snapshotTexture;

        var overlay = quad.AddComponent<QuadYoloOverlay>();
        overlay.Init(yolo.snapshotDetections, 640, 640);
    }
}
