using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Scrollbar))]
public class ScrollbarMinThumb : MonoBehaviour
{
    [Tooltip("Minimum handle height in pixels.")]
    public float minPixels = 48f;

    Scrollbar bar;
    RectTransform track;

    void Awake()
    {
        bar = GetComponent<Scrollbar>();
        track = GetComponent<RectTransform>();
    }

    void OnEnable()
    {
        // Run after ScrollRect/Canvas have sized things
        Canvas.willRenderCanvases += EnforceMinThumb;
    }

    void OnDisable()
    {
        Canvas.willRenderCanvases -= EnforceMinThumb;
    }

    void LateUpdate() { EnforceMinThumb(); }                   // catch runtime changes
    void OnRectTransformDimensionsChange() { EnforceMinThumb(); }        // catch resizes

    void EnforceMinThumb()
    {
        if (!bar || !track) return;
        float trackH = track.rect.height;
        if (trackH <= 0f) return;

        // Scrollbar.size is 0..1 fraction of the track
        float minFrac = Mathf.Clamp01(minPixels / trackH);

        if (bar.size < minFrac)
        {
            bar.size = minFrac;          // enlarge logical handle
            // optional: keep the thumb roughly centred after resize
            bar.value = Mathf.Clamp01(bar.value);
        }
    }
}
