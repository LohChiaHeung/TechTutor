using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Vuforia;

public class ModelSwipeFeedback : MonoBehaviour
{
    [Header("Swiper (assign your Vuforia selector)")]
    public VuforiaImageSwipeSelector_ExistingChildren selector;

    [Header("Optional: UI arrows (if you also have UI Buttons)")]
    public Button leftArrowButton;
    public Button rightArrowButton;

    [Header("Label (3D or UGUI)")]
    [Tooltip("Supports TextMeshPro (3D) or TextMeshProUGUI. Drag your 3D text or UI text here.")]
    public TMP_Text modelLabel;
    [Tooltip("Shown before any selection is made.")]
    public string defaultLabel = "Select Model to Start The Quiz";
    [Tooltip("Prefix for the live label once a model is selected.")]
    public string labelPrefix = "Model Selection : ";
    [Tooltip("Uppercase the model name for emphasis.")]
    public bool uppercaseModelName = false;

    [Header("Behavior")]
    [Tooltip("If true, the FIRST detected model will NOT play the swipe SFX.")]
    public bool muteSwipeOnFirstAppearance = true;

    [Tooltip("If true, the FIRST detected model will NOT play the voice.")]
    public bool muteVoiceOnFirstAppearance = false;

    [Header("Tracking gate (Vuforia)")]
    [Tooltip("Only play sounds when the ImageTarget is tracked (TRACKED/EXTENDED_TRACKED).")]
    public bool playOnlyWhenTracked = true;              // default ON
    public ObserverBehaviour imageTargetObserver;
    private bool _isTracked = false;

    private bool _firstApplied = false; // tracks if we've handled the first model yet

    [Tooltip("Update the label even when not tracked (no sounds).")]
    public bool previewLabelWhenNotTracked = true;
    private string _lastPreviewName = null;

    [Header("Audio")]
    public AudioSource sfxSource;     // short tick/whoosh
    public AudioClip swipeSfx;
    public AudioSource voiceSource;   // narrator “keyboard/mouse/…”
    [Serializable] public class NamedClip { public string key; public AudioClip clip; }
    [Tooltip("Map keywords to voice clips (keys like: keyboard, mouse, laptop, monitor, speaker).")]
    public List<NamedClip> voiceClips = new List<NamedClip>();

    private string _lastName;

    void Awake()
    {
        if (leftArrowButton) leftArrowButton.onClick.AddListener(OnLeft);
        if (rightArrowButton) rightArrowButton.onClick.AddListener(OnRight);

        if (selector)
        {
            selector.OnModelChanged.AddListener(OnModelChanged);
        }

        // Draw initial label only (silent)
        if (modelLabel) modelLabel.text = string.IsNullOrEmpty(defaultLabel) ? "" : defaultLabel;
    }


    void OnDestroy()
    {
        if (selector)
        {
            selector.OnModelChanged.RemoveListener(OnModelChanged);
        }
    }

    // === Arrow handlers (your 3D arrows call UI_Next/UI_Prev on the selector directly; these are for UI Buttons) ===
    private void OnLeft() { if (selector) selector.UI_Prev(); }
    private void OnRight() { if (selector) selector.UI_Next(); }

    // === Event from selector ===
    private void OnModelChanged(string name)
    {
        if (string.IsNullOrEmpty(name)) name = CurrentModelName();

        // NEW: de-dupe — if we already applied this name, do nothing
        if (string.Equals(name, _lastName, StringComparison.Ordinal)) return;

        ApplyChange(name, playAudio: true);
    }


    // === Fallback polling (in case the event didn’t fire for some reason) ===
    void Update()
    {
        if (!selector) return;

        string nameNow = CurrentModelName();

        // Silent preview before tracking
        if (playOnlyWhenTracked && !_isTracked)
        {
            if (previewLabelWhenNotTracked && nameNow != _lastPreviewName)
            {
                _lastPreviewName = nameNow;
                if (!string.IsNullOrEmpty(nameNow))
                    modelLabel.text = labelPrefix + (uppercaseModelName ? Pretty(nameNow).ToUpperInvariant() : Pretty(nameNow));
                else
                    modelLabel.text = defaultLabel;
            }
            return;
        }

        // We have tracking now. Since we're subscribed to OnModelChanged,
        // DO NOT call ApplyChange here (prevents double fire).
        // (If you weren't subscribed to events, you could fall back to polling here.)
    }



    // === Core: update label + play sounds ===
    private void ApplyChange(string rawName, bool playAudio)
    {
        if (string.IsNullOrEmpty(rawName))
        {
            SetLabel(defaultLabel);
            return;
        }

        bool isFirst = !_firstApplied;
        _firstApplied = true;

        _lastName = rawName;

        string pretty = Pretty(rawName);
        if (uppercaseModelName) pretty = pretty.ToUpperInvariant();
        SetLabel(labelPrefix + pretty);

        if (!playAudio) return;

        // Swipe SFX: optionally mute on first
        if (!(isFirst && muteSwipeOnFirstAppearance) && sfxSource && swipeSfx)
            sfxSource.PlayOneShot(swipeSfx);

        // Voice: optionally mute on first (you set this to false so voice plays on first)
        if (!(isFirst && muteVoiceOnFirstAppearance) && voiceSource)
        {
            var clip = FindVoiceClip(pretty);
            if (clip)
            {
                voiceSource.Stop();
                voiceSource.PlayOneShot(clip);
            }
        }
    }



    private void SetLabel(string text)
    {
        if (modelLabel) modelLabel.text = text;
    }

    private AudioClip FindVoiceClip(string modelName)
    {
        string k = Canon(modelName);
        foreach (var nc in voiceClips)
        {
            if (nc == null || nc.clip == null || string.IsNullOrEmpty(nc.key)) continue;
            if (k.Contains(Canon(nc.key))) return nc.clip; // loose match: "KeyboardModel" matches "keyboard"
        }
        return null;
    }

    private string CurrentModelName()
    {
        if (!selector) return "";
        try { return selector.CurrentModelName; } catch { return ""; }
    }

    // Helpers
    private static string Pretty(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = s.Replace("_", " ").Replace("Model", "").Replace("_Model", "").Trim();
        if (s.Length == 0) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
    private static string Canon(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        var sb = new System.Text.StringBuilder(s.Length);
        foreach (char c in s) if (char.IsLetter(c)) sb.Append(char.ToLowerInvariant(c));
        return sb.ToString();
    }

    void OnEnable()
    {
        if (imageTargetObserver)
            imageTargetObserver.OnTargetStatusChanged += OnTargetStatusChanged;
    }

    void OnDisable()
    {
        if (imageTargetObserver)
            imageTargetObserver.OnTargetStatusChanged -= OnTargetStatusChanged;
    }

    private void OnTargetStatusChanged(ObserverBehaviour obs, TargetStatus status)
    {
        bool nowTracked = status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED;
        bool justGained = !_isTracked && nowTracked;
        _isTracked = nowTracked;

        // When we first gain tracking, speak the current model (respect first-appearance flags)
        if (playOnlyWhenTracked && justGained)
        {
            var nameNow = CurrentModelName();
            if (!string.IsNullOrEmpty(nameNow))
                ApplyChange(nameNow, playAudio: true);
        }
    }

}
