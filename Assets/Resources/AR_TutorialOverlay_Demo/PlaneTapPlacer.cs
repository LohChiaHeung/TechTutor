using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaneTapPlacer : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycaster;

    [Header("Prefabs")]
    public GameObject photoBoardPrefab;   // your PhotoBoard prefab

    [Header("Sizing")]
    public float boardWidthMeters = 0.35f; // poster width

    [Header("Analysis (no OpenAI yet)")]
    public LocalLayoutAnalyzer analyzer;  // assign in Inspector

    static readonly List<ARRaycastHit> hits = new();
    GameObject spawnedBoard;

    // --- UI Button: Take Photo (real-time) ---
    public void TakePhoto()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeCamera.TakePicture(path =>
        {
            if (string.IsNullOrEmpty(path)) return;

            Texture2D tex = NativeCamera.LoadImageAtPath(
                path, 1280, markTextureNonReadable: false, generateMipmaps: false
            );
            if (!tex) return;

            StartCoroutine(PlaceOnNextTap(tex));
        },
        maxSize: 2048,
        preferredCamera: NativeCamera.PreferredCamera.Rear
        );
#else
        Debug.Log("TakePhoto works on device. In Editor, use PickFromGallery().");
#endif
    }

    // --- UI Button: Pick from Gallery (optional) ---
    public void PickFromGallery()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery(path =>
        {
            if (string.IsNullOrEmpty(path)) return;

            Texture2D tex = NativeGallery.LoadImageAtPath(
                path, 1280, markTextureNonReadable: false, generateMipmaps: false
            );
            if (!tex) return;

            StartCoroutine(PlaceOnNextTap(tex));
        }, "Select an image", "image/*");
#else
        var tex = Resources.Load<Texture2D>("sampleshot");
        if (tex) StartCoroutine(PlaceOnNextTap(tex));
#endif
    }

    // --- Core placement on plane tap ---
    System.Collections.IEnumerator PlaceOnNextTap(Texture2D tex)
    {
        while (true)
        {
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                var touch = Input.GetTouch(0);
                if (raycaster.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
                {
                    var pose = hits[0].pose;

                    // Instantiate at plane hit position
                    if (spawnedBoard) Destroy(spawnedBoard);
                    spawnedBoard = Instantiate(photoBoardPrefab, pose.position, Quaternion.identity);

                    // Face the camera, keep upright
                    var cam = Camera.main.transform;
                    Vector3 forward = cam.forward; forward.y = 0f; forward.Normalize();
                    spawnedBoard.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

                    // Apply image & size
                    var ctrl = spawnedBoard.GetComponent<PhotoBoardController>();
                    if (!ctrl) ctrl = spawnedBoard.AddComponent<PhotoBoardController>();

                    // Ensure MarkersRoot assigned if prefab missed it
                    if (!ctrl.markersRoot)
                    {
                        var mr = spawnedBoard.transform.Find("MarkersRoot");
                        if (mr) ctrl.markersRoot = mr;
                    }

                    ctrl.SetTexture(tex, boardWidthMeters);

                    // Mock analysis (no OpenAI yet)
                    if (analyzer)
                    {
                        var results = analyzer.Analyze(tex);
                        ctrl.AnalyzeAndOverlay(results);
                    }

                    yield break;
                }
            }
            yield return null;
        }
    }

    // Optional: UI button to clear the current board
    public void ClearBoard()
    {
        if (spawnedBoard) Destroy(spawnedBoard);
    }
}
