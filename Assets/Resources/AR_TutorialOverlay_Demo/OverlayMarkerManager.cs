using UnityEngine;

public class OverlayMarkerManager : MonoBehaviour
{
    [System.Serializable] public struct NormRect { public string id; public Vector2 pos01; public Vector2 size01; public string type; public string label; }

    [Header("Physical size (meters) of the tracked image")]
    public float targetWidthM = 0.30f;
    public float targetHeightM = 0.17f;

    [Header("Prefabs")]
    public RectHighlight rectPrefab;
    public Transform pinPrefab;
    public Transform markersRoot;

    public void ClearAll()
    {
        if (!markersRoot) markersRoot = transform;
        for (int i = markersRoot.childCount - 1; i >= 0; --i)
            Destroy(markersRoot.GetChild(i).gameObject);
    }

    public void CreateMarker(NormRect r)
    {
        if (!markersRoot) markersRoot = transform;

        float x = (r.pos01.x - 0.5f) * targetWidthM;
        float z = (r.pos01.y - 0.5f) * targetHeightM;

        if (r.type == "box")
        {
            var hl = Instantiate(rectPrefab, markersRoot);
            hl.transform.localPosition = new Vector3(x, 0f, z);
            hl.transform.localRotation = Quaternion.identity;
            hl.SetSize(r.size01.x * targetWidthM, r.size01.y * targetHeightM);
            hl.SetLabel(r.label);
        }
        else // "pin"
        {
            var pin = Instantiate(pinPrefab, markersRoot);
            pin.transform.localPosition = new Vector3(x, 0f, z);
            pin.transform.localRotation = Quaternion.identity;
        }
    }
}
