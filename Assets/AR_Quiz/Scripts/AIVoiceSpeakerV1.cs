using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIVoiceSpeakerV1 : MonoBehaviour
{
    [Header("Config")]
    public OpenAIConfigSO config;
    public AudioSource audioSource;

    [Header("Voice")]
    [Tooltip("TTS voice preset. Examples: 'alloy', 'verse', 'aria'.")]
    public string voice = "alloy";
    [Tooltip("Folder name under persistentDataPath for cached audio.")]
    public string cacheFolderName = "TechTutorQuizzesAudio";

    private string CacheRoot => Path.Combine(Application.persistentDataPath, cacheFolderName);

    [System.Serializable]
    private class TtsRequest
    {
        public string model;
        public string voice;
        public string input;
        public string format;
    }

    void Awake()
    {
        if (!Directory.Exists(CacheRoot)) Directory.CreateDirectory(CacheRoot);
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void SpeakText(string text, string cacheKey)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        StartCoroutine(Co_Speak(text, cacheKey, play: true));
    }

    public void CacheText(string text, string cacheKey)  // pre-cache only (no playback)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        StartCoroutine(Co_Speak(text, cacheKey, play: false));
    }

    private IEnumerator Co_Speak(string text, string cacheKey, bool play)
    {
        if (!config || string.IsNullOrEmpty(config.apiKey))
        {
            Debug.LogError("[AIVoiceSpeakerV1] Missing OpenAIConfigSO/apiKey.");
            yield break;
        }

        string safeKey = ToSafeFile(cacheKey) + ".mp3";
        string path = Path.Combine(CacheRoot, safeKey);

        if (File.Exists(path))
        {
            if (play) yield return PlayFromFile(path);
            yield break;
        }

        string ttsUrl = config.baseUrl.TrimEnd('/') + "/audio/speech";
        var bodyObj = new TtsRequest
        {
            model = string.IsNullOrEmpty(config.ttsModel) ? "gpt-4o-mini-tts" : config.ttsModel,
            voice = voice,
            input = text,
            format = "mp3"
        };
        string json = JsonUtility.ToJson(bodyObj);

        using (var req = new UnityWebRequest(ttsUrl, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Authorization", "Bearer " + config.apiKey);
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[AIVoiceSpeakerV1] TTS HTTP {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            var data = req.downloadHandler.data;
            if (data == null || data.Length == 0) yield break;

            File.WriteAllBytes(path, data);
        }

        if (play) yield return PlayFromFile(path);
    }

    private IEnumerator PlayFromFile(string path)
    {
        using (var uwr = UnityWebRequestMultimedia.GetAudioClip("file://" + path, AudioType.MPEG))
        {
            yield return uwr.SendWebRequest();
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("[AIVoiceSpeakerV1] PlayFromFile failed: " + uwr.error);
                yield break;
            }
            var clip = DownloadHandlerAudioClip.GetContent(uwr);
            if (audioSource && clip)
            {
                audioSource.Stop();
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }

    private static string ToSafeFile(string s)
    {
        if (string.IsNullOrEmpty(s)) return "tts";
        foreach (var c in Path.GetInvalidFileNameChars()) s = s.Replace(c, '_');
        return s.Length > 64 ? s.Substring(0, 64) : s;
    }
}
