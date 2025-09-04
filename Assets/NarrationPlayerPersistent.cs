using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NarrationPlayerPersistent : MonoBehaviour
{
    [Header("Drag your OpenAI TTS Manager here (from the package)")]
    public MonoBehaviour ttsManager; // e.g., the plugin's manager component

    [Header("Options")]
    public bool interruptPrevious = true;
    public string folderName = "tts"; // under persistentDataPath

    // Use the same config types as your TTSManager
    [SerializeField] private OpenAIWrapper openAIWrapper;  // drag from scene (same as in TTSManager) or auto-find
    [SerializeField] private TTSModel model = TTSModel.TTS_1;
    [SerializeField] private TTSVoice voice = TTSVoice.Alloy;
    [SerializeField, Range(0.25f, 4f)] private float speed = 1f;
    private readonly HashSet<string> _inFlight = new HashSet<string>();
    private AudioSource source;
    private readonly Dictionary<string, AudioClip> memoryCache = new(); // key -> clip

    string Root => Path.Combine(Application.persistentDataPath, folderName);

    void Awake()
    {
        if (!openAIWrapper) openAIWrapper = FindObjectOfType<OpenAIWrapper>();
        source = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        Directory.CreateDirectory(Root);
    }

    public void Stop()
    {
        if (source.isPlaying) source.Stop();
        // If plugin has Cancel/Stop API, call it here.
    }

    public void SpeakFromPanel(PanelNarration panel)
    {
        if (!panel) return;
        Speak(panel.BuildNarrationText());
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        string key = Hash(text);
        string path = PathFor(key);

        if (interruptPrevious) Stop();

        // 1) In-memory cache
        if (memoryCache.TryGetValue(key, out var cached) && cached)
        {
            Play(cached);
            return;
        }

        // 2) Disk cache
        if (File.Exists(path))
        {
            Debug.Log($"[NarrationPlayerPersistent] Loaded existing TTS file for key '{key}' from: {path}");
            StartCoroutine(WavUtil.LoadWavClip(path, clip =>
            {
                if (clip)
                {
                    memoryCache[key] = clip;
                    Play(clip);
                }
                else
                {
                    // fallback to synth if load fails
                    Debug.Log($"[NarrationPlayerPersistent] No local TTS found for key '{key}'. Generating via OpenAI API...");
                    StartCoroutine(SynthesizeAndPersist(text, key, path));
                }
            }));
            return;
        }

        // 3) Not cached → synthesize, save, play
        //StartCoroutine(SynthesizeAndPersist(text, key, path));
        //Debug.Log($"[NarrationPlayerPersistent] Saving new TTS audio for key '{key}' to: {path}");
        StartSynthIfNeeded(text, key, path);

    }

    void StartSynthIfNeeded(string text, string key, string path)
    {
        if (_inFlight.Contains(key))
        {
            Debug.Log($"[Narration] Synth already in-flight; suppress duplicate key={key[..8]}");
            return;
        }
        _inFlight.Add(key);
        Debug.Log($"[Narration] SYNTH start key={key[..8]} -> {path}");
        StartCoroutine(SynthesizeAndPersist(text, key, path));
    }

    //IEnumerator SynthesizeAndPersist(string text, string key, string path)
    //{
    //    if (!openAIWrapper)
    //    {
    //        Debug.LogWarning("[NarrationPlayerPersistent] OpenAIWrapper is not assigned (and not found).");
    //        yield break;
    //    }

    //    // 1) Request MP3 bytes from OpenAI (await the Task in a coroutine-friendly way)
    //    var task = openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
    //    while (!task.IsCompleted) yield return null;
    //    var audioBytes = task.Result;

    //    if (audioBytes == null || audioBytes.Length == 0)
    //    {
    //        Debug.LogWarning("[NarrationPlayerPersistent] OpenAI returned no audio bytes.");
    //        yield break;
    //    }

    //    // 2) Write a temp MP3, then load it as an AudioClip
    //    string tempMp3 = Path.Combine(Application.persistentDataPath, "tts_tmp.mp3");
    //    try { File.WriteAllBytes(tempMp3, audioBytes); }
    //    catch (Exception e)
    //    {
    //        Debug.LogWarning("[NarrationPlayerPersistent] Failed writing temp MP3: " + e.Message);
    //        yield break;
    //    }

    //    using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + tempMp3, UnityEngine.AudioType.MPEG))
    //    {
    //        yield return www.SendWebRequest();
    //        if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
    //        {
    //            Debug.LogWarning("[NarrationPlayerPersistent] Failed to decode MP3: " + www.error);
    //            yield break;
    //        }

    //        var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
    //        if (!clip)
    //        {
    //            Debug.LogWarning("[NarrationPlayerPersistent] Decoded clip was null.");
    //            yield break;
    //        }

    //        // 3) Play now
    //        Play(clip);

    //        // 4) Save a persistent WAV for next time (and cache in memory)
    //        try
    //        {
    //            WavUtil.SaveWav(path, clip);
    //            memoryCache[key] = clip;
    //            Debug.Log("[NarrationPlayerPersistent] Saved TTS to: " + path);
    //        }
    //        catch (Exception e)
    //        {
    //            Debug.LogWarning("[NarrationPlayerPersistent] Failed saving WAV: " + e.Message);
    //        }
    //    }

    //    // 5) Cleanup temp MP3
    //    try { if (File.Exists(tempMp3)) File.Delete(tempMp3); } catch { }
    //}

    IEnumerator SynthesizeAndPersist(string text, string key, string path)
    {
        try
        {
            if (!openAIWrapper)
            {
                Debug.LogWarning("[Narration] OpenAIWrapper missing.");
                yield break;
            }

            var task = openAIWrapper.RequestTextToSpeech(text, model, voice, speed);
            while (!task.IsCompleted) yield return null;
            var audioBytes = task.Result;

            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogWarning("[Narration] OpenAI returned no audio bytes.");
                yield break;
            }

            string tempMp3 = Path.Combine(Application.persistentDataPath, "tts_tmp.mp3");
            try { File.WriteAllBytes(tempMp3, audioBytes); } catch (Exception e) { Debug.LogWarning("[Narration] Temp write failed: " + e.Message); yield break; }

            using var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip("file://" + tempMp3, UnityEngine.AudioType.MPEG);
            yield return www.SendWebRequest();
            if (www.result != UnityEngine.Networking.UnityWebRequest.Result.Success) { Debug.LogWarning("[Narration] MP3 decode failed: " + www.error); yield break; }

            var clip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
            if (!clip) { Debug.LogWarning("[Narration] Decoded clip null."); yield break; }

            Debug.Log($"[Narration] PLAY synth key={key[..8]}");
            Play(clip);

            try
            {
                WavUtil.SaveWav(path, clip);
                memoryCache[key] = clip;
                Debug.Log($"[Narration] SAVED wav key={key[..8]} -> {path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Narration] Save WAV failed: " + e.Message);
            }
            finally
            {
                try { if (File.Exists(tempMp3)) File.Delete(tempMp3); } catch { }
            }
        }
        finally
        {
            _inFlight.Remove(key); // ← allow future speaks of the same line
        }
    }
    void Play(AudioClip clip)
    {
        source.clip = clip;
        source.Play();
    }

    static string Hash(string input)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    string PathFor(string key) => Path.Combine(Root, key + ".wav");
}
