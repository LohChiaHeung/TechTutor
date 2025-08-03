using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlaneLocker : MonoBehaviour
{
    [Header("AR Managers")]
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;

    [Header("Root Prefabs")]
    public GameObject monitorRootPrefab;

    [Header("UI")]
    public GameObject confirmButton;
    public GameObject modelSelectionPanel; // Panel with model buttons

    [Header("Model Prefabs")]
    public GameObject[] arModels;            // [keyboard, mouse, laptop, monitor, speaker]
    public GameObject[] arModelPreviews;     // Matching preview models
    public GameObject[] arTextCanvasPrefabs;    // matching info panels
    public AudioClip[] modelAudioClips;         // optional                    // ❌ Close button reference


    public UnityEngine.UI.Button rotateLeftButton;
    public UnityEngine.UI.Button rotateRightButton;
    public UnityEngine.UI.Button resetButton;


    [Header("Materials")]
    public Material ghostMaterial;

    [Header("Audio/Text Description")]
    public AudioClip keyboardDescriptionAudio;

    private ARPlane lockedPlane;
    private GameObject lockedAreaVisual;
    private GameObject finalModelInstance;
    private int selectedModelIndex = -1;

    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool lockedAreaReady = false;

    void Start()
    {
        if (modelSelectionPanel != null)
            modelSelectionPanel.SetActive(false);
    }


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

        if (modelSelectionPanel != null)
            modelSelectionPanel.SetActive(true);
    }

    public void SpawnPreviewForSelectedModel(int index)
    {
        selectedModelIndex = index;

        if (arModelPreviews == null || index < 0 || index >= arModelPreviews.Length)
        {
            Debug.LogError("[PlaneLocker] Invalid model index.");
            return;
        }

        // Clean up any previous preview or final model
        if (lockedAreaVisual != null)
        {
            Destroy(lockedAreaVisual);
            Debug.Log("[Model Selection] Destroyed previous preview model.");
        }

        if (finalModelInstance != null)
        {
            Destroy(finalModelInstance);
            Debug.Log("[Model Selection] Destroyed previous confirmed model.");
        }

        string modelName = arModelPreviews[index].name;
        Debug.Log($"[Model Selection] {modelName} model is selected.");

        Vector3 previewPosition = lockedPlane.transform.position;

        Quaternion previewRotation;

        if (index == 1) // 🖱 Mouse
        {
            previewRotation = Quaternion.identity; // Rotate X to lay flat
        }
        else
        {
            previewRotation = Quaternion.identity;
        }

        lockedAreaVisual = Instantiate(
            arModelPreviews[index],
            previewPosition,
            previewRotation
        );

        lockedAreaVisual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

        Vector3 cameraPosition = Camera.main.transform.position;
        Vector3 lookDirection = cameraPosition - lockedAreaVisual.transform.position;
        lookDirection.y = 0;
        lockedAreaVisual.transform.rotation = Quaternion.LookRotation(-lookDirection);

        if (index == 1) // 🖱 Mouse
        {
            //previewRotation = Quaternion.Euler(180f, 0f, 0f); // Rotate X to lay flat
        }
        else
        {
            //lockedAreaVisual.transform.rotation = Quaternion.LookRotation(-lookDirection);
        }

        Renderer[] renderers = lockedAreaVisual.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.material = ghostMaterial;
        }

        lockedAreaVisual.AddComponent<AreaManipulator>();

        if (confirmButton != null)
            confirmButton.SetActive(true);

        if (modelSelectionPanel != null)
            modelSelectionPanel.SetActive(false);
    }

    //public void SpawnPreviewForSelectedModel(int index)
    //{
    //    selectedModelIndex = index;

    //    if (arModelPreviews == null || index < 0 || index >= arModelPreviews.Length)
    //    {
    //        Debug.LogError("[PlaneLocker] Invalid model index.");
    //        return;
    //    }

    //    // Clean up previous models
    //    if (lockedAreaVisual != null)
    //    {
    //        Destroy(lockedAreaVisual);
    //        Debug.Log("[Model Selection] Destroyed previous preview model.");
    //    }

    //    if (finalModelInstance != null)
    //    {
    //        Destroy(finalModelInstance);
    //        Debug.Log("[Model Selection] Destroyed previous confirmed model.");
    //    }

    //    string modelName = arModelPreviews[index].name;
    //    Debug.Log($"[Model Selection] {modelName} model is selected.");

    //    Vector3 previewPosition = lockedPlane.transform.position;
    //    Vector3 cameraPosition = Camera.main.transform.position;
    //    Vector3 lookDirection = cameraPosition - previewPosition;
    //    lookDirection.y = 0;

    //    Quaternion previewRotation;

    //    if (index == 1) // 🖱 Mouse
    //    {
    //        previewRotation = Quaternion.Euler(90f, 0f, 180f); // Mouse specific rotation
    //    }
    //    else
    //    {
    //        previewRotation = Quaternion.LookRotation(-lookDirection); // Face user
    //    }

    //    // ✅ Use the final rotation here
    //    lockedAreaVisual = Instantiate(
    //        arModelPreviews[index],
    //        previewPosition,
    //        previewRotation
    //    );

    //    lockedAreaVisual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    //    // Apply ghost material
    //    Renderer[] renderers = lockedAreaVisual.GetComponentsInChildren<Renderer>();
    //    foreach (Renderer r in renderers)
    //    {
    //        r.material = ghostMaterial;
    //    }

    //    lockedAreaVisual.AddComponent<AreaManipulator>();

    //    if (confirmButton != null)
    //        confirmButton.SetActive(true);

    //    if (modelSelectionPanel != null)
    //        modelSelectionPanel.SetActive(false);
    //}

    //public void ConfirmLockedArea()
    //{
    //    if (lockedAreaVisual == null || selectedModelIndex == -1)
    //    {
    //        Debug.LogError("[PlaneLocker] Cannot confirm - no preview or model selected.");
    //        return;
    //    }

    //    lockedAreaReady = true;
    //    if (confirmButton != null)
    //        confirmButton.SetActive(false);

    //    lockedAreaVisual.SetActive(false);

    //    Vector3 spawnPosition = lockedAreaVisual.transform.position;
    //    spawnPosition.y += 0.01f;

    //    Quaternion spawnRotation = (selectedModelIndex == 1) ?
    //        lockedAreaVisual.transform.rotation * Quaternion.Euler(90f, 0f, 0f) :
    //        Quaternion.identity;

    //    finalModelInstance = Instantiate(
    //        arModels[selectedModelIndex],
    //        spawnPosition,
    //        spawnRotation
    //    );

    //    finalModelInstance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

    //    if (selectedModelIndex == 0)
    //        finalModelInstance.AddComponent<KeyboardManipulator>();
    //    else if (selectedModelIndex == 1)
    //        finalModelInstance.AddComponent<MouseManipulator>();
    //    else
    //        finalModelInstance.AddComponent<AreaManipulator>();

    //    Debug.Log("[PlaneLocker] Model instantiated at: " + spawnPosition);

    //    GameObject canvasObj = Instantiate(arTextCanvasPrefabs[selectedModelIndex], finalModelInstance.transform);
    //    canvasObj.transform.localPosition = (selectedModelIndex == 1) ? new Vector3(0f, 2.3f, -1.0f) : new Vector3(0, 1.0f, 1.5f);
    //    canvasObj.transform.localRotation = (selectedModelIndex == 1) ? Quaternion.Euler(-90f, 0, 0) : Quaternion.identity;
    //    canvasObj.transform.localScale = Vector3.one * 0.0025f;

    //    Debug.Log("[Canvas] Scale: " + canvasObj.transform.localScale);
    //    canvasObj.SetActive(false);

    //    Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
    //    Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");

    //    Button closeBtn = null;
    //    if (closeBtnTransform != null && closeBtnTransform.TryGetComponent(out closeBtn))
    //    {
    //        Debug.Log("[Canvas] Close button assigned successfully.");
    //    }
    //    else
    //    {
    //        Debug.LogError("[PlaneLocker] Close button not found or missing Button component!");
    //    }

    //    var tapHandler = finalModelInstance.GetComponent<MonoBehaviour>();

    //    if (tapHandler is MouseTapHandler mouseHandler)
    //    {
    //        mouseHandler.canvasObject = canvasObj;
    //        mouseHandler.backgroundPanelObject = backgroundPanel?.gameObject;
    //        mouseHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
    //        mouseHandler.closeButton = closeBtn;
    //        if (closeBtn != null) closeBtn.onClick.AddListener(mouseHandler.OnCloseButtonTapped);
    //    }
    //    else if (tapHandler is MonitorTapHandler monitorHandler)
    //    {
    //        monitorHandler.canvasObject = canvasObj;
    //        monitorHandler.backgroundPanelObject = backgroundPanel?.gameObject;
    //        monitorHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
    //        monitorHandler.closeButton = closeBtn;
    //        if (closeBtn != null) closeBtn.onClick.AddListener(monitorHandler.OnCloseButtonTapped);
    //    }
    //    else if (tapHandler is KeyboardTapHandler keyboardHandler)
    //    {
    //        keyboardHandler.canvasObject = canvasObj;
    //        keyboardHandler.backgroundPanelObject = backgroundPanel?.gameObject;
    //        keyboardHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
    //        keyboardHandler.closeButton = closeBtn;

    //        Transform keyGroup = finalModelInstance.transform.Find("KeyHighlightGroup");
    //        if (keyGroup != null)
    //        {
    //            keyboardHandler.keyHighlightGroup = keyGroup.gameObject;
    //            keyGroup.gameObject.SetActive(false);
    //        }

    //        if (closeBtn != null) closeBtn.onClick.AddListener(keyboardHandler.OnCloseButtonTapped);
    //    }
    //}
    public void ConfirmLockedArea()
    {
        if (lockedAreaVisual == null || selectedModelIndex == -1)
        {
            Debug.LogError("[PlaneLocker] Cannot confirm - no preview or model selected.");
            return;
        }

        lockedAreaReady = true;
        if (confirmButton != null)
            confirmButton.SetActive(false);

        lockedAreaVisual.SetActive(false);

        Vector3 spawnPosition = lockedAreaVisual.transform.position;
        spawnPosition.y += 0.01f;

        Vector3 lockedScale = lockedAreaVisual.transform.localScale;
        Quaternion spawnRotation;

        if (selectedModelIndex == 1) // Mouse
        {
            spawnRotation = lockedAreaVisual.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
        }
        else
        {
            //spawnRotation = Quaternion.identity; // Or your existing logic for keyboard, etc.
            spawnRotation = lockedAreaVisual.transform.rotation;
        }

        if (selectedModelIndex == 2)
        {
            spawnPosition.y += 0.2f;
            finalModelInstance = Instantiate(monitorRootPrefab, spawnPosition, spawnRotation); // use MonitorRoot prefab
        } 
        else
        {
            finalModelInstance = Instantiate(
            arModels[selectedModelIndex],
            spawnPosition,
            spawnRotation
        );
        }

        //finalModelInstance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        finalModelInstance.transform.localScale = lockedScale;
        // Add appropriate manipulator based on selected index
        if (selectedModelIndex == 0) // Keyboard
        {
            finalModelInstance.AddComponent<KeyboardManipulator>();
        }
        else if (selectedModelIndex == 1) // Mouse
        {
            finalModelInstance.AddComponent<MouseManipulator>();
        }
        else if (selectedModelIndex == 2) // Monitor
        {
            finalModelInstance.AddComponent<MonitorManipulator>();
        }
        else if (selectedModelIndex == 3) // Laptop
        {
            finalModelInstance.AddComponent<LaptopManipulator>();
        }
        else if (selectedModelIndex == 4) // Speaker
        {
            finalModelInstance.AddComponent<SpeakerManipulator>();
        }
        else
        {
            // Default manipulator for other models
            finalModelInstance.AddComponent<AreaManipulator>();
        }

        Debug.Log("[PlaneLocker] Model instantiated at: " + spawnPosition);

        GameObject canvasObj = Instantiate(arTextCanvasPrefabs[selectedModelIndex], finalModelInstance.transform);
        canvasObj.transform.localPosition = new Vector3(0, 1.0f, 1.5f); // x , y , z ? (z , y , x)
        //canvasObj.transform.localRotation = Quaternion.identity;
        canvasObj.transform.localScale = Vector3.one * 0.0025f;

        // Apply custom upright rotation only for mouse (index 1)
        if (selectedModelIndex == 1) // Mouse
        {
            //canvasObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
            //canvasObj.transform.Rotate(-180f, 0, 0);
            //canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.Rotate(-90f, 0, 0);
            canvasObj.transform.localPosition = new Vector3(0f, 2.3f, -1.0f); // z, x ,y
        }
        else if (selectedModelIndex == 2)
        {
            canvasObj.transform.localPosition = new Vector3(0f, 2.5f, 2.5f); // x, y, z
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * 0.005f;
        }
        else if (selectedModelIndex == 4)
        {
            canvasObj.transform.localPosition = new Vector3(0f, 4f, 2.5f); // x, y, z
            canvasObj.transform.localRotation = Quaternion.identity;
            canvasObj.transform.localScale = Vector3.one * 0.005f;
        }
        else
        {
            canvasObj.transform.localRotation = Quaternion.identity;
        }

        Debug.Log("[Canvas] Scale: " + canvasObj.transform.localScale);

        //Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");

        if (selectedModelIndex == 0)
        {
            Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
            var tapHandler = finalModelInstance.AddComponent<KeyboardTapHandler>();

            KeyboardManipulator manipulator = finalModelInstance.GetComponent<KeyboardManipulator>();

            if (rotateLeftButton != null && rotateRightButton != null && resetButton != null && manipulator != null)
            {
                rotateLeftButton.onClick.RemoveAllListeners();
                rotateRightButton.onClick.RemoveAllListeners();
                resetButton.onClick.RemoveAllListeners();

                rotateLeftButton.onClick.AddListener(manipulator.RotateLeft);
                rotateRightButton.onClick.AddListener(manipulator.RotateRight);
                resetButton.onClick.AddListener(manipulator.ResetModelTransform);
            }

            tapHandler.canvasObject = canvasObj;
            tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
            tapHandler.keyHighlightGroup = finalModelInstance.transform.Find("KeyHighlightGroup")?.gameObject;
            tapHandler.keyHighlightGroup?.SetActive(false);
            tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

            Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
            if (closeBtnTransform != null)
            {
                UnityEngine.UI.Button closeBtn = closeBtnTransform.GetComponent<UnityEngine.UI.Button>();
                if (closeBtn != null)
                {
                    tapHandler.closeButton = closeBtn;
                    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
                }
            }
        }
        else if (selectedModelIndex == 1) {
            Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
            var tapHandler = finalModelInstance.AddComponent<MouseTapHandler>();
            MouseManipulator manipulator = finalModelInstance.GetComponent<MouseManipulator>();

            if (rotateLeftButton != null && rotateRightButton != null && resetButton != null && manipulator != null)
            {
                rotateLeftButton.onClick.RemoveAllListeners();
                rotateRightButton.onClick.RemoveAllListeners();
                resetButton.onClick.RemoveAllListeners();

                rotateLeftButton.onClick.AddListener(manipulator.RotateLeft);
                rotateRightButton.onClick.AddListener(manipulator.RotateRight);
                resetButton.onClick.AddListener(manipulator.ResetModelTransform);
            }

            tapHandler.canvasObject = canvasObj;
            tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
            tapHandler.mouseHighlightGroup = finalModelInstance.transform.Find("KeyHighlightGroup")?.gameObject;
            tapHandler.mouseHighlightGroup?.SetActive(false);
            tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

            Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
            if (closeBtnTransform != null)
            {
                UnityEngine.UI.Button closeBtn = closeBtnTransform.GetComponent<UnityEngine.UI.Button>();
                if (closeBtn != null)
                {
                    tapHandler.closeButton = closeBtn;
                    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
                }
            }
        }
        else if (selectedModelIndex == 2) {
            Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
            var tapHandler = finalModelInstance.AddComponent<MonitorTapHandler>();
            MonitorManipulator manipulator = finalModelInstance.GetComponent<MonitorManipulator>();

            if (rotateLeftButton != null && rotateRightButton != null && resetButton != null && manipulator != null)
            {
                rotateLeftButton.onClick.RemoveAllListeners();
                rotateRightButton.onClick.RemoveAllListeners();
                resetButton.onClick.RemoveAllListeners();

                rotateLeftButton.onClick.AddListener(manipulator.RotateLeft);
                rotateRightButton.onClick.AddListener(manipulator.RotateRight);
                resetButton.onClick.AddListener(manipulator.ResetModelTransform);
            }

            tapHandler.descriptionAudio = keyboardDescriptionAudio;
            tapHandler.canvasObject = canvasObj;
            tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
            tapHandler.monitorHighlightGroup = finalModelInstance.transform.Find("MonitorPrefab/KeyHighlightGroup")?.gameObject;
            tapHandler.monitorHighlightGroup?.SetActive(false);
            tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

            Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
            if (closeBtnTransform != null)
            {
                UnityEngine.UI.Button closeBtn = closeBtnTransform.GetComponent<UnityEngine.UI.Button>();
                if (closeBtn != null)
                {
                    tapHandler.closeButton = closeBtn;
                    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
                }
            }
        }
        else if (selectedModelIndex == 3)
        {
            Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
            var tapHandler = finalModelInstance.AddComponent<LaptopTapHandler>();
            LaptopManipulator manipulator = finalModelInstance.GetComponent<LaptopManipulator>();

            if (rotateLeftButton != null && rotateRightButton != null && resetButton != null && manipulator != null)
            {
                rotateLeftButton.onClick.RemoveAllListeners();
                rotateRightButton.onClick.RemoveAllListeners();
                resetButton.onClick.RemoveAllListeners();

                rotateLeftButton.onClick.AddListener(manipulator.RotateLeft);
                rotateRightButton.onClick.AddListener(manipulator.RotateRight);
                resetButton.onClick.AddListener(manipulator.ResetModelTransform);
            }

            tapHandler.descriptionAudio = keyboardDescriptionAudio;
            tapHandler.canvasObject = canvasObj;
            tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
            tapHandler.laptopHighlightGroup = finalModelInstance.transform.Find("KeyHighlightGroup")?.gameObject;
            tapHandler.laptopHighlightGroup?.SetActive(false);
            tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

            Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
            if (closeBtnTransform != null)
            {
                UnityEngine.UI.Button closeBtn = closeBtnTransform.GetComponent<UnityEngine.UI.Button>();
                if (closeBtn != null)
                {
                    tapHandler.closeButton = closeBtn;
                    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
                }
            }
        }
        else if (selectedModelIndex == 4)
        {
            Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
            var tapHandler = finalModelInstance.AddComponent<SpeakerTapHandler>();
            SpeakerManipulator manipulator = finalModelInstance.GetComponent<SpeakerManipulator>();

            if (rotateLeftButton != null && rotateRightButton != null && resetButton != null && manipulator != null)
            {
                rotateLeftButton.onClick.RemoveAllListeners();
                rotateRightButton.onClick.RemoveAllListeners();
                resetButton.onClick.RemoveAllListeners();

                rotateLeftButton.onClick.AddListener(manipulator.RotateLeft);
                rotateRightButton.onClick.AddListener(manipulator.RotateRight);
                resetButton.onClick.AddListener(manipulator.ResetModelTransform);
            }

            tapHandler.descriptionAudio = keyboardDescriptionAudio;
            tapHandler.canvasObject = canvasObj;
            tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
            tapHandler.speakerHighlightGroup = finalModelInstance.transform.Find("KeyHighlightGroup")?.gameObject;
            tapHandler.speakerHighlightGroup?.SetActive(false);
            tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

            Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");
            if (closeBtnTransform != null)
            {
                UnityEngine.UI.Button closeBtn = closeBtnTransform.GetComponent<UnityEngine.UI.Button>();
                if (closeBtn != null)
                {
                    tapHandler.closeButton = closeBtn;
                    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
                }
            }
        }
        //var tapHandler = finalModelInstance.AddComponent<KeyboardTapHandler>();
        //tapHandler.descriptionAudio = keyboardDescriptionAudio;
        //tapHandler.canvasObject = canvasObj;
        //tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        //tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

        //    var tapHandler = finalModelInstance.GetComponent<MonoBehaviour>();

        //    if (tapHandler is MouseTapHandler mouseHandler)
        //    {
        //        mouseHandler.canvasObject = canvasObj;
        //        mouseHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        //        mouseHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
        //        //mouseHandler.closeButton = closeBtn;
        //    }
        //    else if (tapHandler is MonitorTapHandler monitorHandler)
        //    {
        //        monitorHandler.canvasObject = canvasObj;
        //        monitorHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        //        monitorHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
        //        //monitorHandler.closeButton = closeBtn;
        //    }
        //    else if (tapHandler is KeyboardTapHandler keyboardHandler)
        //    {
        //        keyboardHandler.canvasObject = canvasObj;
        //        keyboardHandler.backgroundPanelObject = backgroundPanel?.gameObject;
        //        keyboardHandler.descriptionAudio = modelAudioClips[selectedModelIndex];
        //        //keyboardHandler.closeButton = closeBtn;

        //        Transform keyGroup = finalModelInstance.transform.Find("KeyHighlightGroup");
        //        if (keyGroup != null)
        //        {
        //            keyboardHandler.keyHighlightGroup = keyGroup.gameObject;
        //            keyGroup.gameObject.SetActive(false);
        //        }
        //}

        //if (closeBtnTransform != null && closeBtnTransform.TryGetComponent(out Button closeBtn))
        //{
        //    tapHandler.closeButton = closeBtn;
        //    closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
        //}
        //else
        //{
        //    Debug.LogError("[PlaneLocker] Close button not found or missing Button component!");
        //}

        //Transform keyGroup = finalModelInstance.transform.Find("KeyHighlightGroup");
        //if (keyGroup != null)
        //{
        //    tapHandler.keyHighlightGroup = keyGroup.gameObject;
        //    keyGroup.gameObject.SetActive(false);
        //}
    }

    public void ToggleModelSelectionPanel()
    {
        if (modelSelectionPanel != null)
        {
            bool isActive = modelSelectionPanel.activeSelf;
            modelSelectionPanel.SetActive(!isActive);
            Debug.Log("[UI] Toggled model selection panel: " + (!isActive));
        }
    }

    public void CloseModelSelectionPanel()
    {
        if (modelSelectionPanel != null)
        {
            modelSelectionPanel.SetActive(false);
            Debug.Log("[UI] Manually closed model selection panel.");
        }
    }
}


