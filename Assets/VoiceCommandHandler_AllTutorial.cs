// File: VoiceCommandHandler.cs
// Keeps your mic on/off UI, STT flow, and model triggers.
// New: pauses listening while any watched AudioSource is playing (TTS or panel audio),
//      then auto-resumes when audio stops. Removed: stepController coupling.

using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class VoiceCommandHandler_AllTutorial : MonoBehaviour, ISpeechToTextListener
{
    [Header("UI & STT")]
    public TextMeshProUGUI SpeechText;
    public Button StartSpeechToTextButton;
    public Slider VoiceLevelSlider;
    public bool PreferOfflineRecognition;
    public Image SpeechToggleImage;
    public Sprite micOnSprite;
    public Sprite micOffSprite;

    [Header("AR Hook")]
    public PlaneLocker planeLocker;  // keep using PlaneLocker for model switching

    private float normalizedVoiceLevel;
    private bool isListening = false;

    // ======== AUDIO FOCUS (NEW) ========
    [Header("Audio Focus")]
    [Tooltip("Your TTS / narrator AudioSource (high priority). Optional but recommended.")]
    public AudioSource ttsAudio;

    [Tooltip("Any other audio that should pause STT (panel voice, tap handlers, etc.). Optional.")]
    public AudioSource[] extraAudioToWatch;

    [Tooltip("Delay before resuming STT after all audio ends.")]
    public float resumeDelay = 0.35f;

    bool wasListeningBeforeAudio;
    bool anyAudioWasPlaying;

    void Awake()
    {
        SpeechToText.Initialize("en-US");
        StartSpeechToTextButton.onClick.AddListener(ToggleSpeechToText);
    }

    void Update()
    {
        // UI interactivity + VU meter
        StartSpeechToTextButton.interactable =
            SpeechToText.IsServiceAvailable(PreferOfflineRecognition) || isListening;
        VoiceLevelSlider.value = Mathf.Lerp(VoiceLevelSlider.value, normalizedVoiceLevel, 15f * Time.unscaledDeltaTime);

        // Detect "any audio started / ended"
        bool anyAudioPlaying = IsAnyAudioPlaying();
        if (anyAudioPlaying && !anyAudioWasPlaying) OnAnyAudioStarted();
        else if (!anyAudioPlaying && anyAudioWasPlaying) OnAnyAudioEnded();
        anyAudioWasPlaying = anyAudioPlaying;
    }

    // === STT Toggle ===
    public void ToggleSpeechToText()
    {
        if (isListening)
        {
            StopListening();
            return;
        }

        // if audio is currently playing, we won't start STT now
        if (IsAnyAudioPlaying())
        {
            SpeechText.text = "Voice is playing… will start listening after it ends.";
            return;
        }

        StartSpeechRecognition();
    }

    // === STT helpers ===
    void StartSpeechRecognition()
    {
        SpeechToText.RequestPermissionAsync((permission) =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                if (SpeechToText.Start(this, preferOfflineRecognition: PreferOfflineRecognition))
                {
                    isListening = true;
                    SpeechText.text = "🎤 Listening...";
                    if (SpeechToggleImage) SpeechToggleImage.sprite = micOnSprite;
                }
                else
                {
                    SpeechText.text = "Couldn't start speech recognition!";
                    isListening = false;
                    if (SpeechToggleImage) SpeechToggleImage.sprite = micOffSprite;
                }
            }
            else
            {
                SpeechText.text = "Microphone permission denied!";
                isListening = false;
                if (SpeechToggleImage) SpeechToggleImage.sprite = micOffSprite;
            }
        });
    }

    void StopListening()
    {
        SpeechToText.ForceStop();
        isListening = false;
        SpeechText.text = "🛑 Stopped listening.";
        if (SpeechToggleImage) SpeechToggleImage.sprite = micOffSprite;
    }

    // === AUDIO FOCUS (NEW) ===
    bool IsAnyAudioPlaying()
    {
        if (ttsAudio && ttsAudio.isPlaying) return true;
        if (extraAudioToWatch != null)
        {
            for (int i = 0; i < extraAudioToWatch.Length; i++)
                if (extraAudioToWatch[i] && extraAudioToWatch[i].isPlaying) return true;
        }
        return false;
    }

    void OnAnyAudioStarted()
    {
        if (isListening)
        {
            wasListeningBeforeAudio = true;
            StopListening(); // pause STT while audio plays
            Debug.Log("[Voice] Paused STT (audio started).");
        }
        else
        {
            wasListeningBeforeAudio = false;
        }
    }

    void OnAnyAudioEnded()
    {
        // Only resume if we were listening before audio preempted us
        if (wasListeningBeforeAudio)
            StartCoroutine(ResumeSTTAfter(resumeDelay));

        Debug.Log("[Voice] Audio ended; scheduling STT resume if needed.");
    }

    IEnumerator ResumeSTTAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!IsAnyAudioPlaying()) // still safe to resume
            StartSpeechRecognition();
    }

    // === ISpeechToTextListener ===
    void ISpeechToTextListener.OnReadyForSpeech() { Debug.Log("OnReadyForSpeech"); }
    void ISpeechToTextListener.OnBeginningOfSpeech() { Debug.Log("OnBeginningOfSpeech"); }
    void ISpeechToTextListener.OnVoiceLevelChanged(float level)
    {
        normalizedVoiceLevel = level;
    }
    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        SpeechText.text = spokenText;
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        if (!string.IsNullOrEmpty(spokenText))
        {
            string cmd = spokenText.ToLower().Trim();

            // Simple keywords → model indices (PlaneLocker handles previews/confirm etc.)
            if (cmd.Contains("keyboard tutorial"))
            {
                Debug.Log("✅ Triggered: Keyboard AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(0);
            }
            else if (cmd.Contains("mouse tutorial"))
            {
                Debug.Log("✅ Triggered: Mouse AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(1);
            }
            else if (cmd.Contains("monitor tutorial"))
            {
                Debug.Log("✅ Triggered: Monitor AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(2);
            }
            else if (cmd.Contains("laptop tutorial"))
            {
                Debug.Log("✅ Triggered: Laptop AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(3);
            }
            else if (cmd.Contains("speaker tutorial"))
            {
                Debug.Log("✅ Triggered: Speaker AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(4);
            }
        }

        // (Removed) stepController forwarding — per your request.

        // Only restart looped recognition if we are still in "listening" mode
        if (isListening)
            StartCoroutine(RestartAfterDelay(0.6f));
    }

    // inside VoiceCommandHandler_AllTutorial
    public void RegisterExtraAudio(AudioSource src)
    {
        if (!src) return;
        if (extraAudioToWatch == null) extraAudioToWatch = new AudioSource[0];
        foreach (var a in extraAudioToWatch) if (a == src) return; // already tracked
        var arr = new AudioSource[extraAudioToWatch.Length + 1];
        extraAudioToWatch.CopyTo(arr, 0);
        arr[arr.Length - 1] = src;
        extraAudioToWatch = arr;
    }

    IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        // don't restart if another audio began
        if (isListening && !SpeechToText.IsBusy() && !IsAnyAudioPlaying())
            StartSpeechRecognition();
    }
}
