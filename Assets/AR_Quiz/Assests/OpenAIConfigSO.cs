using UnityEngine;

[CreateAssetMenu(fileName = "OpenAIConfig", menuName = "TechTutor/OpenAI Config")]
public class OpenAIConfigSO : ScriptableObject
{
    [Header("API")]
    public string apiKey;
    public string baseUrl = "https://api.openai.com/v1";

    [Header("Models")]
    public string chatModel = "gpt-4o-mini";      // quiz text gen
    public string ttsModel = "gpt-4o-mini-tts";  // reserved for Step 2
}
