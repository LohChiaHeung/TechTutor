using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(Collider))]
public class DeskRepositionController : MonoBehaviour
{
    [Header("AR")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    [Header("Drag")]
    public bool enableDragToMove = true;
    public TrackableType dragRaycastMask = TrackableType.PlaneWithinPolygon;

    [Header("Nudge Buttons")]
    public float moveStep = 0.05f;
    public float holdRepeatPerSecond = 6f;

    [Header("Vertical Limits (optional)")]
    public bool clampVertical = true;
    public float minY = 0.0f;
    public float maxY = 1.5f;

    [Header("Scale Buttons")]
    [Tooltip("How much to scale per button press")]
    public float scaleStep = 0.05f; // 5% per tap
    public float minScale = 0.3f;   // 30% of original
    public float maxScale = 2.0f;   // 200% of original

    [Header("Drag Height")]
    public bool followPlaneHeight = true;   // keep glued to plane while dragging
    public float planeYOffset = 0.01f;      // tiny lift to avoid z-fighting

    [Header("Y Baseline")]
    public bool useBaselineY = true;        // if true, Y is controlled by baseline
    private float _baselinePlaneY;
    private bool _baselineInitialized = false;

    [Header("Scale Target")]
    public Transform scaleTarget; // assign your 3D model root (NOT the UI panel)
    

    private HashSet<Transform> _canvasAndDescendants;
    private List<Transform> _scaledBranches;                 // the children we scale
    private Dictionary<Transform, Vector3> _initBranchScales; // initial scales per branch
    private Vector3 _initScaleTarget;
    private bool _hasInitScaleTarget = false;
    private bool _scaleTargetLocked = false;   // don’t rebuild later


    public void SetBaselinePlaneY(float y)
    {
        _baselinePlaneY = y;
        _baselineInitialized = true;
    }

    private float _holdScaleDir = 0f; // -1 shrink, +1 enlarge

    private readonly List<ARRaycastHit> _hits = new();
    private bool _isDragging = false;
    private int _dragFingerId = -1;

    private float _holdDirX = 0f; // -1 left, +1 right
    private float _holdDirY = 0f; // -1 down, +1 up (affects baseline if useBaselineY)

    // preserve offset so object doesn't snap on first drag frame
    private Vector3 _dragOffsetXZ = Vector3.zero;

    //void Awake()
    //{
    //    if (!arCamera) arCamera = Camera.main;
    //    if (!raycastManager) raycastManager = FindObjectOfType<ARRaycastManager>();
    //    if (!scaleTarget) scaleTarget = transform; // default

    //    if (_canvasAndDescendants == null) BuildCanvasSet();
    //    if (_scaledBranches == null) BuildScaledBranches();
    //    float factor = 1f + (dir * scaleStep);
    //    foreach (var child in _scaledBranches)
    //    {
    //        Vector3 newScale = child.localScale * factor;
    //        float min = Mathf.Max(0.01f, minScale);
    //        float max = Mathf.Max(min, maxScale);
    //        newScale.x = Mathf.Clamp(newScale.x, min, max);
    //        newScale.y = Mathf.Clamp(newScale.y, min, max);
    //        newScale.z = Mathf.Clamp(newScale.z, min, max);
    //        child.localScale = newScale;
    //    }

    //    ReplantFootToBaseline();
    //    ApplyMove(transform.position);
    //    if (_initBranchScales == null) CaptureInitialScales(); // optional safety

    //}

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
        if (!raycastManager) raycastManager = FindObjectOfType<ARRaycastManager>();

        if (_canvasAndDescendants == null) BuildCanvasSet();
        EnsureScaleTarget(); // <-- important

        // (do NOT call BuildScaledBranches anymore; we won’t use per-branch scaling)
    }




