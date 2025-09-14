using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpeechToTextDemo : MonoBehaviour, ISpeechToTextListener
{
    public TextMeshProUGUI SpeechText;
    public Button StartSpeechToTextButton;
    public Slider VoiceLevelSlider;
    public bool PreferOfflineRecognition;
    public Image SpeechToggleImage;
    public Sprite micOnSprite;
    public Sprite micOffSprite;

    public PlaneLocker planeLocker; 
    private float normalizedVoiceLevel;
    private bool isListening = false;
    public VoiceStepController stepController;

    [Header("TTS Handling")]
    public AudioSource ttsAudio;   // drag your TTS/narrator AudioSource here
    public float resumeDelay = 0.35f; // small buffer before restarting STT

    bool wasListeningBeforeTTS;
    bool wasTTSPlaying;

    private void Awake()
    {
        SpeechToText.Initialize("en-US");
        StartSpeechToTextButton.onClick.AddListener(ToggleSpeechToText);

        // Auto-find narration AudioSource if not assigned
        if (!ttsAudio)
        {
            var narr = FindObjectOfType<NarrationPlayerPersistent>(true);
            if (narr)
            {
                var src = narr.GetComponent<AudioSource>();
                if (src) ttsAudio = src;
                Debug.Log("[Voice] Auto-assigned NarrationPlayerPersistent AudioSource to ttsAudio.");
            }
        }
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
    void ISpeechToTextListener.OnReadyForSpeech() => Debug.Log("OnReadyForSpeech");
    void ISpeechToTextListener.OnBeginningOfSpeech() => Debug.Log("OnBeginningOfSpeech");

    void ISpeechToTextListener.OnVoiceLevelChanged(float level)
    {
        normalizedVoiceLevel = level;
    }

    void ISpeechToTextListener.OnPartialResultReceived(string spokenText)
    {
        if (ttsAudio && ttsAudio.isPlaying) return; // ignore while TTS
        SpeechText.text = spokenText;
    }

    void ISpeechToTextListener.OnResultReceived(string spokenText, int? errorCode)
    {
        if (ttsAudio && ttsAudio.isPlaying) return;

        if (!string.IsNullOrEmpty(spokenText))
        {
            string cmd = spokenText.ToLower().Trim();

            if (cmd.Contains("keyboard tutorial"))
            {
                Debug.Log("✅ Triggered: Keyboard AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(0); // index 0 = keyboard
            }
            else if (cmd.Contains("mouse tutorial"))
            {
                Debug.Log("✅ Triggered: Mouse AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(1); // index 1 = mouse
            }
            else if (cmd.Contains("monitor tutorial")) 
            {
                Debug.Log("✅ Triggered: Monitor AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(2); // index 2 = monitor
            }
            else if (cmd.Contains("laptop tutorial"))
            {
                Debug.Log("✅ Triggered: Laptop AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(3); // index 2 = laptop
            }
            else if (cmd.Contains("speaker tutorial"))
            {
                Debug.Log("✅ Triggered: Speaker AR Tutorial");
                planeLocker.SpawnPreviewForSelectedModel(4); // index 2 = speaker
            }
        }

        // Only restart if still in listening mode
        if (isListening)
            StartCoroutine(RestartAfterDelay(0.6f));

        if (stepController != null)
            stepController.HandleVoiceCommand(spokenText);
    }

    private IEnumerator RestartAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isListening && !SpeechToText.IsBusy())
        {
            StartSpeechRecognition();
        }
    }
}