using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpeechToText_Gmail : MonoBehaviour, ISpeechToTextListener
{
    public TextMeshProUGUI SpeechText;
    public Button StartSpeechToTextButton;
    public Slider VoiceLevelSlider;
    public bool PreferOfflineRecognition;
    public Image SpeechToggleImage;
    public Sprite micOnSprite;
    public Sprite micOffSprite;

    public ARStepManager stepManager;  // ✅ ONLY for step navigation now

    private float normalizedVoiceLevel;
    private bool isListening = false;

    [Header("TTS Handling")]
    public AudioSource ttsAudio;   // drag your TTS/narrator AudioSource here
    public float resumeDelay = 0.35f; // small buffer before restarting STT

    bool wasListeningBeforeTTS;
    bool wasTTSPlaying;

    private void Awake()
    {
        SpeechToText.Initialize("en-US");
        StartSpeechToTextButton.onClick.AddListener(ToggleSpeechToText);
    }

    private void Update()
    {
        StartSpeechToTextButton.interactable = SpeechToText.IsServiceAvailable(PreferOfflineRecognition) || isListening;
        VoiceLevelSlider.value = Mathf.Lerp(VoiceLevelSlider.value, normalizedVoiceLevel, 15f * Time.unscaledDeltaTime);

        // 🔍 detect TTS start/stop
        bool ttsPlaying = ttsAudio && ttsAudio.isPlaying;

        if (ttsPlaying && !wasTTSPlaying)
            OnTTSStarted();
        else if (!ttsPlaying && wasTTSPlaying)
            OnTTSEnded();

        wasTTSPlaying = ttsPlaying;
    }

    public void ToggleSpeechToText()
    {
        Debug.Log("🎤 [VoiceChat] Mic button clicked!");

        if (isListening)
        {
            isListening = false;
            SpeechToText.ForceStop();
            SpeechText.text = "Recognition stopped.";
            if (SpeechToggleImage != null) SpeechToggleImage.sprite = micOffSprite;
        }
        else
        {
            SpeechToText.RequestPermissionAsync((permission) =>
            {
                if (permission == SpeechToText.Permission.Granted)
                {
                    if (SpeechToText.Start(this, preferOfflineRecognition: PreferOfflineRecognition))
                    {
                        isListening = true;
                        SpeechText.text = "Listening...";
                        if (SpeechToggleImage != null) SpeechToggleImage.sprite = micOnSprite;
                    }
                    else
                    {
                        SpeechText.text = "Couldn't start speech recognition!";
                    }
                }
                else
                {
                    SpeechText.text = "Microphone permission denied!";
                }
            });
        }
    }

    private void StartSpeechRecognition()
    {
        SpeechToText.RequestPermissionAsync((permission) =>
        {
            if (permission == SpeechToText.Permission.Granted)
            {
                if (SpeechToText.Start(this, preferOfflineRecognition: PreferOfflineRecognition))
                {
                    SpeechText.text = "🎤 Listening...";
                    isListening = true;
                }
                else
                {
                    SpeechText.text = "Couldn't start speech recognition!";
                    isListening = false;
                }
            }
            else
            {
                SpeechText.text = "Microphone permission denied!";
                isListening = false;
            }
        });
    }

    private void StopListening()
    {
        SpeechToText.ForceStop();
        isListening = false;
        SpeechText.text = "🛑 Stopped listening.";
    }

    void ISpeechToTextListener.OnReadyForSpeech() => Debug.Log("OnReadyForSpeech");
    void ISpeechToTextListener.OnBeginningOfSpeech() => Debug.Log("OnBeginningOfSpeech");

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

            // ✅ Gmail AR Tutorial Commands
            if (cmd.Contains("next"))
            {
                Debug.Log("🔊 Voice Command: Next Step");
                stepManager?.NextStep();
            }
            else if (cmd.Contains("back") || cmd.Contains("previous"))
            {
                Debug.Log("🔊 Voice Command: Previous Step");
                stepManager?.BackStep();
            }
        }

        // Continue listening loop
        if (isListening)
            StartCoroutine(RestartAfterDelay(0.6f));
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isListening && !SpeechToText.IsBusy())
        {
            StartSpeechRecognition();
        }
    }

    void OnTTSStarted()
    {
        if (isListening)
        {
            wasListeningBeforeTTS = true;
            StopListening(); // your existing method
            Debug.Log("[Voice] Paused STT because TTS started.");
        }
        else
        {
            wasListeningBeforeTTS = false;
        }
    }

    void OnTTSEnded()
    {
        if (wasListeningBeforeTTS)
            StartCoroutine(ResumeSTTAfter(resumeDelay));
        Debug.Log("[Voice] Resuming STT after TTS ended.");
    }

    IEnumerator ResumeSTTAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        StartSpeechRecognition(); // your existing method
    }
}