//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.XR.ARFoundation;
//using UnityEngine.XR.ARSubsystems;
//using UnityEngine.UI;

//public class PlaneLocker : MonoBehaviour
//{
//    [Header("AR Managers")]
//    public ARRaycastManager raycastManager;
//    public ARPlaneManager planeManager;

//    [Header("UI")]
//    public GameObject confirmButton;
//    public GameObject modelSelectionPanel; // Panel with model buttons

//    [Header("Model Prefabs")]
//    public GameObject[] arModels;            // [keyboard, mouse, laptop, monitor, speaker]
//    public GameObject[] arModelPreviews;     // Matching preview models
//    public GameObject[] arTextCanvasPrefabs;    // matching info panels
//    public AudioClip[] modelAudioClips;         // optional

//    [Header("Materials")]
//    public Material ghostMaterial;

//    [Header("Audio/Text Description")]
//    public AudioClip keyboardDescriptionAudio;
//    [TextArea]
//    public string keyboardDescriptionText;

//    private ARPlane lockedPlane;
//    private GameObject lockedAreaVisual;
//    private GameObject finalModelInstance;
//    private int selectedModelIndex = -1;

//    private static List<ARRaycastHit> hits = new List<ARRaycastHit>();
//    private bool lockedAreaReady = false;

//    void Start()
//    {
//        if (modelSelectionPanel != null)
//            modelSelectionPanel.SetActive(false);
//    }

