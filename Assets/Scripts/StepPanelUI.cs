//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//// aliases if you use the global models
//using GuideModel = global::AIGuide;
//using GuideStep = global::AIGuideStep;

//public class StepPanelUI : MonoBehaviour
//{
//    [Header("UI (assign in Inspector)")]
//    public TextMeshProUGUI stepTitleTMP;
//    public TextMeshProUGUI stepBodyTMP;
//    public Button prevButton;
//    public Button nextButton;

//    [Header("Runner link")]
//    public AR_RedboxRunner runner;   // drag your existing runner here

//    // internal
//    GuideModel _guide;
//    int _idx = 0;

//    void Start()
//    {
//        // Load guide from context once
//        _guide = GuideRunContext.I != null ? GuideRunContext.I.guide : null;

//        // Button hooks
//        if (prevButton) prevButton.onClick.AddListener(OnPrev);
//        if (nextButton) nextButton.onClick.AddListener(OnNext);

//        AR_RedboxRunner.OnKeywordMatched += HandleKeywordMatched;

//        // Initialize UI + runner
//        ClampIndex();
//        RenderStep();
//        // Tell the runner to show this step's keywords (and draw the first match)
//        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
//    }


//    void ClampIndex()
//    {
//        int max = (_guide?.steps != null) ? _guide.steps.Length : 0;
//        if (max <= 0) { _idx = 0; return; }
//        _idx = Mathf.Clamp(_idx, 0, max - 1);
//    }

//    void OnDestroy()
//    {
//        AR_RedboxRunner.OnKeywordMatched -= HandleKeywordMatched;
//    }

//    void HandleKeywordMatched(string keyword)
//    {
//        // Update the body text to include the matched keyword
//        if (stepBodyTMP)
//        {
//            stepBodyTMP.text = $"{_guide.steps[_idx].instruction}\n\nMatched: {keyword}";
//        }
//    }

//    void RenderStep()
//    {
//        var has = _guide?.steps != null && _guide.steps.Length > 0;
//        if (!has)
//        {
//            if (stepTitleTMP) stepTitleTMP.text = "No steps";
//            if (stepBodyTMP) stepBodyTMP.text = "Ask a question first.";
//            if (prevButton) prevButton.interactable = false;
//            if (nextButton) nextButton.interactable = false;
//            return;
//        }

//        var s = _guide.steps[_idx];
//        if (stepTitleTMP) stepTitleTMP.text = s.title ?? $"Step {_idx + 1}";
//        if (stepBodyTMP) stepBodyTMP.text = s.instruction ?? "";

//        if (prevButton) prevButton.interactable = (_idx > 0);
//        if (nextButton) nextButton.interactable = (_idx < _guide.steps.Length - 1);
//    }

//    void OnPrev()
//    {
//        if (_guide?.steps == null || _guide.steps.Length == 0) return;
//        _idx = Mathf.Max(0, _idx - 1);
//        RenderStep();
//        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
//    }

//    void OnNext()
//    {
//        if (_guide?.steps == null || _guide.steps.Length == 0) return;
//        _idx = Mathf.Min(_guide.steps.Length - 1, _idx + 1);
//        RenderStep();
//        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
//    }
//}


using UnityEngine;
using UnityEngine.UI;
using TMPro;

// aliases if you use the global models
using GuideModel = global::AIGuide;
using GuideStep = global::AIGuideStep;

public class StepPanelUI : MonoBehaviour
{
    [Header("UI (assign in Inspector)")]
    public TextMeshProUGUI stepTitleTMP;
    public TextMeshProUGUI stepBodyTMP;
    public Button prevButton;
    public Button nextButton;

    [Header("Runner link")]
    public AR_RedboxRunner runner;   // drag your existing runner here

    // internal
    GuideModel _guide;
    int _idx = 0;

    void Start()
    {
        // Load guide from context once
        _guide = GuideRunContext.I != null ? GuideRunContext.I.guide : null;

        // Button hooks
        if (prevButton) prevButton.onClick.AddListener(OnPrev);
        if (nextButton) nextButton.onClick.AddListener(OnNext);

        // Initialize UI + runner
        ClampIndex();
        RenderStep();
        // Tell the runner to show this step's keywords (and draw the first match)
        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
    }

    void ClampIndex()
    {
        int max = (_guide?.steps != null) ? _guide.steps.Length : 0;
        if (max <= 0) { _idx = 0; return; }
        _idx = Mathf.Clamp(_idx, 0, max - 1);
    }

    void RenderStep()
    {
        var has = _guide?.steps != null && _guide.steps.Length > 0;
        if (!has)
        {
            if (stepTitleTMP) stepTitleTMP.text = "No steps";
            if (stepBodyTMP) stepBodyTMP.text = "Ask a question first.";
            if (prevButton) prevButton.interactable = false;
            if (nextButton) nextButton.interactable = false;
            return;
        }

        var s = _guide.steps[_idx];
        if (stepTitleTMP) stepTitleTMP.text = s.title ?? $"Step {_idx + 1}";
        if (stepBodyTMP) stepBodyTMP.text = s.instruction ?? "";

        if (prevButton) prevButton.interactable = (_idx > 0);
        if (nextButton) nextButton.interactable = (_idx < _guide.steps.Length - 1);
    }

    void OnPrev()
    {
        if (_guide?.steps == null || _guide.steps.Length == 0) return;
        _idx = Mathf.Max(0, _idx - 1);
        RenderStep();
        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
    }

    void OnNext()
    {
        if (_guide?.steps == null || _guide.steps.Length == 0) return;
        _idx = Mathf.Min(_guide.steps.Length - 1, _idx + 1);
        RenderStep();
        if (runner) runner.GotoStep(_idx, showFirstMatch: true);
    }
}
