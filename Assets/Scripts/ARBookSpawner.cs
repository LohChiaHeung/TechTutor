using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class ARBookSpawner : MonoBehaviour
{
    [Header("AR Managers")]
    public ARRaycastManager raycastManager;

    [Header("Book Prefab")]
    public GameObject bookPrefab;

    [Header("Book Scale")]
    public float bookScale = 0.01f;

    private GameObject bookInstance;
    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool bookSpawned = false;

    void Update()
    {
        if (bookSpawned || Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began)
            return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            // Instantiate the book
            bookInstance = Instantiate(bookPrefab, hitPose.position, Quaternion.identity);

            // Face the book toward the user
            Vector3 lookDirection = Camera.main.transform.forward;
            lookDirection.y = 0; // Keep upright
            bookInstance.transform.rotation = Quaternion.LookRotation(-lookDirection);

            // Apply scale
            bookInstance.transform.localScale = Vector3.one * bookScale;

            // Assign world camera to canvas
            Canvas canvas = bookInstance.GetComponentInChildren<Canvas>();
            if (canvas != null && canvas.renderMode == RenderMode.WorldSpace)
            {
                canvas.worldCamera = Camera.main;
            }

            bookSpawned = true;
            Debug.Log("[ARBookSpawner] Book placed at: " + hitPose.position);
        }
    }
}
