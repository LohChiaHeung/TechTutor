using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class PlaneLocker : MonoBehaviour
{
    [Header("AR Managers")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    //[Header("Locked Area Prefab")]
    //public GameObject lockedAreaPrefab;

    [Header("Confirm Button")]
    public GameObject confirmButton;

    [Header("Keyboard Prefab")]
    public GameObject keyboardPrefab;
    public AudioClip keyboardDescriptionAudio;
    [TextArea]
    public string keyboardDescriptionText;

    [Header("Preview Prefab")]
    public GameObject keyboardPreviewPrefab;

    [Header("Ghost Material")]
    public Material ghostMaterial;

    [Header("AR Text Overlay Prefab")]
    public GameObject arTextCanvasPrefab;

    //[Header("Key Highlights")]
    //public GameObject keyHighlightGroup;

    private ARPlane lockedPlane;
    private GameObject lockedAreaVisual;
    private GameObject keyboardInstance;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private bool lockedAreaReady = false;

    void Update()
    {
        if (lockedPlane != null || lockedAreaReady)
            return;

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                var hit = hits[0];
                Pose hitPose = hit.pose;

                lockedPlane = hit.trackable as ARPlane;

                LockPlane(lockedPlane, hitPose);
            }
        }
    }

    void LockPlane(ARPlane plane, Pose hitPose)
    {
        Debug.Log("[PlaneLocker] Plane locked: " + plane.trackableId);

        planeManager.enabled = false;

        foreach (var arPlane in planeManager.trackables)
        {
            if (arPlane != plane)
                arPlane.gameObject.SetActive(false);
        }

        if (keyboardPreviewPrefab != null)
        {
            lockedAreaVisual = Instantiate(
                keyboardPreviewPrefab,
                hitPose.position,
                Quaternion.identity
            );
            lockedAreaVisual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            // 🔄 Make it face the camera
            Vector3 cameraPosition = Camera.main.transform.position;
            Vector3 lookDirection = cameraPosition - lockedAreaVisual.transform.position;
            lookDirection.y = 0; // Optional: remove this line if you want full rotation
            lockedAreaVisual.transform.rotation = Quaternion.LookRotation(-lookDirection);

            Renderer[] renderers = lockedAreaVisual.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material = ghostMaterial; // ✅ your semi-transparent material
            }

            lockedAreaVisual.AddComponent<AreaManipulator>();
        }



        if (confirmButton != null)
        {
            confirmButton.SetActive(true);
        }
    }

    public void ConfirmLockedArea()
    {
        if (lockedAreaVisual == null)
        {
            Debug.LogError("[PlaneLocker] Cannot confirm locked area - visual is null.");
            return;
        }

        lockedAreaReady = true;

        if (confirmButton != null)
            confirmButton.SetActive(false);

        //var manipulator = lockedAreaVisual.GetComponent<AreaManipulator>();
        //if (manipulator != null)
        //{
        //    Destroy(manipulator);
        //    Debug.Log("[PlaneLocker] Disabled AreaManipulator after confirmation.");
        //}

        lockedAreaVisual.SetActive(false);

        Vector3 spawnPosition = lockedAreaVisual.transform.position;
        spawnPosition.y += 0.01f;

        keyboardInstance = Instantiate(
            keyboardPrefab,
            spawnPosition,
            Quaternion.identity
        );

        keyboardInstance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        keyboardInstance.AddComponent<KeyboardManipulator>();

        Debug.Log("[PlaneLocker] Keyboard instantiated at: " + spawnPosition);

        GameObject canvasObj = Instantiate(arTextCanvasPrefab, keyboardInstance.transform);
        canvasObj.transform.localPosition = new Vector3(0, 2.5f, -2.5f);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.0025f;

        Debug.Log("[Debug] Canvas local scale: " + canvasObj.transform.localScale);
        Debug.Log("[Debug] Canvas rect size: " + canvasObj.GetComponent<RectTransform>().sizeDelta);

        Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
        if (backgroundPanel == null)
        {
            Debug.LogError("[PlaneLocker] BackgroundPanel not found in ARTextCanvas prefab!");
        }

        KeyboardTapHandler tapHandler = keyboardInstance.AddComponent<KeyboardTapHandler>();
        tapHandler.descriptionAudio = keyboardDescriptionAudio;
        tapHandler.descriptionText = keyboardDescriptionText;
        tapHandler.canvasObject = canvasObj;
        tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        //tapHandler.keyHighlightGroup = keyHighlightGroup;
        //tapHandler.keyHighlightGroup.SetActive(false);

        Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
        if (closeBtnTransform == null)
        {
            Debug.LogError("[PlaneLocker] Close button not found in canvas!");
        }
        else
        {
            Button closeBtn = closeBtnTransform.GetComponent<Button>();
            if (closeBtn != null)
            {
                tapHandler.closeButton = closeBtn;
                closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
            }
            else
            {
                Debug.LogError("[PlaneLocker] Button component missing on Button_Close!");
            }
        }

        Transform keyGroup = keyboardInstance.transform.Find("KeyHighlightGroup");
        if (keyGroup != null)
        {
            tapHandler.keyHighlightGroup = keyGroup.gameObject;
            keyGroup.gameObject.SetActive(false);
        }
    }
}
