using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;

public class OpenRouterChat : MonoBehaviour
{
    public TMP_InputField inputField;
    public TMP_Text outputText;
    public string openRouterApiKey = "sk-or-v1-a58a3a14467d3de14fc950cd0e1a3e4e55e800a6cde3835c17842a7dbf23570esk-or-v1-a58a3a14467d3de14fc950cd0e1a3e4e55e800a6cde3835c17842a7dbf23570e";

    public void OnSendClicked()
    {
        StartCoroutine(SendMessageToOpenRouter(inputField.text));
        Debug.Log("✅ Send Button Clicked!");
    }

    IEnumerator SendMessageToOpenRouter(string userMessage)
    {
        string url = "https://openrouter.ai/api/v1/chat/completions";

        string json = @"{
        ""model"": ""mistralai/mistral-7b-instruct"",
        ""messages"": [
        {""role"": ""system"", ""content"": ""You are a friendly tech tutor who helps beginners learn how to use a computer.""},
        {""role"": ""user"", ""content"": """ + userMessage + @"""}
       ]
    }";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openRouterApiKey);
        request.SetRequestHeader("HTTP-Referer", "https://techtutor.ai");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Response: " + request.downloadHandler.text);
            string responseText = ExtractContent(request.downloadHandler.text);
            outputText.text = responseText;
        }
        else
        {
            Debug.LogError("❌ Error: " + request.error);
            Debug.LogError("❌ Response: " + request.downloadHandler.text);
            outputText.text = "❌ Failed to get response.";
        }
    }

    // Simple JSON parser for the assistant's reply
    string ExtractContent(string json)
    {
        int start = json.IndexOf("\"content\":\"") + 10;
        int end = json.IndexOf("\"", start);
        return json.Substring(start, end - start).Replace("\\n", "\n");
    }
}
