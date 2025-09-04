using System;
using UnityEngine;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine.Networking;
using System.Collections.Generic;

public class TTSManager : MonoBehaviour
{
    private OpenAIWrapper openAIWrapper;
    [SerializeField] private AudioPlayer audioPlayer;
    [SerializeField] private TTSModel model = TTSModel.TTS_1;
    [SerializeField] private TTSVoice voice = TTSVoice.Alloy;
    [SerializeField, Range(0.25f, 4.0f)] private float speed = 1f;
    readonly Dictionary<string, string> _knownPaths = new();

    private void OnEnable()
    {
        if (!openAIWrapper) this.openAIWrapper = FindObjectOfType<OpenAIWrapper>();
        if (!audioPlayer) this.audioPlayer = GetComponentInChildren<AudioPlayer>();
    }

    private void OnValidate() => OnEnable();

    //public async void SynthesizeAndPlay(string text)
    //{
    //    Debug.Log("Trying to synthesize " + text);
    //    byte[] audioData = await openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
    //    if (audioData != null)
    //    {
    //        Debug.Log("Playing audio.");
    //        audioPlayer.ProcessAudioBytes(audioData);
    //    }
    //    else Debug.LogError("Failed to get audio data from OpenAI.");
    //}

    public async void SynthesizeAndPlay(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        var np = FindObjectOfType<NarrationPlayerPersistent>();
        if (np != null)
        {
            np.Speak(text);   // centralizes caching + playback → no first-run double
            return;
        }

        // fallback if no NarrationPlayerPersistent in scene
        byte[] audioData = await openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
        if (audioData != null)
            audioPlayer.ProcessAudioBytes(audioData);
        else
            Debug.LogError("Failed to get audio data from OpenAI.");
    }



    public void SynthesizeAndPlay(string text, TTSModel model, TTSVoice voice, float speed)
    {
        var who = new System.Diagnostics.StackTrace().GetFrame(1)?.GetMethod();
        string caller = who == null ? "?" : $"{who.DeclaringType?.Name}.{who.Name}";
        int h = (text ?? "").GetHashCode();
        Debug.Log($"[TTS] CALL by {caller}  hash={h}  first20='{(text?.Length > 20 ? text[..20] : text)}'");
        this.model = model;
        this.voice = voice;
        this.speed = speed;
        SynthesizeAndPlay(text);
    }
}