    void Update()
    {
        HandleDrag();

        // Hold-to-repeat horizontal, flattened on XZ so height never changes
        if (_holdDirX != 0f)
        {
            Vector3 camRight = arCamera ? arCamera.transform.right : Vector3.right;
            camRight = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized; // <- flatten Y
            Vector3 delta = camRight * (_holdDirX * moveStep * holdRepeatPerSecond * Time.deltaTime);
            ApplyMove(transform.position + delta); // Y will be forced to baseline in ApplyMove
        }

        //if (_holdDirY != 0f)
        //{
        //    if (useBaselineY)
        //    {
        //        if (!_baselineInitialized)
        //        {
        //            _baselinePlaneY = transform.position.y - planeYOffset;
        //            _baselineInitialized = true;
        //        }
        //        _baselinePlaneY += (_holdDirY * moveStep * holdRepeatPerSecond * Time.deltaTime);
        //        ApplyMove(transform.position);
        //    }
        //    else
        //    {
        //        Vector3 delta = Vector3.up * (_holdDirY * moveStep * holdRepeatPerSecond * Time.deltaTime);
        //        ApplyMove(transform.position + delta);
        //    }
        //}

        //if (_holdScaleDir != 0f)
        //{
        //    if (!scaleTarget) scaleTarget = transform;

        //    float factor = 1f + (_holdScaleDir * scaleStep * holdRepeatPerSecond * Time.deltaTime);
        //    Vector3 newScale = scaleTarget.localScale * factor;

        //    float min = Mathf.Max(0.01f, minScale);
        //    float max = Mathf.Max(min, maxScale);
        //    newScale.x = Mathf.Clamp(newScale.x, min, max);
        //    newScale.y = Mathf.Clamp(newScale.y, min, max);
        //    newScale.z = Mathf.Clamp(newScale.z, min, max);

        //    scaleTarget.localScale = newScale;

        //    ReplantFootToBaseline();
        //    ApplyMove(transform.position);
        //}

        //if (_holdScaleDir != 0f)
        //{
        //    if (_scaledBranches == null) BuildScaledBranches();

        //    float factor = 1f + (_holdScaleDir * scaleStep * holdRepeatPerSecond * Time.deltaTime);
        //    foreach (var child in _scaledBranches)
        //    {
        //        Vector3 newScale = child.localScale * factor;
        //        float min = Mathf.Max(0.01f, minScale);
        //        float max = Mathf.Max(min, maxScale);
        //        newScale.x = Mathf.Clamp(newScale.x, min, max);
        //        newScale.y = Mathf.Clamp(newScale.y, min, max);
        //        newScale.z = Mathf.Clamp(newScale.z, min, max);
        //        child.localScale = newScale;
        //    }

        //    ReplantFootToBaseline_ForScaledBranches();
        //    ApplyMove(transform.position);
        //}


        if (_holdScaleDir != 0f)
        {
            EnsureScaleTarget();
            if (!scaleTarget) { _holdScaleDir = 0f; }
            else
            {
                float factor = 1f + (_holdScaleDir * scaleStep * holdRepeatPerSecond * Time.deltaTime);
                Vector3 newScale = scaleTarget.localScale * factor;

                float min = Mathf.Max(0.01f, minScale);
                float max = Mathf.Max(min, maxScale);
                newScale.x = Mathf.Clamp(newScale.x, min, max);
                newScale.y = Mathf.Clamp(newScale.y, min, max);
                newScale.z = Mathf.Clamp(newScale.z, min, max);

                scaleTarget.localScale = newScale;

                ReplantFootToBaseline_Using(scaleTarget);
                ApplyMove(transform.position);
            }
        }






        // Hold-to-repeat scaling
        //if (_holdScaleDir != 0f)
        //{
        //    float factor = 1f + (_holdScaleDir * scaleStep * holdRepeatPerSecond * Time.deltaTime);
        //    Vector3 newScale = transform.localScale * factor;
        //    newScale = ClampScale(newScale);
        //    transform.localScale = newScale;
        //}
    }

    private void ReplantFootToBaseline_Using(Transform target)
    {
        if (!useBaselineY || target == null) return;

        float targetBottomY = _baselinePlaneY + planeYOffset;

        var rends = target.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float dy = targetBottomY - b.min.y;
        transform.position += new Vector3(0f, dy, 0f);
    }


