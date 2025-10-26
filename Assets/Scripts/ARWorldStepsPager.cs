using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static TechTutorAskUI;
using TTSpec = TechTutorAskUI.TutorialSpec;
using TTStep = TechTutorAskUI.TutorialStep;

public class ARWorldStepsPager_TutorialSpec : MonoBehaviour
{
    public TMP_Text headerText;
    public Transform pagerRoot;
    public GameObject stepCardPrefab;
    public Button backButton, nextButton;
    public TMP_Text pageLabel;

    List<TTStep> _steps = new();
    readonly List<GameObject> _cards = new();
    int _index = 0;

    void Start()
    {
        // 1) Load spec from holder (preferred)
        TTSpec spec = (TutorialSpecHolder.I != null) ? TutorialSpecHolder.I.spec : null;

        // 2) Fallback: parse from PlayerPrefs if needed
        if (spec == null || spec.steps == null || spec.steps.Count == 0)
        {
            if (PlayerPrefs.HasKey("last_reply"))
                spec = ConvertToTutorialSpecSafe(PlayerPrefs.GetString("last_reply"));
        }

        if (spec == null || spec.steps == null || spec.steps.Count == 0)
        {
            headerText?.SetText("AI Tutorial (No steps)");
            pageLabel?.SetText("0 / 0");
            backButton.interactable = nextButton.interactable = false;
            return;
        }

        headerText?.SetText(string.IsNullOrEmpty(spec.title) ? "AI Tutorial" : spec.title);
        _steps = spec.steps; // same type (TTStep), direct assign ok

        foreach (var s in _steps)
        {
            var go = Instantiate(stepCardPrefab, pagerRoot);
            var view = go.GetComponent<StepCardView>();
            if (view) view.Set(s.title, s.description);
            go.SetActive(false);
            _cards.Add(go);
        }

        backButton.onClick.AddListener(() => ShowIndex(_index - 1));
        nextButton.onClick.AddListener(() => ShowIndex(_index + 1));
        ShowIndex(0);
    }

    void ShowIndex(int i)
    {
        if (_cards.Count == 0) return;
        _index = Mathf.Clamp(i, 0, _cards.Count - 1);
        for (int k = 0; k < _cards.Count; k++) _cards[k].SetActive(k == _index);
        pageLabel?.SetText($"{_index + 1} / {_cards.Count}");
        backButton.interactable = _index > 0;
        nextButton.interactable = _index < _cards.Count - 1;
    }

    // local fallback parser (mirrors your ConvertToTutorialSpec)
    // local fallback parser (mirrors your ConvertToTutorialSpec, but robust)
    TTSpec ConvertToTutorialSpecSafe(string botReply)
    {
        if (string.IsNullOrWhiteSpace(botReply)) return null;

        string[] lines = botReply.Replace("\r", "").Split('\n');
        var steps = new List<TTStep>();
        TTStep current = null;
        int stepNum = 1;

        // helper: strip leading markers
        string CleanDesc(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            s = s.Trim();
            if (s.StartsWith("→")) s = s.Substring(1).Trim();
            else if (s.StartsWith("->")) s = s.Substring(2).Trim();
            else if (s.StartsWith("-")) s = s.Substring(1).Trim();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();

            // New step header
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

            // Description line(s)
            if (current != null && !string.IsNullOrWhiteSpace(line))
            {
                // prefer explicit arrow/bullet; else first non-empty line after Step
                string desc = CleanDesc(line);
                if (desc == null && string.IsNullOrWhiteSpace(current.description))
                    desc = line;

                if (!string.IsNullOrWhiteSpace(desc) && string.IsNullOrWhiteSpace(current.description))
                    current.description = desc;
            }
        }

        if (current != null) steps.Add(current);
        return new TTSpec { title = "AI Tutorial", steps = steps };
    }

    // Local target extractor (keeps this script self-contained)
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
}

