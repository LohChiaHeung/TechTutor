using UnityEngine;

public class LocalLayoutAnalyzer : MonoBehaviour
{
    public MarkerLayoutAsset layout;
    public bool fallbackDemoIfEmpty = true;

    public OverlayMarkerManager.NormRect[] Analyze(Texture2D tex)
    {
        if (layout != null && layout.entries != null && layout.entries.Length > 0)
        {
            var arr = new OverlayMarkerManager.NormRect[layout.entries.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var e = layout.entries[i];
                arr[i] = new OverlayMarkerManager.NormRect
                {
                    id = e.id,
                    type = e.type,
                    pos01 = e.pos01,
                    size01 = e.size01,
                    label = e.label
                };
            }
            return arr;
        }

        if (!fallbackDemoIfEmpty) return System.Array.Empty<OverlayMarkerManager.NormRect>();

        // Simple demo if no layout set
        return new OverlayMarkerManager.NormRect[]
        {
            new OverlayMarkerManager.NormRect{ id="title", type="box", label="Title",
                pos01=new Vector2(0.5f, 0.15f), size01=new Vector2(0.8f, 0.18f) },
            new OverlayMarkerManager.NormRect{ id="table", type="box", label="Table",
                pos01=new Vector2(0.5f, 0.62f), size01=new Vector2(0.9f, 0.32f) },
            new OverlayMarkerManager.NormRect{ id="hint", type="pin", label="",
                pos01=new Vector2(0.85f, 0.25f), size01=Vector2.zero }
        };
    }
}