    private void BuildCanvasSet()
    {
        _canvasAndDescendants = new HashSet<Transform>();
        var canvases = GetComponentsInChildren<Canvas>(true);
        foreach (var c in canvases)
            foreach (var t in c.GetComponentsInChildren<Transform>(true))
                _canvasAndDescendants.Add(t);
    }

    private void BuildScaledBranches()
    {
        if (_canvasAndDescendants == null) BuildCanvasSet();
        _scaledBranches = new List<Transform>();
        foreach (Transform child in transform)
            if (!_canvasAndDescendants.Contains(child))
                _scaledBranches.Add(child);
    }



    public void CaptureInitialScales() // call this right after placement
    {
        EnsureScaleTarget();
        if (scaleTarget)
        {
            _initScaleTarget = scaleTarget.localScale;
            _hasInitScaleTarget = true;
        }
    }

    public void ResetScaledBranches() // keep method name for your panel
    {
        EnsureScaleTarget();
        if (scaleTarget && _hasInitScaleTarget)
            scaleTarget.localScale = _initScaleTarget;

        ReplantFootToBaseline_Using(scaleTarget);
        ApplyMove(transform.position);
    }



    // Ensure we have a single model root that excludes the Canvas
    private void EnsureScaleTarget()
    {
        if (_scaleTargetLocked && scaleTarget) return;

        if (_canvasAndDescendants == null) BuildCanvasSet();

        // If you already have a good model root, assign it in the Inspector to `scaleTarget`
        // Otherwise, build a container and move non-Canvas children under it.
        if (!scaleTarget)
        {
            var container = new GameObject("ScaleContainer").transform;
            container.SetParent(transform, false); // zero/identity under root

            var toMove = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child == container) continue;
                if (_canvasAndDescendants != null && _canvasAndDescendants.Contains(child)) continue; // skip Canvas
                toMove.Add(child);
            }
            foreach (var t in toMove) t.SetParent(container, true); // keep world pose
            scaleTarget = container;
        }

        // Make sure container has a neutral local scale (so captured scale is meaningful)
        scaleTarget.localScale = Vector3.one;

        _scaleTargetLocked = true;
    }



    //public void ResetScaledBranches()
    //{
    //    if (_initBranchScales == null || _scaledBranches == null) return;
    //    foreach (var t in _scaledBranches)
    //        if (_initBranchScales.TryGetValue(t, out var s))
    //            t.localScale = s;

    //    // keep the model planted at the plane height
    //    ReplantFootToBaseline();
    //    ApplyMove(transform.position);
    //}

    private void HandleDrag()
    {
        if (!enableDragToMove || raycastManager == null || arCamera == null)
            return;

        if (Input.touchCount == 0)
        {
            _isDragging = false;
            _dragFingerId = -1;
            return;
        }

        Touch touch = default;

        if (_isDragging)
        {
            bool found = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                if (t.fingerId == _dragFingerId) { touch = t; found = true; break; }
            }
            if (!found) { _isDragging = false; _dragFingerId = -1; return; }
        }
        else
        {
            touch = Input.GetTouch(0);
        }

        // Ignore touches over UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (!_isDragging && touch.phase == TouchPhase.Began)
        {
            Ray ray = arCamera.ScreenPointToRay(touch.position);
            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    _isDragging = true;
                    _dragFingerId = touch.fingerId;

                    // Capture plane point under finger to compute offset (prevents snap)
                    _hits.Clear();
                    if (raycastManager.Raycast(touch.position, _hits, dragRaycastMask) && _hits.Count > 0)
                    {
                        var p = _hits[0].pose.position;
                        _dragOffsetXZ = new Vector3(transform.position.x - p.x, 0f, transform.position.z - p.z);
                    }
                    else
                    {
                        _dragOffsetXZ = Vector3.zero;
                    }
                }
            }
        }
        else if (_isDragging && (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
        {
            if (raycastManager.Raycast(touch.position, _hits, dragRaycastMask) && _hits.Count > 0)
            {
                Pose pose = _hits[0].pose;

                // Apply preserved offset so it doesn't jump to the ray point
                Vector3 basePos = pose.position + _dragOffsetXZ;

                Vector3 target;
                if (followPlaneHeight)
                {
                    float y = useBaselineY ? (_baselinePlaneY + planeYOffset)
                                           : (pose.position.y + planeYOffset);
                    target = new Vector3(basePos.x, y, basePos.z);
                }
                else
                {
                    target = new Vector3(basePos.x, transform.position.y, basePos.z);
                }

                ApplyMove(target);
            }
        }
        else if (_isDragging && (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
        {
            _isDragging = false;
            _dragFingerId = -1;
        }
    }

    private Bounds GetWorldBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return new Bounds(root.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // Keep the model's bottom on the plane (same baseline logic as your movement fixes)
    private void ReplantFootToBaseline()
    {
        if (!useBaselineY) return;

        float targetBottomY = _baselinePlaneY + planeYOffset;   // the same baseline you use for move
        Bounds after = GetWorldBounds(scaleTarget ? scaleTarget : transform);

        float dy = targetBottomY - after.min.y;                 // how much the foot moved due to scale
        transform.position += new Vector3(0f, dy, 0f);          // nudge root back so foot stays planted
    }

    private Transform FindDefaultScaleTarget()
    {
        // prefer a child that contains renderers (meshes), but is NOT a Canvas
        var canvases = GetComponentsInChildren<Canvas>(true);
        var canvasRoots = new HashSet<Transform>();
        foreach (var c in canvases) canvasRoots.Add(c.transform);

        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            // skip anything that is under a canvas
            Transform t = r.transform;
            bool underCanvas = false;
            while (t != null)
            {
                if (canvasRoots.Contains(t)) { underCanvas = true; break; }
                t = t.parent;
            }
            if (!underCanvas) return r.transform.root == transform ? r.transform : r.transform; // good enough
        }
        return null;
    }


    private void ApplyMove(Vector3 targetPos)
    {
        if (useBaselineY)
        {
            if (!_baselineInitialized)
            {
                // auto-initialize from current pose once (prevents first-jump)
                _baselinePlaneY = transform.position.y - planeYOffset;
                _baselineInitialized = true;
            }
            targetPos.y = _baselinePlaneY + planeYOffset;
        }

        // keep this only for the non-baseline mode
        if (clampVertical && !useBaselineY)
            targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        transform.position = targetPos;
    }


    // Public hooks for buttons
    public void NudgeLeft() { MoveHorizontal(-1f); }
    public void NudgeRight() { MoveHorizontal(+1f); }
    public void NudgeUp() { MoveVertical(+1f); }
    public void NudgeDown() { MoveVertical(-1f); }

    public void NudgeLeft_HoldStart() { _holdDirX = -1f; }
    public void NudgeRight_HoldStart() { _holdDirX = +1f; }
    public void NudgeUp_HoldStart() { _holdDirY = +1f; }
    public void NudgeDown_HoldStart() { _holdDirY = -1f; }
    public void Any_HoldEnd() { _holdDirX = 0f; _holdDirY = 0f; }

    public void SetMoveStep(float meters) { moveStep = Mathf.Max(0.001f, meters); }

    private void MoveHorizontal(float dir)
    {
        // Flatten camera right on XZ so height never changes
        Vector3 camRight = arCamera ? arCamera.transform.right : Vector3.right;
        camRight = Vector3.ProjectOnPlane(camRight, Vector3.up).normalized;
        ApplyMove(transform.position + camRight * (dir * moveStep));
    }

    private void MoveVertical(float dir)
    {
        if (useBaselineY)
        {
            if (!_baselineInitialized)
            {
                _baselinePlaneY = transform.position.y - planeYOffset;
                _baselineInitialized = true;
            }
            _baselinePlaneY += dir * moveStep;
            ApplyMove(transform.position); // re-apply at new baseline Y
        }
        else
        {
            ApplyMove(transform.position + Vector3.up * (dir * moveStep));
        }
    }



    // Scale hooks
    public void Enlarge() { ApplyScale(1f); }
    public void Shrink() { ApplyScale(-1f); }

    public void Enlarge_HoldStart() { _holdScaleDir = 1f; }
    public void Shrink_HoldStart() { _holdScaleDir = -1f; }
    public void Scale_HoldEnd() { _holdScaleDir = 0f; }

    // Helpers
    //private void ApplyScale(float dir)
    //{
    //    float factor = 1f + (dir * scaleStep);
    //    Vector3 newScale = transform.localScale * factor;
    //    newScale = ClampScale(newScale);
    //    transform.localScale = newScale;
    //}

    private Bounds GetWorldBoundsForScaledBranches()
    {
        if (_canvasAndDescendants == null) BuildCanvasSet();
        Bounds b = new Bounds(transform.position, Vector3.zero);
        bool hasAny = false;

        // Use all renderers that are NOT under a Canvas
        var rends = GetComponentsInChildren<Renderer>(true);
        foreach (var r in rends)
        {
            bool underCanvas = false;
            for (Transform t = r.transform; t != null; t = t.parent)
            {
                if (_canvasAndDescendants.Contains(t)) { underCanvas = true; break; }
            }
            if (underCanvas) continue;

            if (!hasAny) { b = r.bounds; hasAny = true; }
            else b.Encapsulate(r.bounds);
        }
        if (!hasAny) return new Bounds(transform.position, Vector3.zero);
        return b;
    }

    // Use the branches’ bounds, not scaleTarget/transform
    private void ReplantFootToBaseline_ForScaledBranches()
    {
        if (!useBaselineY) return;

        float targetBottomY = _baselinePlaneY + planeYOffset;
        Bounds after = GetWorldBoundsForScaledBranches();

        float dy = targetBottomY - after.min.y;
        transform.position += new Vector3(0f, dy, 0f);
    }


    //private void ApplyScale(float dir)
    //{
    //    if (_scaledBranches == null) BuildScaledBranches();

    //    float factor = 1f + (dir * scaleStep);

    //    foreach (var child in _scaledBranches)
    //    {
    //        Vector3 newScale = child.localScale * factor;

    //        float min = Mathf.Max(0.01f, minScale);
    //        float max = Mathf.Max(min, maxScale);
    //        newScale.x = Mathf.Clamp(newScale.x, min, max);
    //        newScale.y = Mathf.Clamp(newScale.y, min, max);
    //        newScale.z = Mathf.Clamp(newScale.z, min, max);

    //        child.localScale = newScale;
    //    }

    //    ReplantFootToBaseline_ForScaledBranches();
    //    ApplyMove(transform.position);
    //}

    private void ApplyScale(float dir)
    {
        EnsureScaleTarget();
        if (!scaleTarget) return;

        float factor = 1f + (dir * scaleStep);
        Vector3 newScale = scaleTarget.localScale * factor;

        float min = Mathf.Max(0.01f, minScale);
        float max = Mathf.Max(min, maxScale);
        newScale.x = Mathf.Clamp(newScale.x, min, max);
        newScale.y = Mathf.Clamp(newScale.y, min, max);
        newScale.z = Mathf.Clamp(newScale.z, min, max);

        scaleTarget.localScale = newScale;

        ReplantFootToBaseline_Using(scaleTarget);
        ApplyMove(transform.position);
    }






    private Vector3 ClampScale(Vector3 scale)
    {
        float min = Mathf.Max(0.01f, minScale);
        float max = Mathf.Max(min, maxScale);
        scale.x = Mathf.Clamp(scale.x, min, max);
        scale.y = Mathf.Clamp(scale.y, min, max);
        scale.z = Mathf.Clamp(scale.z, min, max);
        return scale;
    }
}
