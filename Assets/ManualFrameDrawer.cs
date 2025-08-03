
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;

//public class ManualFrameDrawer4Tap : MonoBehaviour
//{
//    [Header("Gmail Tutorial Panel (World-Space Canvas Prefab)")]
//    public GameObject gmailTutorialPanelPrefab;

//    [Header("Distance in front of camera to place the frame (in meters)")]
//    public float placementDistance = 0.75f;

//    [Header("Scale factor to convert large UI (e.g. 1000x1200) to world units")]
//    public float scaleFactor = 0.001f;

//    private List<Vector2> tapPoints = new List<Vector2>();
//    private GameObject tutorialPanelInstance;
//    private bool panelPlaced = false;

//    void Update()
//    {
//        if (panelPlaced) return; // ✅ Prevent further input

//        if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
//        {
//            Vector2 touchPos = Input.GetTouch(0).position;

//            // Avoid duplicate taps
//            if (tapPoints.Any(p => Vector2.Distance(p, touchPos) < 10f))
//            {
//                Debug.LogWarning("[ManualFrameDrawer4Tap] ⚠️ Tap too close to previous. Ignored.");
//                return;
//            }

//            tapPoints.Add(touchPos);
//            Debug.Log($"📍 Tap {tapPoints.Count}: {touchPos}");

//            //UpdateLinePreview();

//            if (tapPoints.Count == 4)
//            {
//                Debug.Log($"[ManualFrameDrawer4Tap] ✅ Received 4 tap points. Proceeding to place panel...");
//                PlacePanelFromFourPoints();
//            }
//            //if (tapPoints.Count == 1)
//            //{
//            //    Debug.Log($"[ManualFrameDrawer4Tap] ✅ Using single tap to simulate rectangle.");
//            //    SimulateFourPointsAroundCenter(tapPoints[0]);
//            //    PlacePanelFromFourPoints();
//            //}

//        }
//    }

//    //void SimulateFourPointsAroundCenter(Vector2 center)
//    //{
//    //    float offset = 150f; // in pixels; half-width/height of rectangle box

//    //    tapPoints.Clear(); // clear previous

//    //    tapPoints.Add(center + new Vector2(-offset, offset)); // Top-left
//    //    tapPoints.Add(center + new Vector2(offset, offset)); // Top-right
//    //    tapPoints.Add(center + new Vector2(offset, -offset)); // Bottom-right
//    //    tapPoints.Add(center + new Vector2(-offset, -offset)); // Bottom-left
//    //}


//    //void UpdateLinePreview()
//    //{
//    //    if (lineRenderer == null || tapPoints.Count < 2)
//    //        return;

//    //    List<Vector3> worldPoints = new List<Vector3>();
//    //    foreach (var screenPoint in tapPoints)
//    //    {
//    //        var viewPoint = new Vector3(screenPoint.x / Screen.width, screenPoint.y / Screen.height, placementDistance);
//    //        Vector3 rawPoint = Camera.main.ViewportToWorldPoint(viewPoint);
//    //        rawPoint.z = Camera.main.transform.position.z + placementDistance; // force same Z
//    //        worldPoints.Add(rawPoint);

//    //    }

//    //    if (tapPoints.Count == 4)
//    //    {
//    //        List<Vector3> sortedPoints = SortRectanglePoints(worldPoints);
//    //        lineRenderer.positionCount = 5;
//    //        for (int i = 0; i < 4; i++)
//    //        {
//    //            lineRenderer.SetPosition(i, sortedPoints[i]);
//    //        }
//    //        lineRenderer.SetPosition(4, sortedPoints[0]); // close loop
//    //    }
//    //    else
//    //    {
//    //        lineRenderer.positionCount = tapPoints.Count;
//    //        for (int i = 0; i < tapPoints.Count; i++)
//    //        {
//    //            lineRenderer.SetPosition(i, worldPoints[i]);
//    //        }
//    //    }
//    //}

//    void PlacePanelFromFourPoints()
//    {
//        if (gmailTutorialPanelPrefab == null)
//        {
//            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Prefab not assigned.");
//            return;
//        }

//        if (tapPoints.Distinct().Count() < 4)
//        {
//            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Not enough unique tap points. Ignoring.");
//            ResetFrame();
//            return;
//        }

//        List<Vector3> worldPoints = new List<Vector3>();
//        foreach (var screenPoint in tapPoints)
//        {
//            Vector3 viewPoint = new Vector3(
//                screenPoint.x / Screen.width,
//                screenPoint.y / Screen.height,
//                placementDistance
//            );
//            worldPoints.Add(Camera.main.ViewportToWorldPoint(viewPoint));
//        }

