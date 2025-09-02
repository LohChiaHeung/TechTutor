using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ARTutorialBoard : MonoBehaviour
{
    [Header("Refs")]
    public Renderer boardRenderer;       // MeshRenderer on ScreenshotBoard
    public Canvas canvas;                // World-space canvas
    public RectTransform panelRect;      // CalloutPanel
    public TextMeshProUGUI titleText;

    [Header("Layout")]
    public float defaultWidthMeters = 0.7f;   // initial board width in meters
    public float panelOffsetMeters = 0.06f;   // gap above the board top

    Transform boardTransform;

    void Awake()
    {
        if (canvas) canvas.worldCamera = Camera.main;
        if (panelRect)
        {
            // reasonable panel size in pixels; scale is handled by canvas (0.001)
            panelRect.sizeDelta = new Vector2(600f, 260f);
        }

        // find the board transform from the renderer
        boardTransform = boardRenderer ? boardRenderer.transform : transform;
    }

    /// <summary>
    /// Initialize the board with a screenshot and optional texts.
    /// </summary>
    //public void Setup(Texture2D screenshot, string title, string body, float widthMeters = -1f)
    //{
    //    if (widthMeters <= 0f) widthMeters = defaultWidthMeters;

    //    // 1) Material (Unlit/Texture) with the screenshot
    //    if (boardRenderer)
    //    {
    //        var shader = Shader.Find("Unlit/Texture");
    //        var mat = new Material(shader);
    //        mat.mainTexture = screenshot;
    //        boardRenderer.material = mat;
    //    }

    //    // 2) Scale the quad to match the screenshot aspect (quad is 1x1 in local X/Y)
    //    float aspect = 16f / 9f;
    //    if (screenshot && screenshot.width > 0 && screenshot.height > 0)
    //        aspect = (float)screenshot.width / screenshot.height;

    //    float w = widthMeters;
    //    float h = widthMeters / aspect; // if image is wide, height becomes smaller
    //    boardTransform.localScale = new Vector3(w, h, 1f);

    //    // 3) Texts
    //    if (titleText) titleText.text = string.IsNullOrWhiteSpace(title) ? "Step 1" : title;

    //    // 4) Place panel above top edge of the board (in world-space)
    //    PlacePanelAboveBoard();

    //    // 5) Face camera (keep upright)
    //    FaceCameraYawOnly();
    //}

    public void Setup(Texture2D screenshot, string title, float widthMeters = -1f)
    {
        if (widthMeters <= 0f) widthMeters = defaultWidthMeters;

        // 1) unlit material
        if (boardRenderer)
        {
            var shader = Shader.Find("Unlit/Texture");
            var mat = new Material(shader);
            mat.mainTexture = screenshot;
            boardRenderer.material = mat;
        }

        // 2) scale quad to aspect
        float aspect = (screenshot && screenshot.height > 0)
                       ? (float)screenshot.width / screenshot.height
                       : 16f / 9f;

        float w = widthMeters;
        float h = w / aspect;
        boardRenderer.transform.localScale = new Vector3(w, h, 1f);

        SyncCanvasToBoard();
        EnsureWorldCanvas();
        // 3) sync canvas to quad (so overlays align)
        SyncCanvasToBoard();

        // 4) title only
        if (titleText) titleText.text = string.IsNullOrWhiteSpace(title) ? "Step 1" : title;

        // 5) place panel + face camera
        PlacePanelAboveBoard();
        FaceCameraYawOnly();

        var canvasRT = (RectTransform)canvas.transform;
        if (canvasRT.localScale.x == 0f || canvasRT.localScale.y == 0f || canvasRT.localScale.z == 0f)
        {
            canvasRT.localScale = Vector3.one;
        }
        Debug.Log($"[ARTutorialBoard] Canvas sizeDelta={canvasRT.sizeDelta} scale={canvasRT.localScale} sorting={canvas.sortingOrder} (should see overlay now)");
    }
    void SyncCanvasToBoard()
    {
        if (!canvas || !boardRenderer) return;
        var canvasRT = (RectTransform)canvas.transform;

        // world meters → UI pixels using canvas scale (expect 0.001)
        float s = canvasRT.localScale.x;
        var b = boardRenderer.transform.localScale; // meters
        canvasRT.sizeDelta = new Vector2(b.x / s, b.y / s);

        // keep it slightly in front of the quad
        canvasRT.localPosition = new Vector3(0f, 0f, 0.001f);
        canvasRT.localRotation = Quaternion.identity;
    }

    void EnsureWorldCanvas()
    {
        if (!canvas) return;
        canvas.worldCamera = Camera.main;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1000; // render above everything
    }

    //void SyncCanvasToBoard()
    //{
    //    if (!canvas) return;
    //    var canvasRT = (RectTransform)canvas.transform;
    //    float s = canvasRT.localScale.x; // expect 0.001
    //    float boardW = boardRenderer.transform.localScale.x;
    //    float boardH = boardRenderer.transform.localScale.y;
    //    canvasRT.sizeDelta = new Vector2(boardW / s, boardH / s);
    //    canvasRT.localPosition = new Vector3(0f, 0f, 0.001f);
    //    canvasRT.localRotation = Quaternion.identity;
    //}


    void PlacePanelAboveBoard()
    {
        if (!panelRect || !boardTransform) return;

        // top-center of the board in local space (quad lies on X/Y, forward +Z)
        float halfH = boardTransform.localScale.y * 0.5f;
        Vector3 topCenterLocal = new Vector3(0f, halfH, 0f);

        // convert to world, then offset upwards a bit
        Vector3 worldTop = boardTransform.TransformPoint(topCenterLocal);
        Vector3 up = Vector3.up;
        panelRect.position = worldTop + up * panelOffsetMeters;

        // Make panel face the camera nicely
        if (Camera.main)
        {
            Vector3 toCam = Camera.main.transform.position - panelRect.position;
            toCam.y = 0f; // keep upright
            if (toCam.sqrMagnitude > 0.0001f)
                panelRect.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }
    }

    public void FaceCameraYawOnly()
    {
        if (!Camera.main) return;
        Vector3 fwd = Camera.main.transform.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(fwd.normalized, Vector3.up);
    }
}
