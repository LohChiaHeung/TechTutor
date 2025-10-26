using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class BookSpawner : MonoBehaviour
{
    public GameObject bookPrefab;
    public ARAnchorManager anchorManager;
    public float distanceFromCamera = 1.5f;

    private bool hasSpawned = false;

    void Update()
    {
        if (hasSpawned || Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase == TouchPhase.Began)
        {
            // 📍 Spawn position: fixed in front of camera
            Vector3 forward = Camera.main.transform.forward;
            Vector3 spawnPos = Camera.main.transform.position + forward * distanceFromCamera;

            // Optional small downward adjustment (if too high)
            spawnPos.y -= 0.05f;

            // 📐 Rotation: face the user and stand upright
            Quaternion rotation = Quaternion.LookRotation(forward);
            rotation *= Quaternion.Euler(-90f, 0f, 0f); // Adjust for model orientation

            // 🔒 Create an anchor manually (no plane needed)
            GameObject anchorGO = new GameObject("BookAnchor");
            anchorGO.transform.position = spawnPos;
            anchorGO.transform.rotation = rotation;

            ARAnchor anchor = anchorGO.AddComponent<ARAnchor>();

            if (anchor != null)
            {
                Instantiate(bookPrefab, anchor.transform.position, rotation, anchor.transform);
                Debug.Log("[BookSpawner] ✅ Book anchored at fixed world position.");
                hasSpawned = true;
            }
            else
            {
                Debug.LogWarning("[BookSpawner] ❌ Failed to create manual anchor.");
            }
        }
    }
}