//        List<Vector3> sortedPoints = SortRectanglePoints(worldPoints);

//        // Log all 4 world points
//        for (int i = 0; i < sortedPoints.Count; i++)
//        {
//            Debug.Log($"📌 Sorted Point {i + 1}: {sortedPoints[i]}");
//        }

//        float rawWidth = Mathf.Abs(sortedPoints[1].x - sortedPoints[0].x);
//        float rawHeight = Mathf.Abs(sortedPoints[0].y - sortedPoints[3].y);

//        float minX = Mathf.Min(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//        float maxX = Mathf.Max(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//        float minY = Mathf.Min(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);
//        float maxY = Mathf.Max(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);

//        float unclampedWidth = maxX - minX;
//        float unclampedHeight = maxY - minY;

//        float width = Mathf.Clamp(unclampedWidth, 0.5f, 1.5f);
//        float height = Mathf.Clamp(unclampedHeight, 0.3f, 1.0f);

//        float avgZ = sortedPoints.Average(p => p.z);
//        Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, avgZ);

//        Vector3 finalScale = new Vector3(width, height, 0.01f) * scaleFactor;

//        Debug.Log("─────────────────────────────────────────────");
//        Debug.Log($"📏 Raw Width (no clamp): {unclampedWidth:F2}, Raw Height: {unclampedHeight:F2}");
//        Debug.Log($"📐 Clamped Width: {width:F2}, Clamped Height: {height:F2}");
//        Debug.Log($"📦 Final Local Scale (after scaleFactor): {finalScale}");
//        Debug.Log($"🎯 Center Position: {center} | Avg Z: {avgZ:F2}");
//        Debug.Log("─────────────────────────────────────────────");

//        var anchorManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARAnchorManager>();
//        if (anchorManager != null)
//        {
//            var pose = new Pose(center, Quaternion.LookRotation(-Camera.main.transform.forward));
//            var anchor = anchorManager.AddAnchor(pose);

//            if (anchor != null)
//            {
//                tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//                tutorialPanelInstance.transform.SetParent(anchor.transform, true);
//            }
//            else
//            {
//                Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Failed to create ARAnchor.");
//                tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//            }
//        }
//        else
//        {
//            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ ARAnchorManager not found. Falling back to direct placement.");
//            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, Quaternion.LookRotation(-Camera.main.transform.forward));
//        }

//        tutorialPanelInstance.transform.localScale = finalScale;
//        tutorialPanelInstance.SetActive(true);

//        Debug.Log("[ManualFrameDrawer4Tap] 🎉 Panel placed and activated.");

//        tapPoints.Clear();
//        panelPlaced = true;
//    }




//    // Correct Panel
//    //void PlacePanelFromFourPoints()
//    //{
//    //    if (gmailTutorialPanelPrefab == null)
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Prefab not assigned.");
//    //        return;
//    //    }

//    //    if (tapPoints.Distinct().Count() < 4)
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Not enough unique tap points. Ignoring.");
//    //        ResetFrame();
//    //        return;
//    //    }

//    //    List<Vector3> worldPoints = new List<Vector3>();
//    //    foreach (var screenPoint in tapPoints)
//    //    {
//    //        Vector3 viewPoint = new Vector3(
//    //            screenPoint.x / Screen.width,
//    //            screenPoint.y / Screen.height,
//    //            placementDistance
//    //        );
//    //        worldPoints.Add(Camera.main.ViewportToWorldPoint(viewPoint));
//    //    }

//    //    List<Vector3> sortedPoints = SortRectanglePoints(worldPoints);

//    //    float minX = Mathf.Min(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//    //    float maxX = Mathf.Max(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//    //    float minY = Mathf.Min(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);
//    //    float maxY = Mathf.Max(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);

//    //    float width = Mathf.Clamp(maxX - minX, 0.5f, 1.5f);
//    //    float height = Mathf.Clamp(maxY - minY, 0.3f, 1.0f);

//    //    // Average Z for better depth stability
//    //    float avgZ = sortedPoints.Average(p => p.z);

//    //    Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, avgZ);

//    //    Debug.Log($"🧮 Final Frame → Center: {center}, Width: {width}, Height: {height}, Z: {avgZ}");

//    //    // Create anchor at center
//    //    var anchorManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARAnchorManager>();
//    //    if (anchorManager != null)
//    //    {
//    //        var pose = new Pose(center, Quaternion.LookRotation(-Camera.main.transform.forward));
//    //        var anchor = anchorManager.AddAnchor(pose);

