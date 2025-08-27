using UnityEngine;

public class CanvasAdjuster : MonoBehaviour
{
    [Header("Target Canvas RectTransform")]
    public RectTransform target;

    [Header("Move step in meters (local space)")]
    public float step = 0.02f;

    [Header("Optional: Adjust Panel (hide on Done/Reset)")]
    public GameObject adjustPanel;

    private Vector3 initialLocalPos; // original spawn position

    void Awake()
    {
        if (!target) target = GetComponent<RectTransform>();
        initialLocalPos = target.localPosition; // remember spawn/original
    }

    // ---------- Buttons (nudges + reset + done) ----------
    public void NudgeLeft() => NudgeLocal(-step, 0f);
    public void NudgeRight() => NudgeLocal(step, 0f);
    public void NudgeUp() => NudgeLocal(0f, step);
    public void NudgeDown() => NudgeLocal(0f, -step);

    public void ResetPos()
    {
        target.localPosition = initialLocalPos;
        HidePanel();
        Debug.Log($"[CanvasAdjuster] Reset to initial: {initialLocalPos}");
    }

    // Call this from your “Close/Done” button — it just hides the panel.
    public void Done()
    {
        HidePanel();
        Debug.Log("[CanvasAdjuster] Done — panel hidden.");
    }

    private void HidePanel()
    {
        if (adjustPanel && adjustPanel.activeSelf)
            adjustPanel.SetActive(false);
    }

    private void NudgeLocal(float dx, float dy)
    {
        var lp = target.localPosition;
        target.localPosition = new Vector3(lp.x + dx, lp.y + dy, lp.z);
    }
}
