using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OCR_TestHarness : MonoBehaviour
{
    [Header("Assign")]
    public RawImage screenshotImage;   // the RawImage under your Panel
    public AR2_OcrOverlay overlay;     // your overlay script
    public Texture2D testScreenshot;   // assign AR_AI_Tutorial.jpg here in Inspector

    [Header("Mode")]
    public bool useSynthetic = true;   // start with synthetic boxes, then switch to real OCR later

    // Use your project's real OCR types (no nested classes here!)
    private OcrResponse _ocr;

    void Start()
    {
        if (!screenshotImage || !overlay)
        {
            Debug.LogError("[OCR_Test] Missing refs.");
            return;
        }

        // 1) show the test screenshot
        if (testScreenshot != null)
        {
            screenshotImage.texture = testScreenshot;
            screenshotImage.uvRect = new Rect(0, 0, 1, 1);
        }
        else
        {
            Debug.LogWarning("[OCR_Test] No testScreenshot assigned.");
        }

        // settle layout so rects are final
        LayoutRebuilder.ForceRebuildLayoutImmediate(screenshotImage.rectTransform);
        Canvas.ForceUpdateCanvases();

        // 2) build OCR data
        if (useSynthetic) BuildSyntheticForThisImage();
        else Debug.LogWarning("[OCR_Test] useSynthetic=false — plug your real OCR call here.");

        // 3) draw everything
        DrawAll();
    }

    // Synthetic boxes placed by percentage of image size, then converted to pixels.
    void BuildSyntheticForThisImage()
    {
        var tex = (Texture2D)screenshotImage.texture;
        int W = tex ? tex.width : 1600;
        int H = tex ? tex.height : 900;

        // Helper to place rects by percentage (x%, y%, w%, h%) in OCR pixel space
        Rect P(float px, float py, float pw, float ph) =>
            new Rect(W * px, H * py, W * pw, H * ph);

        var words = new List<OcrItem>
        {
            // Top bar items (approximate positions for AR_AI_Tutorial.jpg)
            new OcrItem{ text="Assets", x=P(0.05f, 0.08f, 0.06f, 0.04f).x, y=P(0.05f, 0.08f, 0.06f, 0.04f).y, w=P(0.05f, 0.08f, 0.06f, 0.04f).width,  h=P(0.05f, 0.08f, 0.06f, 0.04f).height },
            new OcrItem{ text="Name",   x=P(0.45f, 0.13f, 0.07f, 0.04f).x, y=P(0.45f, 0.13f, 0.07f, 0.04f).y, w=P(0.45f, 0.13f, 0.07f, 0.04f).width,  h=P(0.45f, 0.13f, 0.07f, 0.04f).height },
            new OcrItem{ text="Date modified", x=P(0.58f, 0.13f, 0.14f, 0.04f).x, y=P(0.58f, 0.13f, 0.14f, 0.04f).y, w=P(0.58f, 0.13f, 0.14f, 0.04f).width, h=P(0.58f, 0.13f, 0.14f, 0.04f).height },

            // Left tree items
            new OcrItem{ text="English Training", x=P(0.02f, 0.45f, 0.14f, 0.04f).x, y=P(0.02f, 0.45f, 0.14f, 0.04f).y, w=P(0.02f, 0.45f, 0.14f, 0.04f).width, h=P(0.02f, 0.45f, 0.14f, 0.04f).height },
            new OcrItem{ text="AR_Rendering_Learning", x=P(0.02f, 0.52f, 0.22f, 0.04f).x, y=P(0.02f, 0.52f, 0.22f, 0.04f).y, w=P(0.02f, 0.52f, 0.22f, 0.04f).width, h=P(0.02f, 0.52f, 0.22f, 0.04f).height },

            // Center list items
            new OcrItem{ text="SendEmailPrefab", x=P(0.36f, 0.32f, 0.16f, 0.045f).x, y=P(0.36f, 0.32f, 0.16f, 0.045f).y, w=P(0.36f, 0.32f, 0.16f, 0.045f).width, h=P(0.36f, 0.32f, 0.16f, 0.045f).height },
            new OcrItem{ text="AR_Congratulations.png", x=P(0.34f, 0.69f, 0.22f, 0.05f).x, y=P(0.34f, 0.69f, 0.22f, 0.05f).y, w=P(0.34f, 0.69f, 0.22f, 0.05f).width, h=P(0.34f, 0.69f, 0.22f, 0.05f).height },
        };

        _ocr = new OcrResponse { width = W, height = H, words = words.ToArray() };
        Debug.Log($"[OCR_Test] Synthetic OCR for {W}x{H} with {words.Count} items.");
    }

    void DrawAll()
    {
        if (_ocr == null || _ocr.words == null || _ocr.words.Length == 0)
        {
            Debug.LogWarning("[OCR_Test] No OCR words to draw.");
            return;
        }

        // ensure layout is settled
        LayoutRebuilder.ForceRebuildLayoutImmediate(screenshotImage.rectTransform);
        Canvas.ForceUpdateCanvases();

        // draw the image bounds and every OCR word box
        overlay.Clear();
        overlay.showImageBounds = true;
        overlay.DrawImageBounds(_ocr.width, _ocr.height);

        foreach (var w in _ocr.words)
            overlay.DrawBox(w, _ocr, Color.red);

        // Corner calibration (press C to toggle)
        _drawCorners = true;
        if (_drawCorners) DrawCorners();
    }

    bool _drawCorners;
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            _drawCorners = !_drawCorners;
            DrawAll();
        }
    }

    void DrawCorners()
    {
        if (!_drawCorners) return;

        var W = Mathf.Max(1, _ocr.width);
        var H = Mathf.Max(1, _ocr.height);

        var tl = new OcrItem { text = "tl", x = 0, y = 0, w = 120, h = 40 };
        var tr = new OcrItem { text = "tr", x = W - 120, y = 0, w = 120, h = 40 };
        var bl = new OcrItem { text = "bl", x = 0, y = H - 40, w = 120, h = 40 };
        var br = new OcrItem { text = "br", x = W - 120, y = H - 40, w = 120, h = 40 };

        overlay.DrawBox(tl, _ocr, new Color(0, 0.6f, 1f, 0.25f));
        overlay.DrawBox(tr, _ocr, new Color(0, 0.6f, 1f, 0.25f));
        overlay.DrawBox(bl, _ocr, new Color(0, 0.6f, 1f, 0.25f));
        overlay.DrawBox(br, _ocr, new Color(0, 0.6f, 1f, 0.25f));
    }
}
