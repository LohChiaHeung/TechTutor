using UnityEngine;

public class ArrowLockBinder : MonoBehaviour
{
    [Header("Refs")]
    public VuforiaImageSwipeSelector_ExistingChildren selector; // your swiper on ImageTarget
    public GameObject leftArrow;   // the 3D arrow object (has collider + ThreeDButton)
    public GameObject rightArrow;  // the other 3D arrow

    [Header("Visuals")]
    public bool dimWhenLocked = true;
    [Range(0f, 1f)] public float dimAlpha = 0.35f;

    // cached
    private Collider _leftCol, _rightCol;
    private Renderer[] _leftRenderers, _rightRenderers;

    void Awake()
    {
        if (leftArrow)
        {
            _leftCol = leftArrow.GetComponent<Collider>();
            _leftRenderers = leftArrow.GetComponentsInChildren<Renderer>(true);
        }
        if (rightArrow)
        {
            _rightCol = rightArrow.GetComponent<Collider>();
            _rightRenderers = rightArrow.GetComponentsInChildren<Renderer>(true);
        }

        // Auto-toggle with Choose / Unchoose (doesn't affect your audio)
        if (selector)
        {
            selector.OnModelChosen.AddListener(_ => LockArrows());
            selector.OnModelUnchosen.AddListener(_ => UnlockArrows());
        }

        // Initial state (unlocked by default)
        UnlockArrows();
    }

    public void LockArrows()
    {
        SetInteractable(false);
    }

    public void UnlockArrows()
    {
        SetInteractable(true);
    }

    // You can also wire these two directly to your own world-space Lock/Unlock buttons:
    // public void UI_Lock()   => LockArrows();
    // public void UI_Unlock() => UnlockArrows();

    private void SetInteractable(bool on)
    {
        if (_leftCol) _leftCol.enabled = on;
        if (_rightCol) _rightCol.enabled = on;

        if (dimWhenLocked)
        {
            float a = on ? 1f : dimAlpha;
            Tint(_leftRenderers, a);
            Tint(_rightRenderers, a);
        }
    }

    private void Tint(Renderer[] rends, float alpha)
    {
        if (rends == null) return;
        foreach (var r in rends)
        {
            if (!r || !r.material) continue;
            if (r.material.HasProperty("_Color"))
            {
                var c = r.material.color;
                c.a = alpha;
                r.material.color = c;
            }
        }
    }
}