//    void Update()
//    {
//        if (lockedPlane != null || lockedAreaReady)
//            return;

//        if (Input.touchCount == 0)
//            return;

//        Touch touch = Input.GetTouch(0);

//        if (touch.phase == TouchPhase.Began)
//        {
//            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
//            {
//                var hit = hits[0];
//                Pose hitPose = hit.pose;
//                lockedPlane = hit.trackable as ARPlane;
//                LockPlane(lockedPlane, hitPose);
//            }
//        }
//    }

//    void LockPlane(ARPlane plane, Pose hitPose)
//    {
//        Debug.Log("[PlaneLocker] Plane locked: " + plane.trackableId);

//        planeManager.enabled = false;
//        foreach (var arPlane in planeManager.trackables)
//        {
//            if (arPlane != plane)
//                arPlane.gameObject.SetActive(false);
//        }

//        if (modelSelectionPanel != null)
//            modelSelectionPanel.SetActive(true);
//    }

//    public void SpawnPreviewForSelectedModel(int index)
//    {
//        selectedModelIndex = index;

//        if (arModelPreviews == null || index < 0 || index >= arModelPreviews.Length)
//        {
//            Debug.LogError("[PlaneLocker] Invalid model index.");
//            return;
//        }

//        if (lockedAreaVisual != null)
//        {
//            Destroy(lockedAreaVisual);
//            Debug.Log("[Model Selection] Destroyed previous preview model.");
//        }

