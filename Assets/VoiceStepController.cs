using UnityEngine;

public class VoiceStepController : MonoBehaviour
{
    [Header("Enable/disable voice step control")]
    public bool enableStepVoice = true;
    public float voiceStepCooldown = 0.75f;

    private float _lastVoiceTime = -999f;

    /// <summary>
    /// Call this from your SpeechToText listener with the recognized text.
    /// </summary>
    public void HandleVoiceCommand(string spokenText)
    {
        if (!enableStepVoice || StepState.I == null || string.IsNullOrEmpty(spokenText))
            return;

        string cmd = spokenText.ToLower().Trim();
        bool cooled = (Time.unscaledTime - _lastVoiceTime) >= voiceStepCooldown;

        bool sayNext = cmd.StartsWith("next") || cmd.Contains(" next") || cmd.Contains("next step");
        bool sayBack = cmd.StartsWith("back") || cmd.Contains(" back") || cmd.Contains("previous");

        if (cooled && sayNext)
        {
            StepState.I.NextStep();
            _lastVoiceTime = Time.unscaledTime;
            Debug.Log("🗣 Voice command: NEXT step");
        }
        else if (cooled && sayBack)
        {
            StepState.I.PrevStep();
            _lastVoiceTime = Time.unscaledTime;
            Debug.Log("🗣 Voice command: BACK step");
        }
    }
}
