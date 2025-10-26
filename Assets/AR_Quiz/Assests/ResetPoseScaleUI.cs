using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResetPoseScaleUI : MonoBehaviour
{
    [Header("Model Root")]
    [Tooltip("Parent that holds your 3D model choices (usually the ImageTarget). We'll scan its children and treat the ACTIVE one as the current model.")]
    public Transform modelParent;

    [Header("Buttons")]
    public Button btnResetPose;   // assign your 'Reset Pose' button
    public Button btnResetScale;  // assign your 'Reset Scale' button

    // Store the original local transform for each child model
    struct PoseScale { public Vector3 pos; public Quaternion rot; public Vector3 scale; }
    private readonly Dictionary<Transform, PoseScale> _defaults = new Dictionary<Transform, PoseScale>();

    private Transform _current;  // track which model is currently active

    void Start()
    {
        if (btnResetPose) btnResetPose.onClick.AddListener(ResetPose);
        if (btnResetScale) btnResetScale.onClick.AddListener(ResetScale);

        // Capture defaults for all immediate children under modelParent
        CaptureDefaultsForAll();
        // Track the initially active child
        _current = GetActiveChild();
    }

    void Update()
    {
        // If user swiped to a different model, update current reference (defaults already captured)
        var active = GetActiveChild();
        if (active != _current) _current = active;
    }

    // ---- Buttons ----
    public void ResetPose()
    {
        var t = _current; if (!t) t = GetActiveChild();
        if (!t) return;
        if (!_defaults.TryGetValue(t, out var d)) return;

        t.localPosition = d.pos;
        t.localRotation = d.rot;
    }

    public void ResetScale()
    {
        var t = _current; if (!t) t = GetActiveChild();
        if (!t) return;
        if (!_defaults.TryGetValue(t, out var d)) return;

        t.localScale = d.scale;
    }

    // ---- Helpers ----
    private void CaptureDefaultsForAll()
    {
        if (!modelParent) return;
        _defaults.Clear();
        for (int i = 0; i < modelParent.childCount; i++)
        {
            var t = modelParent.GetChild(i);
            // store each child model's original local transform
            _defaults[t] = new PoseScale
            {
                pos = t.localPosition,
                rot = t.localRotation,
                scale = t.localScale
            };
        }
    }

    private Transform GetActiveChild()
    {
        if (!modelParent) return null;
        // We assume exactly one child is set active at a time by your swiper.
        for (int i = 0; i < modelParent.childCount; i++)
        {
            var t = modelParent.GetChild(i);
            if (t.gameObject.activeInHierarchy) return t;
        }
        return null;
    }
}
