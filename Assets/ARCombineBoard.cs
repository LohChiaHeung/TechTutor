using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Aliases to use your existing types
using TTSpec = TechTutorAskUI.TutorialSpec;
using TTStep = TechTutorAskUI.TutorialStep;

[RequireComponent(typeof(Canvas))]
public class ARCombinedBoard : MonoBehaviour
{
    [Header("Image (Left)")]
    public RawImage previewImage;     // assign RightPanel/PreviewImage

    [Header("Steps (Right)")]
    public TMP_Text headerText;       // RightPanel/HeaderText
    public Transform pagerRoot;       // RightPanel/PagerRoot
    public GameObject stepCardPrefab; // prefab with StepCardView (TitleText, DescText)
    public Button backButton;
    public Button nextButton;
    public TMP_Text pageLabel;

    [Header("Placement")]
    public bool attachToCamera = true;
    public float distance = 0.9f;
    public Vector3 offset = new Vector3(0f, -0.05f, 0f);

    Camera _cam;
    readonly List<GameObject> _cards = new();
    List<TTStep> _steps = new();
    int _index = 0;

    void Start()
    {
        _cam = Camera.main;

        // 1) Image (prefer fresh in-memory, else load last saved)
        TryShowImage();

        // 2) Steps (prefer TutorialSpecHolder, else parse last_reply)
        var spec = LoadSpec();
        if (spec == null || spec.steps == null || spec.steps.Count == 0)
        {
            headerText?.SetText("AI Tutorial (No steps)");
            pageLabel?.SetText("0 / 0");
            if (backButton) backButton.interactable = false;
            if (nextButton) nextButton.interactable = false;
        }
        else
        {
            headerText?.SetText(string.IsNullOrEmpty(spec.title) ? "AI Tutorial" : spec.title);
            _steps = spec.steps;

            // build one card per step
            foreach (var s in _steps)
            {
                var go = Instantiate(stepCardPrefab, pagerRoot, false);

                // Also force-sanitize local transform
                var rt = go.GetComponent<RectTransform>();
                rt.localScale = Vector3.one;
                rt.localRotation = Quaternion.identity;

                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // center
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(779, 739);                  // ⬅ your desired card size
                rt.anchoredPosition = Vector2.zero;

                go.SetActive(false);
                _cards.Add(go);

                var view = go.GetComponent<StepCardView>();
                if (view) view.Set(s.title, s.description);

                //rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); // center
                //rt.pivot = new Vector2(0.5f, 0.5f);
                //rt.sizeDelta = new Vector2(700, 500);                  // ⬅ your desired card size
                //rt.anchoredPosition = Vector2.zero;
            }

            if (backButton) backButton.onClick.AddListener(() => ShowIndex(_index - 1));
            if (nextButton) nextButton.onClick.AddListener(() => ShowIndex(_index + 1));
            ShowIndex(0);
        }

        // 3) Place the whole canvas
        if (attachToCamera && _cam != null)
        {
            transform.SetParent(_cam.transform, worldPositionStays: false);
            transform.localPosition = new Vector3(0, 0, distance) + offset;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            PlaceInFrontOfCameraOnce();
        }
    }

