using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;
using Unity.Barracuda;

public class ARPhotoCapture : MonoBehaviour
{
    [Header("AR Components")]
    public ARPlaneManager planeManager;
    public ARRaycastManager raycastManager;

    [Header("UI")]
    public Button captureButton;
    public TMP_Text infoText;

    [Header("YOLO")]
    public YoloDetector yoloDetector;
    public GameObject labelPrefab;

    private ARPlane fixedPlane;
    private bool planeLocked = false;


    void Start()
    {
        yoloDetector.Init();

        // Disable the button initially
        captureButton.interactable = false;

        captureButton.onClick.AddListener(CapturePhoto);
    }

    void Update()
    {
        if (!planeLocked)
        {
            CheckForPlaneTap();
        }

        if (planeLocked)
        {
            captureButton.interactable = true;
            infoText.text = "✅ Plane locked. Ready to scan.";
        }
        else
        {
            captureButton.interactable = false;
            infoText.text = "❌ Tap a plane to lock it first.";
        }
    }

    private void CheckForPlaneTap()
    {
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Vector2 touchPosition = Input.GetTouch(0).position;

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                var planeHit = hits[0].trackable as ARPlane;

                if (planeHit != null)
                {
                    LockPlane(planeHit);
                }
            }
        }
    }


    private void LockPlane(ARPlane selectedPlane)
    {
        Debug.Log("✅ Locking plane: " + selectedPlane.trackableId);

        fixedPlane = selectedPlane;
        planeLocked = true;

        // Stop tracking all other planes
        foreach (var plane in planeManager.trackables)
        {
            if (plane.trackableId != fixedPlane.trackableId)
            {
                plane.gameObject.SetActive(false);
                plane.gameObject.GetComponent<ARPlane>().enabled = false;
            }
        }

        // Keep only our fixed plane
        fixedPlane.gameObject.SetActive(true);
        fixedPlane.gameObject.GetComponent<ARPlane>().enabled = false;

        // Disable plane detection entirely
        planeManager.enabled = false;

        infoText.text = "✅ Plane locked! Tap Scan.";
    }


    void CapturePhoto()
    {
        StartCoroutine(CaptureScreenshot());
    }

    private IEnumerator CaptureScreenshot()
    {
        yield return new WaitForEndOfFrame();

        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        Debug.Log("Photo captured. Size: " + screenshot.width + "x" + screenshot.height);

        // Optional: save locally for debugging
        byte[] pngData = screenshot.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.persistentDataPath + "/ARPhoto.png", pngData);

        Debug.Log("Saved screenshot to: " + Application.persistentDataPath + "/ARPhoto.png");

        ProcessImageForModel(screenshot);
    }

    void ProcessImageForModel(Texture2D tex)
    {
        Debug.Log("Ready to process image for ML model. Size: " + tex.width + "x" + tex.height);

        // Crop center region (square) before resizing
        Texture2D cropped = CropCenter(tex, 1080, 1080);
        Texture2D resized = ResizeTexture(cropped, 640, 640);

        List<YoloBox> boxes = yoloDetector.Detect(resized);

        Debug.Log($"✅ YOLO found {boxes.Count} boxes.");

        List<YoloBox> filteredBoxes = yoloDetector.ApplyNMS(boxes, 0.5f);

        Debug.Log($"✅ After NMS: {filteredBoxes.Count} boxes.");

        int placedCount = 0;

        //foreach (var box in filteredBoxes)
        //{
        //    Debug.Log($"📦 Label: {box.label} | Confidence: {box.confidence:F2} | (x={box.x}, y={box.y}, w={box.w}, h={box.h})");

        //    float centerX = box.x;
        //    float centerY = box.y;

        //    float normX = centerX / 640f;
        //    float normY = centerY / 640f;

        //    float screenX = Mathf.Clamp(normX * Screen.width, 0, Screen.width - 1);
        //    float screenY = Mathf.Clamp((1f - normY) * Screen.height, 0, Screen.height - 1);

        //    Debug.Log($"→ Screen point for raycast: ({screenX}, {screenY})");

        //    List<ARRaycastHit> hits = new List<ARRaycastHit>();
        //    if (raycastManager.Raycast(new Vector2(screenX, screenY), hits, TrackableType.PlaneWithinPolygon))
        //    {
        //        Pose hitPose = hits[0].pose;
        //        var labelObj = Instantiate(labelPrefab, hitPose.position, hitPose.rotation);
        //        //labelObj.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        //        var textMesh = labelObj.GetComponentInChildren<TextMeshPro>();

        //        if (textMesh != null)
        //        {
        //            textMesh.text = $"{box.label} ({box.confidence:F2})";
        //        }

        //        Debug.Log($"✅ Placed label [{box.label}] at AR position {hitPose.position}");
        //        placedCount++;
        //    }
        //    else
        //    {
        //        Debug.LogWarning($"❌ No plane hit for detection {box.label} at screen point ({screenX}, {screenY})");
        //    }
        //}

        foreach (var box in boxes)
        {
            Debug.Log($"📦 Label: {box.label} | Confidence: {box.confidence:F2}");

            float centerX = box.x;
            float centerY = box.y;

            float normX = centerX / 640f;
            float normY = centerY / 640f;

            float screenX = Mathf.Clamp(normX * Screen.width, 0, Screen.width);
            float screenY = Mathf.Clamp((1f - normY) * Screen.height, 0, Screen.height);

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(new Vector2(screenX, screenY), hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                var labelObj = Instantiate(labelPrefab, hitPose.position, hitPose.rotation);

                labelObj.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);

                var textMesh = labelObj.GetComponentInChildren<TMPro.TextMeshPro>();

                if (textMesh != null)
                {
                    textMesh.text = $"{box.label} ({box.confidence:F2})";
                }

                Debug.Log($"✅ Placed label [{box.label}] at AR position {hitPose.position}");
            }
            else
            {
                Debug.LogWarning($"❌ No plane hit for detection {box.label} at screen point ({screenX}, {screenY})");
            }
        }


        Debug.Log($"✅ Successfully placed {placedCount} AR labels.");
    }

    Texture2D ResizeTexture(Texture2D source, int width, int height)
    {
        RenderTexture rt = RenderTexture.GetTemporary(width, height);
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        result.Apply();
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    Texture2D CropCenter(Texture2D tex, int targetWidth, int targetHeight)
    {
        int x = (tex.width - targetWidth) / 2;
        int y = (tex.height - targetHeight) / 2;

        Color[] pixels = tex.GetPixels(x, y, targetWidth, targetHeight);
        Texture2D cropped = new Texture2D(targetWidth, targetHeight, tex.format, false);
        cropped.SetPixels(pixels);
        cropped.Apply();
        return cropped;
    }

    public Tensor TransformImageToTensor(Texture2D tex)
    {
        float[] floats = new float[tex.width * tex.height * 3];
        Color32[] pixels = tex.GetPixels32();

        for (int i = 0; i < pixels.Length; i++)
        {
            floats[i * 3 + 0] = pixels[i].r / 255f;
            floats[i * 3 + 1] = pixels[i].g / 255f;
            floats[i * 3 + 2] = pixels[i].b / 255f;
        }

        Tensor t = new Tensor(1, tex.height, tex.width, 3, floats);
        return t;
    }
}
