using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GPTConnector : MonoBehaviour
{
    public string openAI_API_Key = "sk-..."; // 🔑 Replace with your OpenAI API Key

    public IEnumerator SendPrompt(string prompt, System.Action<string> onResponse)
    {
        string apiUrl = "https://api.openai.com/v1/chat/completions";

        string jsonBody = "{ \"model\": \"gpt-4o\", \"messages\": [{\"role\": \"user\", \"content\": \"" +
                          prompt.Replace("\"", "\\\"") + "\"}], \"temperature\": 0.7 }";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAI_API_Key);

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("OpenAI API Error: " + request.error);
        }
        else
        {
            string json = request.downloadHandler.text;
            string content = ExtractResponse(json);
            SaveResponseToFile(content);
            onResponse?.Invoke(content);

        }
    }

    [System.Serializable] public class OpenAIWrapper { public OpenAIResponse wrapper; }
    [System.Serializable] public class OpenAIResponse { public Choice[] choices; }
    [System.Serializable] public class Choice { public AIMessage message; }
    [System.Serializable] public class AIMessage { public string content; }

    private string ExtractResponse(string json)
    {
        try
        {
            // Wrap the actual response with a top-level object for Unity parsing
            string wrapped = "{\"wrapper\":" + json + "}";
            OpenAIWrapper result = JsonUtility.FromJson<OpenAIWrapper>(wrapped);
            return result.wrapper.choices[0].message.content;
        }
        catch
        {
            Debug.LogError("❌ Failed to parse GPT content.");
            return "";
        }
    }


    private void SaveResponseToFile(string content)
    {
        string path = Application.persistentDataPath + "/GPT_TutorialOutput.txt";
        try
        {
            System.IO.File.WriteAllText(path, content);
            Debug.Log($"✅ GPT response saved to: {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ Failed to save GPT response: " + ex.Message);
        }
    }

}