//    //        if (anchor != null)
//    //        {
//    //            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//    //            tutorialPanelInstance.transform.SetParent(anchor.transform, true);
//    //        }
//    //        else
//    //        {
//    //            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Failed to create ARAnchor.");
//    //            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//    //        }
//    //    }
//    //    else
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ ARAnchorManager not found. Falling back to direct placement.");
//    //        tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, Quaternion.LookRotation(-Camera.main.transform.forward));
//    //    }

//    //    tutorialPanelInstance.transform.localScale = new Vector3(width, height, 0.01f) * scaleFactor;
//    //    tutorialPanelInstance.SetActive(true);

//    //    Debug.Log("[ManualFrameDrawer4Tap] 🎉 Panel placed and activated.");

//    //    // Clear
//    //    tapPoints.Clear();
//    //    //if (lineRenderer != null) lineRenderer.positionCount = 0;
//    //    panelPlaced = true;
//    //}

//    //void PlacePanelFromFourPoints()
//    //{
//    //    if (gmailTutorialPanelPrefab == null)
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Prefab not assigned.");
//    //        return;
//    //    }

//    //    if (tapPoints.Distinct().Count() < 4)
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Not enough unique tap points. Ignoring.");
//    //        ResetFrame();
//    //        return;
//    //    }

//    //    List<Vector3> worldPoints = new List<Vector3>();
//    //    foreach (var screenPoint in tapPoints)
//    //    {
//    //        Vector3 viewPoint = new Vector3(
//    //            screenPoint.x / Screen.width,
//    //            screenPoint.y / Screen.height,
//    //            placementDistance
//    //        );
//    //        worldPoints.Add(Camera.main.ViewportToWorldPoint(viewPoint));
//    //    }

//    //    List<Vector3> sortedPoints = SortRectanglePoints(worldPoints);

//    //    float minX = Mathf.Min(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//    //    float maxX = Mathf.Max(sortedPoints[0].x, sortedPoints[1].x, sortedPoints[2].x, sortedPoints[3].x);
//    //    float minY = Mathf.Min(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);
//    //    float maxY = Mathf.Max(sortedPoints[0].y, sortedPoints[1].y, sortedPoints[2].y, sortedPoints[3].y);

//    //    float width = maxX - minX;
//    //    float height = maxY - minY;

//    //    float avgZ = sortedPoints.Average(p => p.z);
//    //    Vector3 center = new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, avgZ);

//    //    Vector3 finalScale = new Vector3(width, height, 0.01f) * scaleFactor;

//    //    Debug.Log("─────────────────────────────────────────────");
//    //    Debug.Log($"📏 Raw Width: {width:F2}, Raw Height: {height:F2}");
//    //    Debug.Log($"📦 Final Local Scale Applied: {finalScale}");
//    //    Debug.Log($"📍 Center Position: {center}");
//    //    Debug.Log("─────────────────────────────────────────────");

//    //    var anchorManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARAnchorManager>();
//    //    if (anchorManager != null)
//    //    {
//    //        var pose = new Pose(center, Quaternion.LookRotation(-Camera.main.transform.forward));
//    //        var anchor = anchorManager.AddAnchor(pose);

//    //        if (anchor != null)
//    //        {
//    //            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//    //            tutorialPanelInstance.transform.SetParent(anchor.transform, true);
//    //        }
//    //        else
//    //        {
//    //            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Failed to create ARAnchor.");
//    //            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, pose.rotation);
//    //        }
//    //    }
//    //    else
//    //    {
//    //        Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ ARAnchorManager not found. Falling back to direct placement.");
//    //        tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, center, Quaternion.LookRotation(-Camera.main.transform.forward));
//    //    }

//    //    tutorialPanelInstance.transform.localScale = finalScale;
//    //    tutorialPanelInstance.SetActive(true);

//    //    tapPoints.Clear();
//    //    panelPlaced = true;
//    //}


//    public void ResetFrame()
//    {
//        if (tutorialPanelInstance != null)
//        {
//            Destroy(tutorialPanelInstance);
//            Debug.Log("[ManualFrameDrawer4Tap] 🗑️ Existing panel destroyed.");
//        }

//        tapPoints.Clear();
//        //if (lineRenderer != null) lineRenderer.positionCount = 0;

//        Debug.Log("[ManualFrameDrawer4Tap] 🔄 Tap points reset.");

//        panelPlaced = false;
//    }

//    List<Vector3> SortRectanglePoints(List<Vector3> points)
//    {
//        points.Sort((a, b) => b.y.CompareTo(a.y)); // top to bottom
//        var topPoints = points.GetRange(0, 2);
//        var bottomPoints = points.GetRange(2, 2);

