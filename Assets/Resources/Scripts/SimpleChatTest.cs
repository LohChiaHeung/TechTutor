using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class SimpleChatTest : MonoBehaviour
{
    public TMP_InputField userInput;       // Input field for user question
    public TMP_Text responseText;          // Text field to show AI response
    public ScrollRect chatScrollRect;      // ScrollRect to auto-scroll
    public ScrollRect inputScroll;
    //public string openRouterApiKey = "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"; 
    public string openAIKey = "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";


    public void OnSendClicked()
    {
        if (!string.IsNullOrWhiteSpace(userInput.text))
        {
            string userMessage = userInput.text;
            userInput.text = "";
            StartCoroutine(SendMessageToOpenAI(userMessage));
        }
        else
        {
            responseText.text = "Please enter a question.";
        }
    }

    IEnumerator SendMessageToOpenAI(string userMessage)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        string json = @"{
            ""model"": ""gpt-3.5-turbo"",
            ""messages"": [
                {""role"": ""system"", ""content"": ""You are a friendly and helpful tech tutor who helps beginners learn how to use a computer.""},
                {""role"": ""user"", ""content"": """ + userMessage.Replace("\"", "\\\"") + @"""}
            ]
        }";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIKey);

        // Show user input + thinking...
        responseText.text += $"\n\nYou: {userMessage}\n\nTechTutor: Thinking...";

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 1f;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            string reply = ExtractReply(jsonResponse);

            string previousText = responseText.text.Replace("TechTutor: Thinking...", "TechTutor: ");
            StartCoroutine(TypeText(previousText, reply));
        }
        else
        {
            responseText.text = responseText.text.Replace("TechTutor: Thinking...", "TechTutor: Error: " + request.error);
        }

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 0f;
    }


    IEnumerator TypeText(string baseText, string newText, float delay = 0.02f)
    {
        string current = baseText;

        for (int i = 0; i < newText.Length; i++)
        {
            current += newText[i];
            responseText.text = current;

            Canvas.ForceUpdateCanvases();
            chatScrollRect.verticalNormalizedPosition = 0f;

            yield return new WaitForSeconds(delay);
        }
    }


    string ExtractReply(string json)
    {
        try
        {
            OpenAIResponse parsed = JsonUtility.FromJson<OpenAIWrapper>("{\"wrapper\":" + json + "}").wrapper;
            return parsed.choices[0].message.content;
        }
        catch
        {
            return "(Could not parse reply)";
        }
    }

    void Update()
    {
        if (userInput.isFocused && Application.isMobilePlatform)
        {
            Canvas.ForceUpdateCanvases();
            inputScroll.verticalNormalizedPosition = 0f;
        }
    }

    [System.Serializable]
    public class OpenAIWrapper
    {
        public OpenAIResponse wrapper;
    }

    [System.Serializable]
    public class OpenAIResponse
    {
        public Choice[] choices;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }


    public static class JsonHelper
    {
        public static string GetJsonArray(string json, string arrayName)
        {
            int start = json.IndexOf($"\"{arrayName}\":[");
            if (start == -1) return "[]";

            int arrayStart = json.IndexOf('[', start);
            int arrayEnd = json.IndexOf("]", arrayStart);

            return json.Substring(arrayStart, arrayEnd - arrayStart + 1);
        }
    }

}
