using UnityEngine;
using Vuforia;
using System;
using System.Security.Cryptography;

public class VuforiaOneOverlayDemo : MonoBehaviour
{
    [Header("Overlay to spawn on detect (e.g., PinArrow prefab)")]
    public GameObject overlayPrefab;

    [Header("Optional UI")]
    public TMPro.TMP_InputField statusText;

    Texture2D pickedTex;
    GameObject targetGo;
    float targetWidthM = 0.30f; // physical width (meters) you want to assume for the image

    void Log(string msg) { if (statusText) statusText.text = msg; Debug.Log(msg); }

    // Call this from a UI button: "Pick Image"
    public void PickImage()
    {
#if UNITY_ANDROID || UNITY_IOS
        NativeGallery.GetImageFromGallery(path =>
        {
            if (path == null) return;
            var bytes = System.IO.File.ReadAllBytes(path);
            pickedTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            pickedTex.LoadImage(bytes);
            Log($"[Pick] {pickedTex.width}x{pickedTex.height}");
        }, "Select a screenshot", "image/*");
#else
        // Editor fallback: put a PNG named 'sampleshot.png' under Assets/Resources/
        pickedTex = Resources.Load<Texture2D>("sampleshot");
        Log(pickedTex ? $"[Pick] Loaded Resources/sampleshot ({pickedTex.width}x{pickedTex.height})" : "[Pick] No Resources/sampleshot.png");
#endif
    }

    // Call this from a UI button: "Start AR"
    public void StartAR()
    {
        if (!pickedTex) { Log("No image picked."); return; }

        var vf = VuforiaBehaviour.Instance?.ObserverFactory;
        if (vf == null) { Log("Vuforia ObserverFactory missing."); return; }

        // Create a runtime Image Target from the Texture2D
        string hash = ComputeSha256(pickedTex.EncodeToPNG());
        var imageTarget = vf.CreateImageTarget(pickedTex, targetWidthM, "UserTarget_" + hash);
        targetGo = imageTarget.gameObject;
        Log("[AR] Runtime target created. Point your camera at the same image.");

        // Hook Vuforia’s status events
        var doh = targetGo.GetComponent<DefaultObserverEventHandler>();
        if (!doh) doh = targetGo.AddComponent<DefaultObserverEventHandler>();

        var forwarder = targetGo.AddComponent<VuforiaTargetForwarder>();
        forwarder.onFound += OnTargetFound;
        forwarder.onLost += OnTargetLost;
    }

    void OnTargetFound(ObserverBehaviour obs)
    {
        Log("[AR] Target FOUND");

        // Spawn ONE overlay at the CENTER of the tracked image
        // (local origin (0,0,0) is the image center; X=right, Y=up, Z=forward (usually))
        var overlay = Instantiate(overlayPrefab, obs.transform);
        overlay.transform.localPosition = Vector3.zero;     // center of the image
        overlay.transform.localRotation = Quaternion.identity;

        // Optional: offset slightly forward so it doesn’t Z-fight the image plane
        overlay.transform.localPosition += new Vector3(0f, 0.001f, 0f);
    }

    void OnTargetLost(ObserverBehaviour obs)
    {
        Log("[AR] Target LOST");
        // (You can destroy overlay here if you want to hide it)
        // foreach (Transform child in obs.transform) Destroy(child.gameObject);
    }

    static string ComputeSha256(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}

// Helper: forwards Vuforia callbacks so we can subscribe easily
public class VuforiaTargetForwarder : MonoBehaviour
{
    public event Action<ObserverBehaviour> onFound, onLost;
    DefaultObserverEventHandler handler;
    ObserverBehaviour obs;

    void Awake()
    {
        obs = GetComponent<ObserverBehaviour>();
        handler = GetComponent<DefaultObserverEventHandler>();
        if (!handler) handler = gameObject.AddComponent<DefaultObserverEventHandler>();
    }
    void OnEnable()
    {
        handler.OnTargetFound.AddListener(() => onFound?.Invoke(obs));
        handler.OnTargetLost.AddListener(() => onLost?.Invoke(obs));
    }
    void OnDisable()
    {
        handler.OnTargetFound.RemoveAllListeners();
        handler.OnTargetLost.RemoveAllListeners();
    }
}
