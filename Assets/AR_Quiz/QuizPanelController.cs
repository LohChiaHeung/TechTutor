using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;


[Serializable]
public class QuizQuestion
{
    public string type = "mcq";          // "mcq" | "pick3d"
    public string[] accept_names = null; // for pick3d
    public string prompt;
    public string[] options = new string[4];
    public int correct;                  // 0..3
    public string note;                  // optional
}

public class QuizPanelController : MonoBehaviour
{
    [Header("AI Voice (optional)")]
    public AIVoiceSpeakerV1 aiSpeaker;
    public bool speakQuestionAndOptions = true;

    // NEW: track history items for this session
    private List<QuizHistoryItem> _histItems = new List<QuizHistoryItem>();
    // at top of class
    private Coroutine _speakCo;


    [Header("UI")]
    public Canvas quizCanvas;
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI feedbackText;
    public TextMeshProUGUI optionsListText;

    public Button[] optionButtons = new Button[4];
    public Button backButton;
    public Button nextButton;
    public Button closeButton;

    [Header("AR Input (for pick3d)")]
    public Camera arCamera;
    public string tapLayerName = "AR_TapTargets";
    public float raycastMaxDistance = 10f;

    [Header("Config")]
    public bool showExplanation = true;
    public float autoAdvanceSeconds = 5f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip sfxCorrect;
    public AudioClip sfxWrong;

    [Header("Colors")]
    public Color progressDefault = Color.white;
    public Color feedbackDefault = Color.white;
    public Color feedbackCorrect = new Color(0.2f, 0.8f, 0.2f);
    public Color feedbackWrong = new Color(0.9f, 0.3f, 0.3f);

    // runtime
    private List<QuizQuestion> _queue;
    private int _qIndex;
    private bool _answered;
    private bool _waitingPick3D;
    private int _tapMask;

    private enum QState { Unanswered, Correct }
    private List<QState> _states = new List<QState>();

    private Renderer _lastRenderer; Color _lastColor;
    private readonly Color OK = new Color(0.2f, 0.8f, 0.2f);
    private readonly Color BAD = new Color(0.9f, 0.3f, 0.3f);
    private Coroutine _autoNextCo;

    // NEW: make TTS cache unique per session/model
    private string _sessionIdForAudio = "nosession";
    private string _modelIdForAudio = "nomodel";

    void Awake()
    {
        if (!arCamera) arCamera = Camera.main;
        if (quizCanvas) quizCanvas.gameObject.SetActive(false);
        _tapMask = string.IsNullOrEmpty(tapLayerName) ? ~0 : LayerMask.GetMask(tapLayerName);

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int ix = i;
            if (optionButtons[i]) optionButtons[i].onClick.AddListener(() => OnChooseOption(ix));
        }

        if (nextButton) nextButton.onClick.AddListener(Next);
        if (backButton) backButton.onClick.AddListener(Back);
        if (closeButton) closeButton.onClick.AddListener(Close);

