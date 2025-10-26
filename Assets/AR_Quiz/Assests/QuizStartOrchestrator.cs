using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuizStartOrchestrator : MonoBehaviour
{
    [Header("Refs")]
    public VuforiaImageSwipeSelector_ExistingChildren selector;
    public QuizPanelController quizPanel;
    [Tooltip("Loads AI sessions from disk.")]
    public QuizSessionLoader aiLoader;          // add this component somewhere (Step 1 file)
    [Tooltip("If no AI session found, fall back to built-in questions in QuizPanelController.")]
    public bool fallbackToBuiltIn = true;

    [Header("Keyboard special pick-3D first question")]
    public bool includeKeyboardPick3D = true;
    public bool randomCapsOrWindows = true; // true=random Caps/Windows, false=Windows only

    const int TARGET_COUNT = 5; // total questions per model

    void Awake()
    {
        if (!selector) selector = FindObjectOfType<VuforiaImageSwipeSelector_ExistingChildren>();
        // You can also wire this via ModelChooseUI_TMP, but easiest: subscribe here
        if (selector)
        {
            selector.OnModelChosen.AddListener(OnModelChosen);
        }
    }

    void OnDestroy()
    {
        if (selector)
            selector.OnModelChosen.RemoveListener(OnModelChosen);
    }

    private IEnumerator SpeakAfterFrame(string text)
    {
        yield return null;                    // wait one frame so the panel is shown
        QuizVoiceManager.Instance.Stop();     // kill any stray preview
        QuizVoiceManager.Instance.Unmute();   // allow final TTS
        QuizVoiceManager.Instance.Speak(text);
    }

    private void OnModelChosen(string chosenName)
    {
        if (!quizPanel)
        {
            Debug.LogError("[QuizStartOrchestrator] QuizPanel not assigned.");
            return;
        }

        string key = CanonModel(chosenName);
        // 1) Try load AI session for that model
        QuizSessionMeta latest = null;
        if (aiLoader)
        {
            aiLoader.modelId = key; // set and reuse
            latest = aiLoader.LoadLatest();
        }

        List<QuizQuestion> finalList = new List<QuizQuestion>();

        // 2) If model is keyboard, prepend pick-3D
        if (key == "keyboard" && includeKeyboardPick3D)
        {
            bool askCaps = randomCapsOrWindows ? (Random.value < 0.5f) : false;
            if (askCaps)
            {
                finalList.Add(new QuizQuestion
                {
                    type = "pick3d",
                    prompt = "Looking at the keyboard model, where is the <b>Caps Lock</b> key? Tap it.",
                    accept_names = new[] { "Caps", "CapsLock", "Caps Lock" },
                    note = "Caps Lock is usually on the left, above Shift."
                });

            }
            else
            {
                finalList.Add(new QuizQuestion
                {
                    type = "pick3d",
                    prompt = "Looking at the keyboard model, where is the <b>Windows</b> key? Tap it.",
                    accept_names = new[] { "Windows", "Win", "Super" },
                    note = "The Windows key is usually between Ctrl and Alt on the bottom row."
                });
            }
        }

        // 3) Append AI MCQs if available
        if (latest != null && latest.payload != null)
        {
            var aiQs = AIQuizMapper.ToRuntimeQuestions(latest.payload);
            foreach (var q in aiQs)
            {
                finalList.Add(q);
                if (finalList.Count >= TARGET_COUNT) break;
            }
        }

        // 4) If fewer than target and fallback enabled → let panel use its built-in set
        if (finalList.Count == 0 && fallbackToBuiltIn)
        {
            // Use existing entry point (it will render its internal bank)
            quizPanel.StartQuizForModel(chosenName);
            return;
        }

        // Otherwise feed our custom list directly
        QuizVoiceManager.Instance?.Mute();                    // ← block autospeak inside RenderQ
        if (latest != null)
            quizPanel.StartQuizWithQueue(finalList, latest.sessionId, latest.modelId);
        else
            quizPanel.StartQuizWithQueue(finalList, "nosession", key);

        StartCoroutine(SpeakFirstQuestionAfterUI(quizPanel, includeOptions: true));
    }

    public void StartNowFromPayload(QuizSessionMeta meta)
    {
        if (meta == null || meta.payload == null || quizPanel == null) return;

        var finalList = new System.Collections.Generic.List<QuizQuestion>();

        // Keyboard: prepend the pick-3D question as Q1
        if (meta.modelId == "keyboard" && includeKeyboardPick3D)
        {
            bool askCaps = randomCapsOrWindows ? (Random.value < 0.5f) : false;
            if (askCaps)
            {
                finalList.Add(new QuizQuestion
                {
                    type = "pick3d",
                    prompt = "Which key on the 3D keyboard is the Caps Lock key?",
                    options = new[] { "Tap the Caps Lock key on the model." },
                    accept_names = new[] { "Caps", "CapsLock", "Caps Lock" },
                    note = ""
                });
            }
            else
            {
                finalList.Add(new QuizQuestion
                {
                    type = "pick3d",
                    prompt = "Which key on the 3D keyboard is the Windows key?",
                    options = new[] { "Tap the Windows key on the model." },
                    accept_names = new[] { "Windows", "Win", "Super" },
                    note = ""
                });
            }
        }

        // Append AI MCQs to reach 5 total
        var aiQs = AIQuizMapper.ToRuntimeQuestions(meta.payload);
        foreach (var q in aiQs)
        {
            finalList.Add(q);
            if (finalList.Count >= TARGET_COUNT) break;
        }

        QuizVoiceManager.Instance?.Mute();                    // ← block autospeak inside RenderQ
        quizPanel.StartQuizWithQueue(finalList, meta.sessionId, meta.modelId);
        StartCoroutine(SpeakFirstQuestionAfterUI(quizPanel, includeOptions: true));

    }

    private IEnumerator SpeakFirstQuestionAfterUI(QuizPanelController panel, bool includeOptions = true)
    {
        yield return null; // wait 1 frame so RenderQ finished drawing the first question
        QuizVoiceManager.Instance?.Stop();
        QuizVoiceManager.Instance?.Unmute();

        // Ask panel to speak whatever is currently showing (Q0), including pick3d
        panel.RepeatSpeakCurrent(includeOptions);
    }


    private static string CanonModel(string modelName)
    {
        string key = (modelName ?? "").ToLowerInvariant();
        if (key.Contains("keyboard")) return "keyboard";
        if (key.Contains("mouse")) return "mouse";
        if (key.Contains("laptop")) return "laptop";
        if (key.Contains("monitor")) return "monitor";
        if (key.Contains("speaker")) return "speaker";
        return "default";
    }
}