    void LateUpdate()
    {
        if (!attachToCamera && _cam != null)
        {
            // billboard-ish: keep facing camera horizontally
            Vector3 toCam = _cam.transform.position - transform.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-6f)
                transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
        }
    }

    // ---------- Image ----------
    void TryShowImage()
    {
        if (!previewImage) return;

        // Prefer fresh screenshot
        if (GuideRunContext.I != null && GuideRunContext.I.screenshot != null)
        {
            previewImage.texture = GuideRunContext.I.screenshot;
            FitRawImageToTexture(previewImage, GuideRunContext.I.screenshot);
            return;
        }

        // Fallback: last saved PNG
        var path = PlayerPrefs.GetString("last_image_path", "");
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            var bytes = File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                previewImage.texture = tex; // keep reference if you want to Destroy later
                FitRawImageToTexture(previewImage, tex);
                return;
            }
        }

        previewImage.texture = null;
    }

    static void FitRawImageToTexture(RawImage img, Texture tex)
    {
        if (!img || tex == null) return;
        var rt = img.rectTransform;

        float tw = tex.width, th = tex.height;
        if (tw <= 0 || th <= 0) return;

        // Fit inside current rect while preserving aspect
        var box = rt.sizeDelta;
        if (box.x <= 0 || box.y <= 0) { box = new Vector2(700, 800); rt.sizeDelta = box; }

        float texAR = tw / th, boxAR = box.x / box.y;
        if (texAR > boxAR) rt.sizeDelta = new Vector2(box.x, box.x / texAR);
        else rt.sizeDelta = new Vector2(box.y * texAR, box.y);
    }

    // ---------- Steps ----------
    void ShowIndex(int i)
    {
        if (_cards.Count == 0) return;

        _index = Mathf.Clamp(i, 0, _cards.Count - 1);
        for (int k = 0; k < _cards.Count; k++) _cards[k].SetActive(k == _index);

        pageLabel?.SetText($"{_index + 1} / {_cards.Count}");
        if (backButton) backButton.interactable = _index > 0;
        if (nextButton) nextButton.interactable = _index < _cards.Count - 1;
    }

    TTSpec LoadSpec()
    {
        // Preferred: from holder (set in chat scene)
        TTSpec spec = (TutorialSpecHolder.I != null) ? TutorialSpecHolder.I.spec : null;

        if (spec != null && spec.steps != null && spec.steps.Count > 0)
            return spec;

        // Fallback: parse from last_reply (robust parsing for → / -> / - / plain line)
        if (PlayerPrefs.HasKey("last_reply"))
            return ConvertToTutorialSpecSafe(PlayerPrefs.GetString("last_reply"));

        return null;
    }

    // robust fallback parser (keeps this script self-contained)
    TTSpec ConvertToTutorialSpecSafe(string botReply)
    {
        if (string.IsNullOrWhiteSpace(botReply)) return null;

        string[] lines = botReply.Replace("\r", "").Split('\n');
        var steps = new List<TTStep>();
        TTStep current = null; int stepNum = 1;

        string CleanDesc(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.StartsWith("→")) s = s.Substring(1).Trim();
            else if (s.StartsWith("->")) s = s.Substring(2).Trim();
            else if (s.StartsWith("-")) s = s.Substring(1).Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        foreach (var raw in lines)
        {
            var line = raw.Trim();

            if (line.StartsWith("Step ", System.StringComparison.OrdinalIgnoreCase))
            {
                if (current != null) steps.Add(current);
                current = new TTStep
                {
                    id = stepNum++,
                    title = line,
                    description = "",
                    target = ExtractTargetFromLine(line)
                };
                continue;
            }

            if (current != null && !string.IsNullOrWhiteSpace(line))
            {
                var desc = CleanDesc(line);
                if (desc == null && string.IsNullOrWhiteSpace(current.description))
                    desc = line;
                if (!string.IsNullOrWhiteSpace(desc) && string.IsNullOrWhiteSpace(current.description))
                    current.description = desc;
            }
        }

        if (current != null) steps.Add(current);
        return new TTSpec { title = "AI Tutorial", steps = steps };
    }

    string ExtractTargetFromLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        string t = text.ToLowerInvariant();
        if (t.Contains("keyboard")) return "keyboard";
        if (t.Contains("mouse")) return "mouse";
        if (t.Contains("monitor")) return "monitor";
        if (t.Contains("laptop")) return "laptop";
        return null;
    }

    void PlaceInFrontOfCameraOnce()
    {
        if (_cam == null) return;
        transform.position = _cam.transform.position + _cam.transform.forward * distance + _cam.transform.TransformVector(offset);
        transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);
    }
}
