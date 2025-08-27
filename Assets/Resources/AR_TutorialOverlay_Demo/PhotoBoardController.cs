using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class PhotoBoardController : MonoBehaviour
{
    [Header("Assign MarkersRoot (child)")]
    public Transform markersRoot; // drag the MarkersRoot child here

    [Header("Board width in meters (height auto from aspect)")]
    public float widthMeters = 0.35f;
    public float heightMeters { get; private set; }

    MeshRenderer mr;
    Material mat;
    Texture2D tex;
    OverlayMarkerManager overlay;

    void Awake()
    {
        mr = GetComponent<MeshRenderer>();
        mat = new Material(Shader.Find("Unlit/Texture"));
        mr.material = mat;

        overlay = GetComponentInChildren<OverlayMarkerManager>();
        if (!overlay && markersRoot)
            overlay = markersRoot.gameObject.AddComponent<OverlayMarkerManager>();
    }

    public void SetTexture(Texture2D t, float widthM)
    {
        if (!t) { Debug.LogWarning("[PhotoBoard] Null texture"); return; }

        tex = t;
        widthMeters = Mathf.Max(0.05f, widthM);
        float aspect = (float)tex.width / tex.height;
        heightMeters = widthMeters / aspect;

        // Vertical poster: x=width, y=height, (z is thin)
        transform.localScale = new Vector3(widthMeters, heightMeters, 0.001f);
        mat.mainTexture = tex;

        // Quick visual check: one pin at center
        SpawnCenterPin();
    }

    void SpawnCenterPin()
    {
        if (!overlay) return;
        overlay.markersRoot = markersRoot ? markersRoot : transform;
        overlay.targetWidthM = widthMeters;
        overlay.targetHeightM = heightMeters;
        overlay.ClearAll();

        var center = new OverlayMarkerManager.NormRect
        {
            id = "pin_center",
            type = "pin",
            label = "",
            pos01 = new Vector2(0.5f, 0.5f),
            size01 = Vector2.zero
        };
        overlay.CreateMarker(center);
    }

    public void AnalyzeAndOverlay(OverlayMarkerManager.NormRect[] results)
    {
        if (!overlay || results == null || results.Length == 0) return;

        overlay.markersRoot = markersRoot ? markersRoot : transform;
        overlay.targetWidthM = widthMeters;
        overlay.targetHeightM = heightMeters;

        overlay.ClearAll();
        foreach (var r in results) overlay.CreateMarker(r);
    }

    public void FaceCamera()
    {
        if (!Camera.main) return;
        Vector3 fwd = Camera.main.transform.forward;
        fwd.y = 0f; fwd.Normalize();
        transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
    }
}