//        if (finalModelInstance != null)
//        {
//            Destroy(finalModelInstance);
//            Debug.Log("[Model Selection] Destroyed previous confirmed model.");
//        }

//        string modelName = arModelPreviews[index].name;
//        Debug.Log($"[Model Selection] {modelName} model is selected.");

//        Vector3 previewPosition = lockedPlane.transform.position;
//        Quaternion previewRotation = Quaternion.identity;

//        lockedAreaVisual = Instantiate(arModelPreviews[index], previewPosition, previewRotation);
//        lockedAreaVisual.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

//        Vector3 cameraPosition = Camera.main.transform.position;
//        Vector3 lookDirection = cameraPosition - lockedAreaVisual.transform.position;
//        lookDirection.y = 0;
//        Quaternion faceCameraY = Quaternion.LookRotation(-lookDirection);
//        lockedAreaVisual.transform.rotation = faceCameraY * previewRotation;

//        Renderer[] renderers = lockedAreaVisual.GetComponentsInChildren<Renderer>();
//        foreach (Renderer r in renderers)
//        {
//            r.material = ghostMaterial;
//        }

//        lockedAreaVisual.AddComponent<AreaManipulator>();

//        if (confirmButton != null)
//            confirmButton.SetActive(true);

//        if (modelSelectionPanel != null)
//            modelSelectionPanel.SetActive(false);
//    }

