using UnityEngine;

public class QuizVoiceManager : MonoBehaviour
{
    public static QuizVoiceManager Instance;
    [SerializeField] AudioSource audioSource;   // Assign in Inspector
    public bool allowSpeak = true;              // Gate switch

    void Awake() => Instance = this;

    public void Speak(string text, bool force = false)
    {
        if (!allowSpeak && !force) return;
        Stop(); // Cancel any previous TTS

        // TODO: Replace with your actual TTS call
        // Example: YourTTSPlugin.Speak(text);
    }

    public void Stop()
    {
        // Stop audio or plugin playback
        if (audioSource && audioSource.isPlaying)
            audioSource.Stop();
        // Example: YourTTSPlugin.Stop();
    }

    public void Mute() => allowSpeak = false;
    public void Unmute() => allowSpeak = true;
}
