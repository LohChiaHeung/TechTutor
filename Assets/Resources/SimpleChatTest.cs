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
    public string openRouterApiKey = "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"; 

    public void OnSendClicked()
    {
        if (!string.IsNullOrWhiteSpace(userInput.text))
        {
            StartCoroutine(SendMessageToOpenRouter(userInput.text));
            userInput.text = "";
            chatScrollRect.verticalNormalizedPosition = 0f;
        }
        else
        {
            responseText.text = "Please enter a question.";
        }
    }

    IEnumerator SendMessageToOpenRouter(string userMessage)
    {
        string escapedMessage = userMessage.Replace("\"", "\\\"");

        string url = "https://openrouter.ai/api/v1/chat/completions";
        string json = @"{
        ""model"": ""mistralai/mistral-7b-instruct"",
        ""messages"": [
            {""role"": ""system"", ""content"": ""You are a friendly tech tutor who helps beginners learn how to use a computer.""},
            {""role"": ""user"", ""content"": """ + escapedMessage + @"""}
        ]
    }";

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();

        request.SetRequestHeader("X-Title", "TechTutorChatApp");
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openRouterApiKey);
        request.SetRequestHeader("HTTP-Referer", "https://techtutor.ai");

        // ✅ Append user's question and "Thinking..." (no typing yet)
        responseText.text += $"\n\nYou: {userMessage}\n\nTechTutor: Thinking...";

        Canvas.ForceUpdateCanvases();
        chatScrollRect.verticalNormalizedPosition = 1f;

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            string reply = ExtractReply(jsonResponse);

            // ✅ Replace only the "Thinking..." part with the animated reply
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
            int start = json.IndexOf("\"content\":\"") + 11;
            int end = json.IndexOf("\"", start);

            if (start > 10 && end > start)
            {
                string content = json.Substring(start, end - start);
                return content.Replace("\\n", "\n").Replace("\\\"", "\"");
            }
        }
        catch { }

        return "(Could not parse reply)";
    }

    void Update()
    {
        if (userInput.isFocused && Application.isMobilePlatform)
        {
            Canvas.ForceUpdateCanvases();
            inputScroll.verticalNormalizedPosition = 0f;
        }
    }
}