//        topPoints.Sort((a, b) => a.x.CompareTo(b.x));    // left to right
//        bottomPoints.Sort((a, b) => a.x.CompareTo(b.x)); // left to right

//        Vector3 topLeft = topPoints[0];
//        Vector3 topRight = topPoints[1];
//        Vector3 bottomLeft = bottomPoints[0];
//        Vector3 bottomRight = bottomPoints[1];

//        return new List<Vector3> { topLeft, topRight, bottomRight, bottomLeft };
//    }
//}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ManualFrameDrawer4Tap : MonoBehaviour
{
    public ARStepManager stepManager;
    private int retainedStepIndex = 0;

    [Header("Canvas Prefab (World Space)")]
    public GameObject gmailTutorialPanelPrefab;

    [Header("Distance in front of camera")]
    public float placementDistance = 0.75f;

    [Header("Fixed canvas size in meters")]
    public float fixedWidth = 0.63f;
    public float fixedHeight = 1.20f;
    public float scaleFactor = 0.001f;

    private GameObject tutorialPanelInstance;
    private bool panelPlaced = false;

    public GameObject arGmailCanvas;  // assign in Inspector

    void Start()
    {
        if (arGmailCanvas != null)
        {
            arGmailCanvas.SetActive(false);
            Debug.Log("[ManualFrameDrawer4Tap] ✅ Canvas set inactive at start.");
        }
    }

    void Update()
    {
        if (panelPlaced || Input.touchCount != 1 || Input.GetTouch(0).phase != TouchPhase.Began)
            return;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 spawnPos = Camera.main.transform.position + cameraForward * placementDistance;
        Quaternion faceCamera = Quaternion.LookRotation(-cameraForward);

        Vector3 finalScale = new Vector3(fixedWidth, fixedHeight, 0.01f) * scaleFactor;

        Debug.Log("─────────────────────────────────────────────");
        Debug.Log($"📌 Spawning fixed-size panel");
        Debug.Log($"📏 Width: {fixedWidth}m, Height: {fixedHeight}m, ScaleFactor: {scaleFactor}");
        Debug.Log($"📦 Final Scale: {finalScale}");
        Debug.Log($"📍 Position: {spawnPos}");
        Debug.Log("─────────────────────────────────────────────");

        var anchorManager = FindObjectOfType<UnityEngine.XR.ARFoundation.ARAnchorManager>();
        if (anchorManager != null)
        {
            var pose = new Pose(spawnPos, faceCamera);
            var anchor = anchorManager.AddAnchor(pose);

            if (anchor != null)
            {
                tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, spawnPos, faceCamera);
                tutorialPanelInstance.transform.SetParent(anchor.transform, true);
            }
            else
            {
                Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ Failed to create ARAnchor.");
                tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, spawnPos, faceCamera);
            }
        }
        else
        {
            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ ARAnchorManager not found. Falling back to direct placement.");
            tutorialPanelInstance = Instantiate(gmailTutorialPanelPrefab, spawnPos, faceCamera);
        }

        tutorialPanelInstance.transform.localScale = finalScale;
        tutorialPanelInstance.SetActive(true);
        StartCoroutine(RestoreStepAfterDelay());

        // AFTER tutorialPanelInstance is created
        if (stepManager != null)
        {
            Debug.Log($"[ManualFrameDrawer4Tap] Restoring step index: {retainedStepIndex}");
            stepManager.SetStep(retainedStepIndex);
        }


        panelPlaced = true;
    }

    private IEnumerator RestoreStepAfterDelay()
    {
        yield return null; // wait 1 frame so prefab fully initializes

        stepManager = tutorialPanelInstance.GetComponentInChildren<ARStepManager>();

        if (stepManager != null)
        {
            Debug.Log($"[ManualFrameDrawer4Tap] ✅ Restoring step index: {retainedStepIndex}");
            stepManager.SetStep(retainedStepIndex);
        }
        else
        {
            Debug.LogWarning("[ManualFrameDrawer4Tap] ❌ stepManager not found after delay.");
        }
    }

    public void ResetPanel()
    {
        if (tutorialPanelInstance != null)
        {
            // ✅ Must find stepManager BEFORE destroying
            stepManager = tutorialPanelInstance.GetComponentInChildren<ARStepManager>();

            if (stepManager != null)
            {
                retainedStepIndex = stepManager.GetCurrentStep();
                Debug.Log($"[ResetPanel] Retaining progress at step: {retainedStepIndex}");
            }
            else
            {
                Debug.LogWarning("[ResetPanel] ❌ stepManager not found in panel.");
            }

            Destroy(tutorialPanelInstance);
        }

        panelPlaced = false;
    }



}