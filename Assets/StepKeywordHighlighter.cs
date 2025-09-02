using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

public class StepKeywordHighlighter : MonoBehaviour
{
    [Header("Assign")]
    public RawImage screenshotImage;           // display the screenshot
    public OcrOverlayRenderer overlay;         // from #2
    public Button prevButton;
    public Button nextButton;
    public TMP_Text stepTitle;
    public TMP_Text stepDesc;

    [Header("OCR")]
    public EasyOcrClient ocrClient;           // your existing OCR client (or plug your response in)
    public float minMatchScore = 0.55f;

    Texture2D _shot;
    AIGuide _guide;        // assume global AIGuide model already exists in your project
    int _idx = 0;
    KeywordsMatcher.OcrResponse _lastOcr;

    void Start()
    {
        // Pull data from the handoff
        _shot = GuideRunContext.I != null ? GuideRunContext.I.screenshot : null;
        _guide = GuideRunContext.I != null ? GuideRunContext.I.guide : null;

        if (screenshotImage && _shot) screenshotImage.texture = _shot;

        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0 || _shot == null)
        {
            Debug.LogWarning("[StepHighlighter] Missing guide or screenshot.");
            SetButtons(false);
            return;
        }

        prevButton.onClick.AddListener(OnPrev);
        nextButton.onClick.AddListener(OnNext);

        // Run OCR once per screenshot; then we reuse
 //       StartCoroutine(ocrClient.Run(
 //    _shot,
 //    OnOcrReady,
 //    err => Debug.LogError("[OCR] " + err)
 //));
    }

    //void OnOcrReady(OcrResponse resp)
    //{
    //    _lastOcr = resp;   // make sure _lastOcr is type OcrResponse
    //    _idx = 0;
    //    RefreshUI();
    //}

    void RefreshUI()
    {
        if (_guide == null || _guide.steps == null || _guide.steps.Length == 0 || _lastOcr == null)
            return;

        var step = _guide.steps[_idx];
        if (stepTitle) stepTitle.text = step.title;
        if (stepDesc) stepDesc.text = step.instruction;

        overlay.Clear();

        // Prefer explicit keywords from step; if none, derive from title+instruction:
        var kws = (step.keywords != null && step.keywords.Length > 0)
            ? step.keywords
            : DeriveKeywords(step.title, step.instruction);

        // Try each keyword until we find a good match; draw only the best one for clarity
        var best = KeywordsMatcher.FindBest(kws, _lastOcr, minMatchScore);
        if (best != null)
        {
            var r = new Rect(best.word.x, best.word.y, best.word.w, best.word.h);
            overlay.DrawBox(r, _lastOcr.width, _lastOcr.height, Color.green);
            Debug.Log($"[StepHighlighter] Matched \"{best.keyword}\" -> \"{best.word.text}\" score={best.score:0.00} @ ({best.word.x},{best.word.y},{best.word.w},{best.word.h})");
        }
        else
        {
            Debug.Log("[StepHighlighter] No good match found for this step.");
        }

        SetButtons(true);
    }

    void OnPrev()
    {
        if (_idx > 0) { _idx--; RefreshUI(); }
    }

    void OnNext()
    {
        if (_idx + 1 < _guide.steps.Length) { _idx++; RefreshUI(); }
    }

    void SetButtons(bool enable)
    {
        if (prevButton) prevButton.interactable = enable && _idx > 0;
        if (nextButton) nextButton.interactable = enable && _idx + 1 < (_guide?.steps?.Length ?? 0);
    }

    // Fallback if a step has no keywords array:
    static readonly HashSet<string> Stop = new HashSet<string>(new[] {
        "the","a","an","to","and","or","of","in","on","at","for","with","by","from","into","your","this","that","it","is","are","as","be",
        "click","press","select","choose","open","tap","type","enter","menu","window","screen","page","folder","file","document"
    });

    string[] DeriveKeywords(string title, string instruction)
    {
        var bag = new List<string>();
        ExtractQuoted(title, bag);
        ExtractQuoted(instruction, bag);
        bag.AddRange(TitleCaseTokens(title));
        bag.AddRange(TitleCaseTokens(instruction));

        return bag
            .Select(t => t.Trim())
            .Where(t => t.Length > 1 && !Stop.Contains(t.ToLowerInvariant()))
            .Distinct(System.StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToArray();
    }

    void ExtractQuoted(string s, List<string> outList)
    {
        if (string.IsNullOrEmpty(s)) return;
        var m1 = System.Text.RegularExpressions.Regex.Matches(s, "\"([^\"]+)\"");
        foreach (System.Text.RegularExpressions.Match m in m1) outList.Add(m.Groups[1].Value);
        var m2 = System.Text.RegularExpressions.Regex.Matches(s, "'([^']+)'");
        foreach (System.Text.RegularExpressions.Match m in m2) outList.Add(m.Groups[1].Value);
    }

    IEnumerable<string> TitleCaseTokens(string s)
    {
        if (string.IsNullOrEmpty(s)) yield break;
        var rx = new System.Text.RegularExpressions.Regex(@"\b([A-Z][a-zA-Z0-9\+\-_/]{1,30}|[A-Z0-9\+\-_/]{2,30})\b");
        foreach (System.Text.RegularExpressions.Match m in rx.Matches(s))
            yield return m.Value;
    }
}
