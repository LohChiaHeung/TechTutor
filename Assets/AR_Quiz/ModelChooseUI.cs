using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModelChooseUI_TMP : MonoBehaviour
{
    public VuforiaImageSwipeSelector_ExistingChildren selector;
    public TextMeshProUGUI modelLabel;
    public Button chooseButton;
    public Button unchooseButton;

    [Header("Quiz")]
    public QuizPanelController quizPanel; // (kept)

    [Header("AI Generator")]
    public AIQuizGenerator_OpenAI generator;
    public GameObject generateSaveButton;

    [Header("Generating Overlay")]
    public GameObject generatingPanel;
    public TextMeshProUGUI generatingText;
    public float toastSeconds = 1.2f;
    public GameObject generatedToastPanel;

    [Header("Voice Precache (optional)")]
    public AIVoiceSpeakerV1 aiSpeaker;
    public bool precacheTts = true;

    [Header("Start Quiz (immediate)")]
    public QuizStartOrchestrator orchestrator; // <-- assign your orchestrator here

    private void Start()
    {
        if (chooseButton) chooseButton.onClick.AddListener(OnChooseClicked);
        if (unchooseButton) unchooseButton.onClick.AddListener(OnUnchooseClicked);

        if (selector)
        {
            selector.OnModelChanged.AddListener(OnModelChanged);
            selector.OnModelChosen.AddListener(OnModelChosen);
            selector.OnModelUnchosen.AddListener(OnModelUnchosen);
        }
        RefreshAll();
    }

    private void OnDestroy()
    {
        if (selector)
        {
            selector.OnModelChanged.RemoveListener(OnModelChanged);
            selector.OnModelChosen.RemoveListener(OnModelChosen);
            selector.OnModelUnchosen.RemoveListener(OnModelUnchosen);
        }
    }

    private void OnModelChanged(string name)
    {
        if (modelLabel) modelLabel.text = $"Current: {name}";
        RefreshAll();
    }

    private void OnModelChosen(string name)
    {
        if (modelLabel) modelLabel.text = $"Chosen: {name}";
        RefreshAll();
        // (do not auto-start here; we start only after Save)
    }

    private void OnModelUnchosen(string name)
    {
        if (modelLabel) modelLabel.text = $"Current: {selector.CurrentModelName}";
        RefreshAll();
    }

    public void OnSaveClicked()
    {
        if (!selector || !generator) return;
        if (!selector.IsChosen) { Debug.Log("[UI] Not locked."); return; }

        if (generateSaveButton && generateSaveButton.TryGetComponent(out UnityEngine.UI.Button btn))
            btn.interactable = false;

        if (generatingPanel) generatingPanel.SetActive(true);
        if (generatingText) generatingText.text = "Generating quiz…";

        string display = selector.ChosenName;
        generator.modelName = display;
        generator.modelId = CanonModel(display);

        generator.OnQuizGeneratedAndSaved -= OnQuizSaved_StartNow;
        generator.OnQuizGeneratedAndSaved += OnQuizSaved_StartNow;
        generator.OnError -= OnQuizError;
        generator.OnError += OnQuizError;

        generator.GenerateAndSave();
    }

    private void OnQuizSaved_StartNow(QuizSessionMeta meta)
    {
        generator.OnQuizGeneratedAndSaved -= OnQuizSaved_StartNow;
        generator.OnError -= OnQuizError;
        StartCoroutine(Co_AfterSaveFlow(meta));
    }

    private IEnumerator Co_AfterSaveFlow(QuizSessionMeta meta)
    {
        if (precacheTts && aiSpeaker != null && meta?.payload?.items != null)
        {
            if (generatingText) generatingText.text = "Preparing voice…";
            var lines = BuildTtsLines(meta);
            int done = 0, total = lines.Count;
            foreach (var pair in lines)
            {
                aiSpeaker.CacheText(pair.text, pair.cacheKey);
                yield return new WaitForSeconds(0.05f);
                done++;
                if (generatingText) generatingText.text = $"Preparing voice… {done}/{total}";
            }
            yield return new WaitForSeconds(0.2f);
        }

        if (generatedToastPanel)
        {
            generatedToastPanel.SetActive(true);
            yield return new WaitForSeconds(toastSeconds);
            generatedToastPanel.SetActive(false);
        }

        if (orchestrator) orchestrator.StartNowFromPayload(meta);

        if (generatingPanel) generatingPanel.SetActive(false);
        if (generateSaveButton && generateSaveButton.TryGetComponent(out UnityEngine.UI.Button btn))
            btn.interactable = true;
    }

    private void OnQuizError(string msg)
    {
        generator.OnQuizGeneratedAndSaved -= OnQuizSaved_StartNow;
        generator.OnError -= OnQuizError;

        if (generatingText) generatingText.text = $"Error: {msg}";
        Debug.LogError("[Generator] " + msg);

        StartCoroutine(Co_HideGenPanelSoon());
        if (generateSaveButton && generateSaveButton.TryGetComponent(out UnityEngine.UI.Button btn))
            btn.interactable = true;
    }

    private IEnumerator Co_HideGenPanelSoon()
    {
        yield return new WaitForSeconds(1.2f);
        if (generatingPanel) generatingPanel.SetActive(false);
    }

    private string CanonModel(string name)
    {
        var s = (name ?? "").ToLowerInvariant();
        if (s.Contains("keyboard")) return "keyboard";
        if (s.Contains("mouse")) return "mouse";
        if (s.Contains("monitor")) return "monitor";
        if (s.Contains("laptop")) return "laptop";
        if (s.Contains("speaker")) return "speaker";
        return "default";
    }

    private struct TtsPair { public string text; public string cacheKey; public TtsPair(string t, string k) { text = t; cacheKey = k; } }

    private System.Collections.Generic.List<TtsPair> BuildTtsLines(QuizSessionMeta meta)
    {
        var list = new System.Collections.Generic.List<TtsPair>();
        if (meta.modelId == "keyboard")
            list.Add(new TtsPair("Which key on the 3D keyboard is the Caps Lock key?", $"{meta.modelId}_{meta.sessionId}_q0_prompt"));

        int baseIndex = (meta.modelId == "keyboard") ? 1 : 0;
        int i = 0;
        foreach (var it in meta.payload.items)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(it.question);
            if (it.options != null && it.options.Length >= 4)
                sb.Append(" Options: A. ").Append(it.options[0]).Append(" B. ").Append(it.options[1]).Append(" C. ").Append(it.options[2]).Append(" D. ").Append(it.options[3]);

            string key = $"{meta.modelId}_{meta.sessionId}_q{baseIndex + i}_full";
            list.Add(new TtsPair(sb.ToString(), key));
            i++;
            if (baseIndex + i >= 5) break;
        }
        return list;
    }

    private void OnChooseClicked()
    {
        if (selector) selector.ChooseCurrentModel();
        RefreshAll();
    }

    private void OnUnchooseClicked()
    {
        if (selector) selector.UnchooseCurrentModel();
        RefreshAll();
    }

    private void RefreshAll()
    {
        if (selector && modelLabel)
            modelLabel.text = selector.IsChosen ? $"Chosen: {selector.ChosenName}" : $"Current: {selector.CurrentModelName}";
        UpdateButtons();
        UpdateSaveButton();
    }

    private void UpdateButtons()
    {
        if (!selector) return;
        if (chooseButton) chooseButton.interactable = !selector.IsChosen;
        if (unchooseButton) unchooseButton.interactable = selector.IsChosen;
    }

    private void UpdateSaveButton()
    {
        if (generateSaveButton)
            generateSaveButton.SetActive(selector != null && selector.IsChosen);
    }
}
