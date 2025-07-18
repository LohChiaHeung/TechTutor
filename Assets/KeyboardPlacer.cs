//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR.ARFoundation;
//using UnityEngine.XR.ARSubsystems;

//public class KeyboardPlacer : MonoBehaviour
//{
//    [Header("References")]
//    public GameObject keyboardPrefab;
//    public ARRaycastManager raycastManager;
//    public PlaneLocker planeLocker;

//    private GameObject placedKeyboard;
//    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

//    void Update()
//    {
//        // Don't place anything if locked area is not ready yet
//        if (planeLocker == null || !planeLocker.lockedAreaReady)
//            return;

//        if (Input.touchCount == 0)
//            return;

//        Touch touch = Input.GetTouch(0);

//        if (touch.phase == TouchPhase.Began)
//        {
//            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
//            {
//                Pose hitPose = hits[0].pose;

//                if (planeLocker.IsPointInsideLockedArea(hitPose.position))
//                {
//                    if (placedKeyboard == null)
//                    {
//                        placedKeyboard = Instantiate(
//                            keyboardPrefab,
//                            hitPose.position,
//                            Quaternion.identity
//                        );

//                        placedKeyboard.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);

//                        Debug.Log("Keyboard placed INSIDE locked area.");
//                    }
//                }
//                else
//                {
//                    Debug.Log("Tap is outside the locked area.");
//                }
//            }
//        }
//    }
//}
