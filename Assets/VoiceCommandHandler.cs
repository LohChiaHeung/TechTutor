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

    private void Awake()
    {
        SpeechToText.Initialize("en-US");
        StartSpeechToTextButton.onClick.AddListener(ToggleSpeechToText);
    }

    private void Update()
    {
        StartSpeechToTextButton.interactable = SpeechToText.IsServiceAvailable(PreferOfflineRecognition) || isListening;
        VoiceLevelSlider.value = Mathf.Lerp(VoiceLevelSlider.value, normalizedVoiceLevel, 15f * Time.unscaledDeltaTime);
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