        if (!audioSource && (sfxCorrect || sfxWrong))
            Debug.LogWarning("[Quiz] Assign an AudioSource to play SFX.");
    }

    public QuizQuestion GetCurrentQuestion()
    {
        if (_queue == null || _qIndex < 0 || _qIndex >= _queue.Count) return null;
        return _queue[_qIndex];
    }
    public int CurrentIndex() => _qIndex;
    // ===== Public API =====
    public void StartQuizWithQueue(List<QuizQuestion> queue)
    {
        if (queue == null || queue.Count == 0) { Debug.LogError("[Quiz] Empty queue."); return; }
        _queue = queue;
        // Randomize MCQ options once per question
        for (int i = 0; i < _queue.Count; i++)
        {
            var q = _queue[i];
            if (q != null && q.type == "mcq" && q.options != null && q.options.Length >= 2)
                ShuffleOptions(q);
        }

        _states.Clear();
        for (int i = 0; i < _queue.Count; i++) _states.Add(QState.Unanswered);
        _qIndex = 0;
        _histItems = new List<QuizHistoryItem>();
        if (quizCanvas) quizCanvas.gameObject.SetActive(true);
        RenderQ();
    }

    // NEW: overload that also sets session/model ids (for fresh TTS per session)
    public void StartQuizWithQueue(List<QuizQuestion> queue, string sessionId, string modelId)
    {
        _sessionIdForAudio = string.IsNullOrEmpty(sessionId) ? "nosession" : sessionId;
        _modelIdForAudio = string.IsNullOrEmpty(modelId) ? "nomodel" : modelId;
        StartQuizWithQueue(queue);
        //StartCoroutine(SpeakAfterFrame());
    }

    // --- Add to class ---
    // Put inside QuizPanelController (e.g., near other helpers)

    // Fingerprint prompt + 4 options (order-sensitive)
    private static string Fingerprint(QuizQuestion q)
    {
        if (q == null) return "noq";
        unchecked
        {
            uint h = 2166136261;
            void mix(string s)
            {
                if (s == null) return;
                foreach (char c in s) { h ^= (byte)c; h *= 16777619; }
                h ^= (byte)'|'; h *= 16777619;
            }
            mix(q.prompt);
            if (q.options != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    var s = (i < q.options.Length) ? (q.options[i] ?? "") : "";
                    mix(s);
                }
            }
            return h.ToString("x8");
        }
    }

    private string CacheKeyForCurrent(string suffix, bool includeOptions)
    {
        const string v = "v2"; // bump version once to invalidate all old clips
        var q = GetCurrentQuestion();
        string fp = includeOptions ? Fingerprint(q) : "nop";
        return $"{_modelIdForAudio}_{_sessionIdForAudio}_{v}_q{_qIndex}_{fp}_{suffix}";
    }


    private IEnumerator SpeakAfterFrame()
    {
        yield return null; // wait for RenderQ to finish
        RepeatSpeakCurrent(includeOptions: true);
    }
    private void RenderQ()
    {
        CancelAutoAdvance();
        _answered = false;
        _waitingPick3D = false;

        if (progressText) progressText.color = progressDefault;

        if (_qIndex >= _queue.Count)
        {
            SetText(questionText, "Quiz Complete!");
            if (QuizHistoryManager.Instance != null)
            {
                var entry = new QuizHistoryEntry
                {
                    modelId = _modelIdForAudio,
                    sessionId = _sessionIdForAudio,
                    date = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                    score = Score(),
                    totalQuestions = _queue.Count,
                    items = _histItems
                };
                QuizHistoryManager.Instance.AddHistory(entry);
                Debug.Log($"[History] Saved session {entry.sessionId} ({entry.modelId}) with {entry.score}/{entry.totalQuestions}");
            }
            SetText(progressText, $"{Score()}/{_queue.Count} correct");
            SetText(feedbackText, "Tap Close to finish.");
            SetText(optionsListText, "");
            ShowMCQ(false);
            SetInteract(nextButton, false);
            SetInteract(backButton, _queue.Count > 0);
            return;
        }

        SetText(progressText, $"Question {_qIndex + 1} / {_queue.Count}");
        SetText(feedbackText, "");

        var q = _queue[_qIndex];
        SetText(questionText, q.prompt);

        // Ensure history item exists for this question
        while (_histItems.Count <= _qIndex) _histItems.Add(null);
        if (_histItems[_qIndex] == null)
        {
            _histItems[_qIndex] = new QuizHistoryItem
            {
                type = q.type,
                prompt = q.prompt,
                options = (q.type == "mcq" ? (string[])q.options.Clone() : null),
                correct = (q.type == "mcq" ? q.correct : -1),
                chosen = -1,
                pickedName = null,
                isCorrect = false
            };
        }

        if (q.type == "pick3d")
        {
            ShowMCQ(false);
            SetText(optionsListText, "You may Rotate/Scale the model and tap the correct key.");
            SetText(feedbackText, "");
            _waitingPick3D = true;
            SetInteract(nextButton, false);
        }
        else
        {
            ShowMCQ(true);
            SetText(optionsListText, BuildOptionsList(q.options));
            for (int i = 0; i < optionButtons.Length; i++)
                SetInteract(optionButtons[i], true);
            SetInteract(nextButton, false);
        }

        if (progressText) progressText.color = progressDefault;
        if (feedbackText) feedbackText.color = feedbackDefault;
        SetInteract(backButton, _qIndex > 0);

        //if (speakQuestionAndOptions && aiSpeaker)
        //{
        //    // Only gate the TTS call, don't return from RenderQ()
        //    bool canSpeak = (QuizVoiceManager.Instance == null) || QuizVoiceManager.Instance.allowSpeak;

        //    if (canSpeak)
        //    {
        //        if (q.type == "pick3d")
        //        {
        //            aiSpeaker.SpeakText(q.prompt, CacheKeyFor(_qIndex, "prompt"));
        //        }
        //        else
        //        {
        //            var sb = new System.Text.StringBuilder();
        //            sb.Append(q.prompt);
        //            if (q.options != null && q.options.Length >= 4)
        //            {
        //                sb.Append(" Options: A. ").Append(q.options[0])
        //                  .Append(" B. ").Append(q.options[1])
        //                  .Append(" C. ").Append(q.options[2])
        //                  .Append(" D. ").Append(q.options[3]);
        //            }
        //            aiSpeaker.SpeakText(sb.ToString(), CacheKeyFor(_qIndex, "full"));
        //        }
        //    }
        //}
        ScheduleSpeak(includeOptions: q.type != "pick3d");
    }

    private void ScheduleSpeak(bool includeOptions = true)
    {
        if (!speakQuestionAndOptions || !aiSpeaker) return;
        if (QuizVoiceManager.Instance != null && !QuizVoiceManager.Instance.allowSpeak) return;

        if (_speakCo != null) StopCoroutine(_speakCo);
        _speakCo = StartCoroutine(Co_SpeakAfterFrame(includeOptions));
    }

    private IEnumerator Co_SpeakAfterFrame(bool includeOptions)
    {
        // wait until UI & shuffle are fully applied this frame
        yield return null;

        // build narration from the *current* question (post-shuffle)
        var q = GetCurrentQuestion();
        if (q == null) yield break;

        // optional: stop any stray playback
        QuizVoiceManager.Instance?.Stop();

        if (q.type == "pick3d" || !includeOptions)
        {
            aiSpeaker.SpeakText(q.prompt, CacheKeyForCurrent("prompt", false));
        }
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(q.prompt);
            if (q.options != null && q.options.Length >= 4)
            {
                sb.Append(" Options: A. ").Append(q.options[0])
                  .Append(" B. ").Append(q.options[1])
                  .Append(" C. ").Append(q.options[2])
                  .Append(" D. ").Append(q.options[3]);
            }
            aiSpeaker.SpeakText(sb.ToString(), CacheKeyForCurrent("full", true));
        }
    }

    public void RepeatSpeakCurrent(bool includeOptions = true)
    {
        if (!aiSpeaker) return;

        var q = GetCurrentQuestion();
        if (q == null) return;

        // Build narration text (same as in RenderQ)
        string text;
        string suffix;
        if (q.type == "pick3d" || !includeOptions)
        {
            text = q.prompt;
            suffix = "prompt_repeat";
        }
        else
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(q.prompt);
            if (q.options != null && q.options.Length >= 4)
            {
                sb.Append(" Options: A. ").Append(q.options[0])
                  .Append(" B. ").Append(q.options[1])
                  .Append(" C. ").Append(q.options[2])
                  .Append(" D. ").Append(q.options[3]);
            }
            text = sb.ToString();
            suffix = "full_repeat";
        }

        // Ensure TTS isn’t muted
        if (QuizVoiceManager.Instance != null && !QuizVoiceManager.Instance.allowSpeak)
            QuizVoiceManager.Instance.Unmute();

        // Best-effort: stop any shared AudioSource if you routed audio through the manager
        QuizVoiceManager.Instance?.Stop(); // safe no-op if not used

        // Use a UNIQUE cache key so AIVoiceSpeakerV1 won’t dedupe/skip this repeat
        var uniqueKey = CacheKeyFor(_qIndex, suffix) + "_" + System.DateTime.UtcNow.Ticks;

        // If your AIVoiceSpeakerV1 supports an "interrupt" or "priority" arg, pass it here.
        // Current signature seems (string text, string cacheKey), so:
        aiSpeaker.SpeakText(text, uniqueKey);
    }

    public void StopCurrentSpeak()
    {
        // No aiSpeaker.Stop(). We only stop the shared voice manager channel if used.
        QuizVoiceManager.Instance?.Stop();
    }



    void Update()
    {
        if (!_waitingPick3D || _answered || arCamera == null) return;

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;
            TryPickAt(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                if (EventSystem.current && EventSystem.current.IsPointerOverGameObject(t.fingerId)) return;
                TryPickAt(t.position);
            }
        }