//    public void ConfirmLockedArea()
//    {
//        if (lockedAreaVisual == null || selectedModelIndex == -1)
//        {
//            Debug.LogError("[PlaneLocker] Cannot confirm - no preview or model selected.");
//            return;
//        }

//        lockedAreaReady = true;
//        if (confirmButton != null)
//            confirmButton.SetActive(false);

//        lockedAreaVisual.SetActive(false);

//        Vector3 spawnPosition = lockedAreaVisual.transform.position;
//        spawnPosition.y += 0.01f;

//        Quaternion spawnRotation = lockedAreaVisual.transform.rotation;

//        finalModelInstance = Instantiate(arModels[selectedModelIndex], spawnPosition, spawnRotation);
//        finalModelInstance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

//        if (selectedModelIndex == 0) finalModelInstance.AddComponent<KeyboardManipulator>();
//        else if (selectedModelIndex == 1) finalModelInstance.AddComponent<MouseManipulator>();
//        else finalModelInstance.AddComponent<AreaManipulator>();

//        Debug.Log("[PlaneLocker] Model instantiated at: " + spawnPosition);

//        GameObject canvasObj = Instantiate(arTextCanvasPrefabs[selectedModelIndex], finalModelInstance.transform);
//        canvasObj.transform.localScale = Vector3.one * 0.0025f;

