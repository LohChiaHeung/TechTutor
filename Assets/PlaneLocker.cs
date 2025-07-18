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

    [Header("Locked Area Prefab")]
    public GameObject lockedAreaPrefab;

    [Header("Confirm Button")]
    public GameObject confirmButton;

    [Header("Keyboard Prefab")]
    public GameObject keyboardPrefab;
    public AudioClip keyboardDescriptionAudio;
    [TextArea]
    public string keyboardDescriptionText;

    [Header("AR Text Overlay Prefab")]
    public GameObject arTextCanvasPrefab;

    [Header("Key Highlights")]
    public GameObject keyHighlightGroup;

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

        // Disable plane detection
        planeManager.enabled = false;

        // Hide all other planes except the locked one
        foreach (var arPlane in planeManager.trackables)
        {
            if (arPlane != plane)
                arPlane.gameObject.SetActive(false);
        }

        if (lockedAreaPrefab != null)
        {
            lockedAreaVisual = Instantiate(
                lockedAreaPrefab,
                hitPose.position,
                Quaternion.Euler(90, 0, 0)
            );

            // Set initial reasonable scale (e.g. 0.3m x 0.3m)
            lockedAreaVisual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);

            Debug.Log("[PlaneLocker] Locked area visual spawned at: " + hitPose.position);
        }
        else
        {
            Debug.LogWarning("[PlaneLocker] Locked Area Prefab not assigned!");
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

        Debug.Log("[PlaneLocker] Confirming locked area...");

        lockedAreaReady = true;

        if (confirmButton != null)
            confirmButton.SetActive(false);

        var manipulator = lockedAreaVisual.GetComponent<AreaManipulator>();
        if (manipulator != null)
        {
            Destroy(manipulator);
            Debug.Log("[PlaneLocker] Disabled AreaManipulator after confirmation.");
        }

        lockedAreaVisual.SetActive(false);

        Vector3 spawnPosition = lockedAreaVisual.transform.position;
        spawnPosition.y += 0.01f;

        keyboardInstance = Instantiate(
            keyboardPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // Apply extra rotation in code:
        //keyboardInstance.transform.Rotate(-90f, 0f, 180f, Space.Self);
        keyboardInstance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        // Add manipulator for scaling/rotation
        keyboardInstance.AddComponent<KeyboardManipulator>();

        Debug.Log("[PlaneLocker] Keyboard instantiated at: " + spawnPosition);

        GameObject canvasObj = Instantiate(arTextCanvasPrefab, keyboardInstance.transform);
        canvasObj.transform.localPosition = new Vector3(0, 2.5f, 0);
        canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.01f;

        //canvasObj.transform.LookAt(Camera.main.transform);
        //canvasObj.transform.Rotate(0, 180f, 0);

        Debug.Log("[Debug] Canvas local scale: " + canvasObj.transform.localScale);
        Debug.Log("[Debug] Canvas rect size: " + canvasObj.GetComponent<RectTransform>().sizeDelta);

        // Get the background panel child
        Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
        if (backgroundPanel == null)
        {
            Debug.LogError("[PlaneLocker] BackgroundPanel not found in ARTextCanvas prefab!");
        }



        //TMPro.TextMeshProUGUI text = canvasObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        //text.text = keyboardDescriptionText;
        //text.color = new Color(1, 1, 1, 1);
        //text.gameObject.SetActive(false);

        // Assign handler
        KeyboardTapHandler tapHandler = keyboardInstance.AddComponent<KeyboardTapHandler>();
        tapHandler.descriptionAudio = keyboardDescriptionAudio;
        tapHandler.descriptionText = keyboardDescriptionText;
        tapHandler.canvasObject = canvasObj;
        tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        tapHandler.keyHighlightGroup = keyHighlightGroup;
        tapHandler.keyHighlightGroup.SetActive(false);

        // Find the close button inside the canvas
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
            keyGroup.gameObject.SetActive(false); // 👈 this is the key fix
        }


    }

}