#endif
    }

    private string CacheKeyFor(int qIndex, string suffix)
    {
        // Unique per model + session + question index
        return $"{_modelIdForAudio}_{_sessionIdForAudio}_q{qIndex}_{suffix}";
    }

    private void OnChooseOption(int ix)
    {
        if (_answered || _qIndex >= _queue.Count) return;
        var q = _queue[_qIndex];
        if (q.type != "mcq") return;

        bool correct = ix == q.correct;
        var hi = _histItems[_qIndex];
        if (hi != null)
        {
            hi.chosen = ix;
            hi.isCorrect = correct;
        }

        if (correct)
        {
            _answered = true;
            _states[_qIndex] = QState.Correct;
            PlaySfx(sfxCorrect);
            SetText(feedbackText, "Correct!");
            SetFeedbackColor(feedbackCorrect);
            for (int i = 0; i < optionButtons.Length; i++)
                SetInteract(optionButtons[i], false);
            if (autoAdvanceSeconds > 0f && gameObject.activeInHierarchy)
                _autoNextCo = StartCoroutine(AutoNextAfter(autoAdvanceSeconds));
        }
        else
        {
            PlaySfx(sfxWrong);
            SetText(feedbackText, "Try again.");
            SetFeedbackColor(feedbackWrong);
        }
    }

    private void TryPickAt(Vector2 pos)
    {
        var ray = arCamera.ScreenPointToRay(pos);
        if (Physics.Raycast(ray, out var hit, raycastMaxDistance, _tapMask))
        {
            var go = hit.collider.gameObject;
            string picked = Canon(KeyNameOf(go));

            var q = _queue[_qIndex];
            bool ok = MatchesAny(picked, q.accept_names);

            var hi = _histItems[_qIndex];
            if (hi != null)
            {
                hi.pickedName = picked;
                hi.isCorrect = ok;
            }
            HighlightOnce(go, ok ? OK : BAD);

            if (ok)
            {
                _answered = true;
                _states[_qIndex] = QState.Correct;
                PlaySfx(sfxCorrect);
                SetText(feedbackText, "Correct!");
                SetFeedbackColor(feedbackCorrect);
                if (autoAdvanceSeconds > 0f && gameObject.activeInHierarchy)
                    _autoNextCo = StartCoroutine(AutoNextAfter(autoAdvanceSeconds));
            }
            else
            {
                PlaySfx(sfxWrong);
                SetText(feedbackText, "Try again.");
                SetFeedbackColor(feedbackWrong);
            }
        }
    }

    static System.Random _rng = new System.Random();

    static void ShuffleOptions(QuizQuestion q)
    {
        if (q == null || q.type != "mcq" || q.options == null || q.options.Length < 2) return;

        // Build an index map 0..N-1
        int n = q.options.Length;
        var indices = new int[n];
        for (int i = 0; i < n; i++) indices[i] = i;

        // Fisher–Yates shuffle on indices
        for (int i = n - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Create new options and find new correct index
        var newOpts = new string[n];
        int newCorrect = -1;
        for (int i = 0; i < n; i++)
        {
            newOpts[i] = q.options[indices[i]];
            if (indices[i] == q.correct) newCorrect = i;
        }

        q.options = newOpts;
        q.correct = newCorrect;
    }

    private void SetFeedbackColor(Color c) { if (feedbackText) feedbackText.color = c; }

    public void Next()
    {
        CancelAutoAdvance();
        if (_qIndex < _queue.Count) _qIndex++;
        RenderQ();
    }

    public void Back()
    {
        CancelAutoAdvance();
        if (_qIndex > 0) _qIndex--;
        RenderQ();
    }

    // Legacy shim to satisfy old calls; AI flow starts the quiz elsewhere.
    public void StartQuizForModel(string modelName)
    {
        Debug.Log($"[Quiz] StartQuizForModel called for '{modelName}'. Ignored — AI flow handles starting after Save.");
        // no-op on purpose
    }

    public void Close()
    {
        CancelAutoAdvance();
        if (quizCanvas) quizCanvas.gameObject.SetActive(false);
    }

    private void ShowMCQ(bool on)
    {
        for (int i = 0; i < optionButtons.Length; i++)
            if (optionButtons[i]) optionButtons[i].gameObject.SetActive(on);
    }

    private void SetText(TextMeshProUGUI t, string v) { if (t) t.text = v; }
    private void SetInteract(Selectable s, bool v) { if (s) s.interactable = v; }

    private IEnumerator AutoNextAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        Next();
    }

    private void CancelAutoAdvance()
    {
        if (_autoNextCo != null)
        {
            StopCoroutine(_autoNextCo);
            _autoNextCo = null;
        }
    }

    private string BuildOptionsList(string[] opts)
    {
        if (opts == null) return "";
        string[] labels = { "A", "B", "C", "D" };
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < 4; i++)
        {
            string line = (i < opts.Length ? opts[i] : "");
            sb.Append(labels[i]).Append(" - ").Append(line);
            if (i < 3) sb.Append('\n');
        }
        return sb.ToString();
    }

    private int Score()
    {
        int s = 0;
        for (int i = 0; i < _states.Count; i++)
            if (_states[i] == QState.Correct) s++;
        return s;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource && clip) audioSource.PlayOneShot(clip);
    }

    private void HighlightOnce(GameObject go, Color c)
    {
        if (_lastRenderer) _lastRenderer.material.color = _lastColor;
        var r = go.GetComponentInChildren<Renderer>();
        if (r)
        {
            _lastRenderer = r; _lastColor = r.material.color;
            r.material.color = c;
            CancelInvoke(nameof(ClearHighlight));
            Invoke(nameof(ClearHighlight), 0.2f);
        }
    }
    private void ClearHighlight()
    {
        if (_lastRenderer) _lastRenderer.material.color = _lastColor;
        _lastRenderer = null;
    }

    private static string Canon(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char ch in s) if (char.IsLetter(ch)) sb.Append(char.ToLowerInvariant(ch));
        return sb.ToString();
    }
    private static bool MatchesAny(string pickedCanon, string[] accepted)
    {
        if (accepted == null || accepted.Length == 0) return false;
        string p = Canon(pickedCanon);
        foreach (var a in accepted) if (p.Contains(Canon(a))) return true;
        return false;
    }
    private static string KeyNameOf(GameObject go)
    {
        var tag = go.GetComponent<KeyTag>();
        if (tag && !string.IsNullOrEmpty(tag.keyName)) return tag.keyName;
        return go.name;
    }
}