//        if (selectedModelIndex == 1)
//        {
//            canvasObj.transform.localRotation = Quaternion.identity;
//            canvasObj.transform.Rotate(-90f, 0, 0);
//            canvasObj.transform.localPosition = new Vector3(0f, 2.3f, -1.0f);
//        }
//        else
//        {
//            canvasObj.transform.localRotation = Quaternion.identity;
//            canvasObj.transform.localPosition = new Vector3(0, 1.0f, 1.5f);
//        }

//        Debug.Log("[Canvas] Scale: " + canvasObj.transform.localScale);

//        Transform backgroundPanel = canvasObj.transform.Find("BackgroundPanel");
//        Transform closeBtnTransform = canvasObj.transform.Find("BackgroundPanel/Button_Close");

//        var tapHandler = finalModelInstance.AddComponent<ModelTapHandler>();
//        tapHandler.canvasObject = canvasObj;
//        tapHandler.backgroundPanelObject = backgroundPanel?.gameObject;
//        tapHandler.descriptionAudio = modelAudioClips[selectedModelIndex];

//        if (closeBtnTransform != null && closeBtnTransform.TryGetComponent(out Button closeBtn))
//        {
//            tapHandler.closeButton = closeBtn;
//            closeBtn.onClick.AddListener(tapHandler.OnCloseButtonTapped);
//        }
//        else
//        {
//            Debug.LogError("[PlaneLocker] Close button not found or missing Button component!");
//        }

//        Transform keyGroup = finalModelInstance.transform.Find("KeyHighlightGroup");
//        if (keyGroup != null)
//        {
//            tapHandler.keyHighlightGroup = keyGroup.gameObject;
//            keyGroup.gameObject.SetActive(false);
//        }
//    }

//    public void ToggleModelSelectionPanel()
//    {
//        if (modelSelectionPanel != null)
//        {
//            bool isActive = modelSelectionPanel.activeSelf;
//            modelSelectionPanel.SetActive(!isActive);
//            Debug.Log("[UI] Toggled model selection panel: " + (!isActive));
//        }
//    }

//    public void CloseModelSelectionPanel()
//    {
//        if (modelSelectionPanel != null)
//        {
//            modelSelectionPanel.SetActive(false);
//            Debug.Log("[UI] Manually closed model selection panel.");
//        }
//    }
//}


