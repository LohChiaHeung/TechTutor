//using System.Collections.Generic;
//using TMPro;
//using UnityEngine;
//using UnityEngine.UI;

//public class AR_ImageTest : MonoBehaviour
//{
//    [Header("UI")]
//    public RawImage photo;                 // The screenshot/image you’re showing
//    public TMP_Text stepTitle;
//    public TMP_Text stepBody;
//    public Button nextBtn;
//    public Button backBtn;

//    //[Header("Callout")]
//    //public calloutOverlay calloutOverlay;  // Drag the GameObject with CalloutOverlay.cs here

//    // --- Simple test data types ---
//    class Detection { public string label; public float x, y, w, h; } // normalized, TOP-LEFT origin
//    class Step { public string title; public string desc; public string target; }

//    readonly List<Detection> detections = new();
//    readonly List<Step> steps = new();
//    int index = 0;

//    void Start()
//    {
//        // Safety: require CalloutOverlay + RawImage
//        if (calloutOverlay == null) { Debug.LogError("[AR_ImageTest] Missing CalloutOverlay reference."); return; }
//        if (photo == null) { Debug.LogError("[AR_ImageTest] Missing RawImage (photo) reference."); return; }

//        // Ensure CalloutOverlay uses the same RawImage
//        if (calloutOverlay.photo == null) calloutOverlay.photo = photo;

//        // 1) Load test image from Resources (Assets/Resources/AR_AI_Tutorial.jpg)
//        var tex = Resources.Load<Texture2D>("AR_AI_Tutorial");
//        if (tex == null) { Debug.LogError("Put your image at Assets/Resources/AR_AI_Tutorial.jpg"); return; }
//        photo.texture = tex;

//        // 2) Fake detections to test (adjust to your image)
//        //    Coords are normalized (0..1), origin TOP-LEFT, width/height in [0..1]
//        detections.Add(new Detection { label = "folder", x = 0.30f, y = 0.20f, w = 0.22f, h = 0.12f });
//        detections.Add(new Detection { label = "keyboard", x = 0.10f, y = 0.70f, w = 0.60f, h = 0.20f });

//        // 3) Fixed steps (target matches detection.label)
//        steps.Add(new Step { title = "Step 1: Click folder", desc = "→ Select the Documents folder", target = "folder" });
//        steps.Add(new Step { title = "Step 2: Type on keyboard", desc = "→ Enter your file name", target = "keyboard" });
//        steps.Add(new Step { title = "Step 3: Read this tip", desc = "→ Nothing to highlight", target = null });

//        // 4) Wire buttons
//        if (nextBtn) nextBtn.onClick.AddListener(() => { if (index < steps.Count - 1) Show(++index); });
//        if (backBtn) backBtn.onClick.AddListener(() => { if (index > 0) Show(--index); });

//        // 5) Start on first step
//        Show(0);
//    }

//    void Show(int i)
//    {
//        index = Mathf.Clamp(i, 0, steps.Count - 1);
//        var s = steps[index];

//        if (stepTitle) stepTitle.text = s.title;
//        if (stepBody) stepBody.text = s.desc;

//        // Clear old callouts (CalloutOverlay manages children under its calloutRoot)
//        ClearCallouts();

//        // If this step points to something, show the callout near that region
//        if (!string.IsNullOrEmpty(s.target))
//        {
//            var det = detections.Find(d => d.label == s.target);
//            if (det != null)
//            {
//                calloutOverlay.ShowCallout(s.title, s.desc, det.x, det.y, det.w, det.h);
//            }
//        }
//        // else: no highlight, just the text panel
//    }

//    void ClearCallouts()
//    {
//        if (calloutOverlay == null || calloutOverlay.calloutRoot == null) return;
//        for (int i = calloutOverlay.calloutRoot.childCount - 1; i >= 0; i--)
//            Destroy(calloutOverlay.calloutRoot.GetChild(i).gameObject);
//    }
//}
