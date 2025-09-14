using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class AIQuizGenerator_OpenAI : MonoBehaviour
{
    [Header("Config")]
    public OpenAIConfigSO config;

    public System.Action<string> OnError;
    public System.Action<QuizSessionMeta> OnQuizGeneratedAndSaved;
    public bool IsBusy { get; private set; }

    [Header("Context")]
    public string modelId = "keyboard";
    public string modelName = "Keyboard";

    [Header("Debug")]
    public bool logResponse = false;

    [System.Serializable] private class ChatMessage { public string role; public string content; }
    [System.Serializable] private class ResponseFormat { public string type; }
    [System.Serializable]
    private class ChatRequest
    {
        public string model;
        public ResponseFormat response_format;
        public ChatMessage[] messages;
        public float temperature;
        public int max_tokens;
    }
    [System.Serializable] private class ChatResp { public Choice[] choices; }
    [System.Serializable] private class Choice { public Msg message; }
    [System.Serializable] private class Msg { public string role; public string content; }

    public void GenerateAndSave() { StartCoroutine(Co_GenerateAndSave()); }

    IEnumerator Co_GenerateAndSave()
    {
        if (!config || string.IsNullOrEmpty(config.apiKey))
        {
            Debug.LogError("[AIQuizGenerator] Missing OpenAIConfigSO or API key.");
            OnError?.Invoke("Missing OpenAIConfig/api key");
            yield break;
        }
        IsBusy = true;
        try
        {
            string systemMsg = "You write strictly valid JSON for TechTutor quizzes. No extra text.";
            string userMsg =
$@"Write 5 beginner-friendly multiple-choice questions about {modelName} (computer basics).
Keep questions and options simple and clear (single sentence per option).
Use this exact JSON schema:

{{
  ""topic"": ""string"",
  ""items"": [
    {{
      ""type"": ""mcq"",
      ""question"": ""string"",
      ""options"": [""string"", ""string"", ""string"", ""string""],
      ""answer_index"": 0,
      ""explain"": ""string (optional)""
    }}
  ]
}}

Rules:
- Exactly 5 items.
- Each item.type = ""mcq"".
- options must have exactly 4 short, plain sentences.
- answer_index is 0..3 and correct option must be obvious to a beginner.
- Avoid brand trivia and OS/version-specific details.";

            if (modelId.ToLower().Contains("keyboard"))
                userMsg += "\n- Do NOT ask to locate keys on a physical layout (we handle that separately). Focus on general usage.";

            var reqObj = new ChatRequest
            {
                model = string.IsNullOrEmpty(config.chatModel) ? "gpt-4o-mini" : config.chatModel,
                response_format = new ResponseFormat { type = "json_object" },
                temperature = 0.2f,
                max_tokens = 900,
                messages = new ChatMessage[]
                {
                    new ChatMessage { role = "system", content = systemMsg },
                    new ChatMessage { role = "user",   content = userMsg }
                }
            };

            string url = config.baseUrl.TrimEnd('/') + "/chat/completions";
            string jsonBody = JsonUtility.ToJson(reqObj);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Authorization", "Bearer " + config.apiKey);
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

                if (req.result != UnityWebRequest.Result.Success)
                {
                    var msg = $"HTTP {req.responseCode} {req.error}\n{req.downloadHandler.text}";
                    Debug.LogError("[AIQuizGenerator] " + msg);
                    OnError?.Invoke(msg);
                    yield break;
                }

                var resp = JsonUtility.FromJson<ChatResp>(req.downloadHandler.text);
                string content = resp != null && resp.choices != null && resp.choices.Length > 0
                    ? resp.choices[0].message.content
                    : null;

                if (string.IsNullOrEmpty(content))
                {
                    Debug.LogError("[AIQuizGenerator] Empty content.");
                    OnError?.Invoke("Empty content");
                    yield break;
                }
                if (logResponse) Debug.Log("[AIQuizGenerator] JSON:\n" + content);

                QuizPayload payloadObj = JsonUtility.FromJson<QuizPayload>(content);
                if (payloadObj == null || payloadObj.items == null || payloadObj.items.Length == 0)
                {
                    Debug.LogError("[AIQuizGenerator] Could not parse QuizPayload.");
                    OnError?.Invoke("Parse error");
                    yield break;
                }

                var meta = new QuizSessionMeta
                {
                    sessionId = AIQuizLocalStore.MakeSessionId(modelId),
                    modelId = modelId,
                    modelName = modelName,
                    createdAtIsoUtc = System.DateTime.UtcNow.ToString("o"),
                    payload = payloadObj
                };
                AIQuizLocalStore.Save(meta);
                Debug.Log($"[AIQuizGenerator] Saved AI quiz for {modelId} / {modelName}");
                OnQuizGeneratedAndSaved?.Invoke(meta);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
