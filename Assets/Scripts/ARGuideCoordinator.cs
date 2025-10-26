////using System.Collections.Generic;
////using UnityEngine;
////using UnityEngine.XR.ARFoundation;
////using UnityEngine.XR.ARSubsystems;

////public class ARGuideCoordinator : MonoBehaviour
////{
////    [Header("AR")]
////    public ARRaycastManager raycastManager;
////    public ARPlaneManager planeManager;
////    public ARAnchorManager anchorManager;

////    [Header("Prefabs")]
////    public ARTutorialBoard boardPrefab;

////    [Header("Options")]
////    public float boardWidthMeters = 0.7f;
////    public bool disablePlaneVisualizationAfterPlace = true;

////    ARTutorialBoard placedBoard;
////    static readonly List<ARRaycastHit> s_Hits = new();

////    void Reset()
////    {
////        raycastManager = GetComponent<ARRaycastManager>();
////        planeManager = GetComponent<ARPlaneManager>();
////        anchorManager = GetComponent<ARAnchorManager>();
////    }

////    void Update()
////    {
////        if (placedBoard != null) return;
////        if (Input.touchCount == 0) return;

////        var touch = Input.GetTouch(0);
////        if (touch.phase != TouchPhase.Began) return;

////        if (raycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
////        {
////            var hit = s_Hits[0];
////            var plane = planeManager?.GetPlane(hit.trackableId);

////            ARAnchor anchor = null;
////            if (anchorManager != null && plane != null)
////            {
////                anchor = anchorManager.AttachAnchor(plane, hit.pose);
////            }
////            if (anchor == null)
////            {
////                // Fallback: plain instantiate at pose (less stable w/o anchor)
////                var go = new GameObject("TempAnchor");
////                go.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
////                anchor = go.AddComponent<ARAnchor>();
////            }

////            placedBoard = Instantiate(boardPrefab, anchor.transform);

////            // Get screenshot + first step text from your context
////            Texture2D tex = GuideRunContext.I ? GuideRunContext.I.screenshot : null;
////            string title = "Step 1";
////            string body = "";

////            if (GuideRunContext.I && GuideRunContext.I.guide != null &&
////                GuideRunContext.I.guide.steps != null && GuideRunContext.I.guide.steps.Length > 0)
////            {
////                var step0 = GuideRunContext.I.guide.steps[0];
////                title = string.IsNullOrWhiteSpace(step0.title) ? "Step 1" : step0.title;
////                body = string.IsNullOrWhiteSpace(step0.instruction) ? "" : step0.instruction;
////            }

////            placedBoard.Setup(tex, title, body, boardWidthMeters);


////            if (disablePlaneVisualizationAfterPlace && planeManager != null)
////            {
////                // hide plane polys (optional)
////                foreach (var p in planeManager.trackables)
////                {
////                    var mr = p.GetComponent<MeshRenderer>();
////                    if (mr) mr.enabled = false;
////                }
////                planeManager.requestedDetectionMode = PlaneDetectionMode.None;
////            }
////        }
////    }
////}

//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR.ARFoundation;
//using UnityEngine.XR.ARSubsystems;

//public class ARGuideCoordinator : MonoBehaviour
//{
//    [Header("AR")]
//    public ARRaycastManager raycastManager;
//    public ARPlaneManager planeManager;
//    public ARAnchorManager anchorManager;

//    [Header("Prefabs")]
//    public ARTutorialBoard boardPrefab;

//    [Header("Options")]
//    public float boardWidthMeters = 0.7f;
//    public bool disablePlaneVisualizationAfterPlace = true;

//    ARTutorialBoard placedBoard;
//    static readonly List<ARRaycastHit> s_Hits = new();

//    void Reset()
//    {
//        raycastManager = GetComponent<ARRaycastManager>();
//        planeManager = GetComponent<ARPlaneManager>();
//        anchorManager = GetComponent<ARAnchorManager>();
//    }

//    void Update()
//    {
//        if (placedBoard != null) return;
//        if (Input.touchCount == 0) return;

//        var touch = Input.GetTouch(0);
//        if (touch.phase != TouchPhase.Began) return;

//        if (!raycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon)) return;

//        var hit = s_Hits[0];
//        var plane = planeManager?.GetPlane(hit.trackableId);

//        ARAnchor anchor = null;
//        if (anchorManager != null && plane != null)
//            anchor = anchorManager.AttachAnchor(plane, hit.pose);

//        if (anchor == null)
//        {
//            var go = new GameObject("TempAnchor");
//            go.transform.SetPositionAndRotation(hit.pose.position, hit.pose.rotation);
//            anchor = go.AddComponent<ARAnchor>();
//        }

//        placedBoard = Instantiate(boardPrefab, anchor.transform);

//        // Use distinct names to avoid shadowing
//        Texture2D screenshotTex = GuideRunContext.I ? GuideRunContext.I.screenshot : null;
//        AIGuide guideData = GuideRunContext.I ? GuideRunContext.I.guide : null;

//        string title = "Step 1";
//        if (guideData?.steps != null && guideData.steps.Length > 0)
//            title = string.IsNullOrWhiteSpace(guideData.steps[0].title) ? "Step 1" : guideData.steps[0].title;

//        // If your board Setup takes (texture, title, widthMeters):
//        placedBoard.Setup(screenshotTex, title, boardWidthMeters);

//        // If you still have (texture, title, body, widthMeters), use this instead:
//        // string body = (guideData?.steps != null && guideData.steps.Length > 0) ? (guideData.steps[0].instruction ?? "") : "";
//        // placedBoard.Setup(screenshotTex, title, body, boardWidthMeters);

//        var runner = placedBoard.GetComponent<ARGuideRunner>();
//        if (runner) runner.Run(screenshotTex, guideData);

//        if (disablePlaneVisualizationAfterPlace && planeManager != null)
//        {
//            foreach (var p in planeManager.trackables)
//            {
//                var mr = p.GetComponent<MeshRenderer>();
//                if (mr) mr.enabled = false;
//            }
//            planeManager.requestedDetectionMode = PlaneDetectionMode.None;
//        }
//    }
//}

