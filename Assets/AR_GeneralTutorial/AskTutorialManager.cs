//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System.Collections;
//using System.Collections.Generic;

//public class AskTutorialManager : MonoBehaviour
//{
//    [Header("UI Elements")]
//    public TMP_InputField inputField;
//    public Button askButton;
//    public Button arModeButton;
//    public Transform conversationContent;
//    public GameObject userMessagePrefab;
//    public GameObject botReplyPrefab;

//    [Header("Prompt Settings")]
//    [TextArea(3, 10)]
//    public string promptTemplate;

//    private string lastBotReply = "";

//    private void Start()
//    {
//        askButton.onClick.AddListener(OnAskClicked);
//        arModeButton.onClick.AddListener(OnARModeClicked);
//        arModeButton.gameObject.SetActive(false);
//    }

//    void OnAskClicked()
//    {
//        string userQuestion = inputField.text.Trim();
//        if (string.IsNullOrEmpty(userQuestion)) return;

//        inputField.text = "";
//        DisplayUserMessage(userQuestion);
//        StartCoroutine(SendToOpenAI(userQuestion));
//    }

//    IEnumerator SendToOpenAI(string prompt)
//    {
//        yield return StartCoroutine(OpenAIAgent.SendPrompt(prompt, (reply) =>
//        {
//            lastBotReply = reply;

//            DisplayMessage(reply, false);

//            if (reply.Contains("Step 1") && reply.Contains("Step 2"))
//            {
//                arModeButton.gameObject.SetActive(true);
//            }

//            SaveConversation(prompt, reply);
//        }));
//    }


//    void OnARModeClicked()
//    {
//        // Later: parse `lastBotReply` into TutorialSpec, then switch to AR
//        Debug.Log("AR Mode triggered!");
//    }

//    void DisplayUserMessage(string message)
//    {
//        var go = Instantiate(userMessagePrefab, conversationParent);
//        go.GetComponentInChildren<TMP_Text>().text = message;
//    }

//    void DisplayBotReply(string message)
//    {
//        var go = Instantiate(botMessagePrefab, conversationParent);
//        go.GetComponentInChildren<TMP_Text>().text = message;
//    }


//    void SaveConversation(string user, string bot)
//    {
//        // Save to PlayerPrefs, JSON file, or local DB (Room/Firebase)
//        Debug.Log("Saved conversation: " + user + " → " + bot);
//    }
//}
