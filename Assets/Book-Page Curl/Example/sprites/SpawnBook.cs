//using System.Collections;
//using UnityEngine;
//using UnityEngine.XR.ARFoundation;

//public class SpawnBook : MonoBehaviour
//{
//    [Header("World-Space Book Panel Prefab")]
//    public GameObject bookPanelPrefab;

//    [Header("Distance in front of camera")]
//    public float placementDistance = 0.75f;

//    [Header("Fixed canvas size in meters")]
//    public float fixedWidth = 0.63f;
//    public float fixedHeight = 1.20f;
//    public float scaleFactor = 0.001f;

//    private GameObject spawnedBookInstance;
//    private bool panelPlaced = false;

//    void Update()
//    {
//        if (panelPlaced || Input.touchCount != 1 || Input.GetTouch(0).phase != TouchPhase.Began)
//            return;

//        Vector3 cameraForward = Camera.main.transform.forward;
//        Vector3 spawnPos = Camera.main.transform.position + cameraForward * placementDistance;
//        //Quaternion faceCamera = Quaternion.LookRotation(-cameraForward);
//        Quaternion faceCamera = Quaternion.Euler(0f, 180f, 0f); // Make the canvas face the camera

//        //Vector3 finalScale = new Vector3(fixedWidth, fixedHeight, 0.05f) * scaleFactor;
//        Vector3 finalScale = Vector3.one * 0.05f; // uniform scale

//        Debug.Log("─────────────────────────────────────────────");
//        Debug.Log("[SpawnBook] 📌 Spawning fixed-size book panel");
//        Debug.Log($"[SpawnBook] 📏 Width: {fixedWidth}m, Height: {fixedHeight}m, ScaleFactor: {scaleFactor}");
//        Debug.Log($"[SpawnBook] 📦 Final Scale: {finalScale}");
//        Debug.Log($"[SpawnBook] 📍 Position: {spawnPos}");
//        Debug.Log("─────────────────────────────────────────────");

//        ARAnchorManager anchorManager = FindObjectOfType<ARAnchorManager>();
//        if (anchorManager != null)
//        {
//            Pose pose = new Pose(spawnPos, faceCamera);
//            ARAnchor anchor = anchorManager.AddAnchor(pose);

//            if (anchor != null)
//            {
//                spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, faceCamera);
//                spawnedBookInstance.transform.SetParent(anchor.transform, true);
//            }
//            else
//            {
//                Debug.LogWarning("[SpawnBook] ❌ Failed to create ARAnchor.");
//                spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, faceCamera);
//            }
//        }
//        else
//        {
//            Debug.LogWarning("[SpawnBook] ❌ ARAnchorManager not found. Using fallback direct placement.");
//            spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, faceCamera);
//            spawnedBookInstance.SetActive(true);

//        }

//        spawnedBookInstance.transform.localScale = finalScale;
//        spawnedBookInstance.SetActive(true);

//        panelPlaced = true;
//    }

//    public void ResetPanel()
//    {
//        if (spawnedBookInstance != null)
//        {
//            Destroy(spawnedBookInstance);
//            Debug.Log("[SpawnBook] 🗑️ Destroyed existing book panel.");
//        }
//        panelPlaced = false;
//    }
//}


using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class SpawnBook : MonoBehaviour
{
    [Header("World-Space Book Panel Prefab")]
    public GameObject bookPanelPrefab;

    [Header("Distance in front of camera")]
    public float placementDistance = 0.75f;

    [Header("Fixed canvas size in meters")]
    public float fixedWidth = 0.63f;
    public float fixedHeight = 1.20f;
    public float scaleFactor = 0.001f;

    private GameObject spawnedBookInstance;
    private bool panelPlaced = false;

    void Update()
    {
        if (panelPlaced || Input.touchCount != 1 || Input.GetTouch(0).phase != TouchPhase.Began)
            return;

        Vector3 cameraForward = Camera.main.transform.forward; 
        Vector3 spawnPos = Camera.main.transform.position + cameraForward * placementDistance;
        spawnPos.y -= 0.1f;
        //Quaternion faceCamera = Quaternion.LookRotation(-cameraForward);
        Quaternion customRotation = Quaternion.Euler(0f, 0f, 0f); // 👈 Adjust X, Y, Z here as you like


        Vector3 finalScale = new Vector3(fixedWidth, fixedHeight, 0.01f) * scaleFactor;

        Debug.Log("─────────────────────────────────────────────");
        Debug.Log("[SpawnBook] 📌 Spawning fixed-size book panel");
        Debug.Log($"[SpawnBook] 📏 Width: {fixedWidth}m, Height: {fixedHeight}m, ScaleFactor: {scaleFactor}");
        Debug.Log($"[SpawnBook] 📦 Final Scale: {finalScale}");
        Debug.Log($"[SpawnBook] 📍 Position: {spawnPos}");
        Debug.Log("─────────────────────────────────────────────");

        ARAnchorManager anchorManager = FindObjectOfType<ARAnchorManager>();
        if (anchorManager != null)
        {
            Pose pose = new Pose(spawnPos, customRotation);
            ARAnchor anchor = anchorManager.AddAnchor(pose);

            if (anchor != null)
            {
                spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, customRotation);
                spawnedBookInstance.transform.SetParent(anchor.transform, true);
            }
            else
            {
                Debug.LogWarning("[SpawnBook] ❌ Failed to create ARAnchor.");
                spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, customRotation);
            }
        }
        else
        {
            Debug.LogWarning("[SpawnBook] ❌ ARAnchorManager not found. Using fallback direct placement.");
            spawnedBookInstance = Instantiate(bookPanelPrefab, spawnPos, customRotation);
            spawnedBookInstance.SetActive(true);

        }

        spawnedBookInstance.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
        spawnedBookInstance.SetActive(true);

        panelPlaced = true;
    }

    public void ResetPanel()
    {
        if (spawnedBookInstance != null)
        {
            Destroy(spawnedBookInstance);
            Debug.Log("[SpawnBook] 🗑️ Destroyed existing book panel.");
        }
        panelPlaced = false;
    }
}