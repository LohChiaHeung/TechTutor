//using System.Collections;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;
//using TMPro;

//public class OpenAIImageDescriber : MonoBehaviour
//{

//    //public GameObject objectPrefab;          // Assign your MysteryObject prefab
//    //public Transform spawnAnchor;            // Where to place the object
//    private GameObject currentInstance;      // Keep reference for reuse
//    //public GameObject labelOnlyPrefab;  // <- your ARLabelCanvas prefab
//    //public GameObject guideModelPrefab; // e.g., robot prefab
//    //private GameObject guideInstance;   // to destroy it later
//    private Vector3 lockedSpawnPosition;
//    public GameObject guideBundlePrefab; // This prefab contains both the robot model + label canvas


//    [Header("UI References")]
//    public Button captureButton;
//    public TextMeshProUGUI resultText;

//    [Header("OpenAI API")]
//    [TextArea]
//    public string openAIApiKey = "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"; // Replace with your real GPT-4o key

//    private void Start()
//    {
//        //captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSendToOpenAI()));
//        captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSendToOpenAI()));
//        // ✅ Start a test coroutine
//        StartCoroutine(TestSpawnLabel());
//    }

//    IEnumerator TestSpawnLabel()
//    {
//        yield return new WaitForSeconds(1f); // optional delay to test spawn after app starts

//        string testResponse = "This is a keyboard.";
//        yield return StartCoroutine(SpawnLabelAndModel(testResponse));
//    }

//    IEnumerator CaptureAndSendToOpenAI()
//    {
//        yield return new WaitForEndOfFrame();

//        // ✅ Step 1: Capture camera screen
//        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
//        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
//        tex.Apply();

//        byte[] imageBytes = tex.EncodeToJPG();
//        string base64Image = System.Convert.ToBase64String(imageBytes);
//        Destroy(tex);

//        // Lock the spawn position NOW (not later)
//        lockedSpawnPosition = GetSmartSpawnPosition();

//        // ✅ Step 2: Send to OpenAI
//        //yield return StartCoroutine(SendToOpenAI(base64Image));
//        string simulatedReply = "This is a keyboard."; // You can change this to test different labels
//        yield return StartCoroutine(SpawnLabelAndModel(simulatedReply));
//    }

//    // ✅ DYNAMIC POSITION BASED ON CAMERA ANGLE
//    //private Vector3 GetSmartSpawnPosition()
//    //{
//    //    Vector3 forwardOffset = Camera.main.transform.forward * 0.5f;
//    //    Vector3 heightOffset = Vector3.up * 0.15f; // default upward boost

//    //    float cameraAngle = Vector3.Angle(Camera.main.transform.forward, Vector3.down);

//    //    if (cameraAngle > 30f && cameraAngle < 80f)
//    //    {
//    //        heightOffset = Vector3.up * 0.1f; // 45° downward: boost higher
//    //    }
//    //    else if (cameraAngle <= 30f)
//    //    {
//    //        //forwardOffset += Camera.main.transform.up * -0.1f; // flat: slide back
//    //        forwardOffset = forward * 0.45f;
//    //        upwardOffset = Vector3.up * 0.05f; // 🔽 Lower than before
//    //    }

//    //    return Camera.main.transform.position + forwardOffset + heightOffset;
//    //}

//    private Vector3 GetSmartSpawnPosition()
//    {
//        Vector3 forward = Camera.main.transform.forward;
//        float tiltAngle = Vector3.Angle(forward, Vector3.down);

//        Vector3 forwardOffset;
//        Vector3 upwardOffset;

//        if (tiltAngle > 30f && tiltAngle < 80f)
//        {
//            // Angled downward (e.g. for mouse/keyboard)
//            forwardOffset = forward;//* 0.3f;
//            upwardOffset = Vector3.up * 0.01f;
//        }
//        else
//        {
//            // Flat/parallel view (e.g. laptop screen)
//            forwardOffset = forward * 0.45f;
//            upwardOffset = Vector3.up * -0.1f; // 🔽 Lower than before
//        }

//        return Camera.main.transform.position + forwardOffset + upwardOffset;
//    }



//    IEnumerator SendToOpenAI(string base64Image)
//    {
//        // ✅ Step 3: Construct GPT-4o JSON request
//        string prompt = "What object is shown in this image? Just answer: 'This is a ____.'";

//        string json = $@"
//        {{
//            ""model"": ""gpt-4o"",
//            ""messages"": [
//                {{
//                    ""role"": ""user"",
//                    ""content"": [
//                        {{
//                            ""type"": ""text"",
//                            ""text"": ""{prompt}""
//                        }},
//                        {{
//                            ""type"": ""image_url"",
//                            ""image_url"": {{
//                                ""url"": ""data:image/jpeg;base64,{base64Image}""
//                            }}
//                        }}
//                    ]
//                }}
//            ]
//        }}";

//        // ✅ Step 4: UnityWebRequest setup
//        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
//        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
//        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
//        request.downloadHandler = new DownloadHandlerBuffer();
//        request.SetRequestHeader("Content-Type", "application/json");
//        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

//        // ✅ Step 5: Send the request
//        yield return request.SendWebRequest();

//        // ✅ Step 6: Handle response
//        if (request.result == UnityWebRequest.Result.Success)
//        {
//            string reply = ExtractOpenAIResponse(request.downloadHandler.text);
//            Debug.Log("✅ GPT-4o replied: " + reply);
//            // Step 1: Update spawn position
//            Vector3 spawnPos = lockedSpawnPosition;

//            if (currentInstance != null)
//            {
//                Destroy(currentInstance);
//                Debug.Log("🗑️ Destroyed previous label instance");
//            }

//            // Destroy old guide model
//            //if (guideInstance != null)
//            //{
//            //    Destroy(guideInstance);
//            //    Debug.Log("🗑️ Destroyed previous guide model");
//            //}

//            // Calculate position slightly beside the label (e.g., to the right)
//            Vector3 guidePos = spawnPos + Camera.main.transform.right * 0.3f;

//            // Spawn the robot model
//            //guideInstance = Instantiate(guideModelPrefab, guidePos, Quaternion.Euler(270, 0, 180));
//            //guideInstance.transform.localScale = Vector3.one * 0.1f;
//            //guideInstance.transform.LookAt(Camera.main.transform);


//            currentInstance = Instantiate(guideBundlePrefab, lockedSpawnPosition, Quaternion.identity);
//            currentInstance.transform.localScale = Vector3.one * 0.005f;
//            currentInstance.transform.LookAt(Camera.main.transform); // optional, for facing user
//            Debug.Log("🤖 Spawned guide bundle at: " + lockedSpawnPosition);

//            // Update label inside the bundle
//            TextMeshProUGUI label = currentInstance.GetComponentInChildren<TextMeshProUGUI>(true);
//            if (label != null)
//            {
//                string cleanReply = reply.Replace("This is", "").Trim().TrimEnd('.');
//                label.text = "This is " + cleanReply + ".";
//                Debug.Log("🏷️ Updated floating label: " + label.text);
//            }
//            else
//            {
//                Debug.LogWarning("⚠️ TextMeshPro label not found inside prefab!");
//            }


//            // Step 2: Spawn just the label canvas
//            //currentInstance = Instantiate(labelOnlyPrefab, spawnPos, Quaternion.identity);
//            //currentInstance.transform.localScale = Vector3.one * 0.005f;
//            //Debug.Log("🧾 Spawned label canvas at: " + spawnPos);

//            // Step 3: Update the label text
//            //TextMeshProUGUI label = currentInstance.GetComponentInChildren<TextMeshProUGUI>(true);
//            //if (label != null)
//            //{
//            //    string cleanReply = reply.Replace("This is", "").Trim().TrimEnd('.');
//            //    label.text = "This is " + cleanReply + ".";
//            //    Debug.Log("🏷️ Updated floating label: " + label.text);
//            //}
//            //else
//            //{
//            //    Debug.LogWarning("⚠️ TextMeshPro label not found inside prefab!");
//            //}

//        }

//        // ✅ Simple JSON parser for GPT-4o reply
//        string ExtractOpenAIResponse(string json)
//        {
//            try
//            {
//                var wrapper = JsonUtility.FromJson<OpenAIWrapper>("{\"wrapper\":" + json + "}");
//                return wrapper.wrapper.choices[0].message.content;
//            }
//            catch
//            {
//                return "(⚠️ Could not parse GPT-4o reply)";
//            }
//        }
//    }

//    IEnumerator SpawnLabelAndModel(string reply)
//    {
//        Vector3 spawnPos = lockedSpawnPosition;

//        if (currentInstance != null)
//        {
//            Destroy(currentInstance);
//            Debug.Log("🗑️ Destroyed previous label instance");
//        }

//        currentInstance = Instantiate(guideBundlePrefab, spawnPos, Quaternion.identity);
//        currentInstance.transform.localScale = Vector3.one * 0.005f;
//        //currentInstance.transform.LookAt(Camera.main.transform);
//        //currentInstance.transform.Rotate(0, 180f, 0); // 🔁 Flip around Y axis

//        //Debug.Log("🤖 Spawned guide bundle at: " + spawnPos);

//        //TextMeshProUGUI label = currentInstance.GetComponentInChildren<TextMeshProUGUI>(true);
//        //if (label != null)
//        //{
//        //    string cleanReply = reply.Replace("This is", "").Trim().TrimEnd('.');
//        //    label.text = "This is " + cleanReply + ".";
//        //    Debug.Log("🏷️ Updated label text: " + label.text);
//        //}
//        //else
//        //{
//        //    Debug.LogWarning("⚠️ TextMeshPro label not found inside prefab!");
//        //}

//        //yield return null;
//        // ✅ Rotate towards camera (Y axis only for stability)
//        Vector3 lookDirection = currentInstance.transform.position - Camera.main.transform.position;
//        lookDirection.y = 0f; // prevent tilt
//        currentInstance.transform.rotation = Quaternion.LookRotation(lookDirection);

//        // ✅ Update label
//        TextMeshProUGUI label = currentInstance.GetComponentInChildren<TextMeshProUGUI>(true);
//        if (label != null)
//        {
//            string cleanReply = reply.Replace("This is", "").Trim().TrimEnd('.');
//            label.text = "This is " + cleanReply + ".";
//        }

//        yield return null;
//    }


//    [System.Serializable]
//    public class OpenAIWrapper
//    {
//        public OpenAIResponse wrapper;
//    }

//    [System.Serializable]
//    public class OpenAIResponse
//    {
//        public Choice[] choices;
//    }

//    [System.Serializable]
//    public class Choice
//    {
//        public Message message;
//    }

//    [System.Serializable]
//    public class Message
//    {
//        public string content;
//    }
//}

//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class OpenAIImageDescriber : MonoBehaviour
//{
//    [Header("UI References")]
//    public Button captureButton;
//    public TextMeshProUGUI guideText; // The overlay label in bottom-left

//    private void Start()
//    {
//        captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSimulate()));
//    }

//    IEnumerator CaptureAndSimulate()
//    {
//        yield return new WaitForEndOfFrame();

//        // Step 1: Capture image (simulate, no OpenAI)
//        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
//        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
//        tex.Apply();

//        // You can use this later if needed:
//        // byte[] imageBytes = tex.EncodeToJPG();
//        // string base64Image = System.Convert.ToBase64String(imageBytes);
//        Destroy(tex);

//        // Step 2: Simulate result (replace with real API call later if needed)
//        string simulatedReply = "This is a keyboard.";

//        // Step 3: Show result in overlay text
//        guideText.text = simulatedReply;
//    }
//}


//using System.Collections;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class OpenAIImageDescriber : MonoBehaviour
//{
//    [Header("UI References")]
//    public Button captureButton;
//    public TextMeshProUGUI guideText;

//    public Button moreInfoButton;
//    public GameObject infoPanel;
//    public TextMeshProUGUI infoTitle;
//    public TextMeshProUGUI infoContent;
//    public Button closeButton;

//    private string lastDetectedObject = "";

//    private void Start()
//    {
//        captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSimulate()));
//        moreInfoButton.onClick.AddListener(ShowInfoPanel);
//        closeButton.onClick.AddListener(() => infoPanel.SetActive(false));

//        infoPanel.SetActive(false); // Make sure it's hidden on start
//    }

//    IEnumerator CaptureAndSimulate()
//    {
//        yield return new WaitForEndOfFrame();

//        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
//        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
//        tex.Apply();
//        Destroy(tex);

//        string simulatedReply = "This is a keyboard.";
//        guideText.text = simulatedReply;

//        // Save keyword for info
//        lastDetectedObject = "keyboard"; // <- Extract or simulate keyword only
//    }

//    void ShowInfoPanel()
//    {
//        infoPanel.SetActive(true);

//        switch (lastDetectedObject.ToLower())
//        {
//            case "keyboard":
//                infoTitle.text = "Keyboard";
//                infoContent.text = "A keyboard is an input device used to type and control a computer.";
//                break;
//            case "mouse":
//                infoTitle.text = "Mouse";
//                infoContent.text = "A mouse is a pointing device that lets you interact with a computer.";
//                break;
//            case "laptop":
//                infoTitle.text = "Laptop";
//                infoContent.text = "A laptop is a portable personal computer with a screen and keyboard.";
//                break;
//            default:
//                infoTitle.text = "Unknown";
//                infoContent.text = "No information available for this object.";
//                break;
//        }
//    }
//}

using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class OpenAIImageDescriber : MonoBehaviour
{
    [Header("UI References")]
    public Button captureButton;
    public TextMeshProUGUI guideText;

    public Button moreInfoButton;
    public GameObject infoPanel;
    public TextMeshProUGUI infoTitle;
    public TextMeshProUGUI infoContent;
    public Button closeButton;

    [Header("OpenAI API")]
    [TextArea]
    public string openAIApiKey = ""; // Your GPT-4o API key

    private string lastFullReply = "";

    private void Start()
    {
        captureButton.onClick.AddListener(() => StartCoroutine(CaptureAndSendToOpenAI()));
        moreInfoButton.onClick.AddListener(ShowInfoPanel);
        closeButton.onClick.AddListener(() => infoPanel.SetActive(false));
        infoPanel.SetActive(false);
    }

    IEnumerator CaptureAndSendToOpenAI()
    {
        yield return new WaitForEndOfFrame();

        // ✅ Capture screen
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        tex.Apply();
        byte[] imageBytes = tex.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageBytes);
        Destroy(tex);

        // ✅ Manually construct JSON
        //string prompt =
        //    "What object is shown in this image?\n" +
        //    "Reply in this format:\n" +
        //    "This is a ____.\n" +
        //    "[TITLE]: ____\n" +
        //    "[INFO]: ____";

        //string prompt = "What object is shown in this image? Just answer: 'This is a ____.'";

        //    string prompt =
        //"What object is shown in this image?\\n" +
        //"Reply in this format:\\n" +
        //"This is a ____." + "\\n" +
        //"[TITLE]: ____" + "\\n" +
        //"[INFO]: ____";

        string prompt =
    "You are helping to build an AR educational tutorial titled 'Identify Computer Components'.\\n" +
    "Your job is to describe what computer component is shown in the image.\\n" +
    "Examples of components include: mouse, keyboard, laptop, monitor, speaker, etc.\\n" +
    "Always reply in this exact format:\\n" +
    "This is a _____.\\n" +
    "[TITLE]: _____\\n" +
    "[INFO]: _____\\n" +
    "Replace the blanks with the name of the component.\\n" +
    "Make sure the INFO is written in a beginner-friendly way using 1 to 2 short paragraphs.\\n" +
    "Do not describe the image (e.g., 'This image shows...'). Instead, provide educational content about the component.\\n" +
    "The TITLE must be the same as the component name."
;

        string json =
            "{\"model\": \"gpt-4o\", " +
            "\"messages\": [{" +
                "\"role\": \"user\", " +
                "\"content\": [" +
                    "{\"type\": \"text\", \"text\": \"" + prompt + "\"}," +
                    "{\"type\": \"image_url\", \"image_url\": {\"url\": \"data:image/jpeg;base64," + base64Image + "\"}}" +
                "]" +
            "}]}";

        // ✅ Send request
        UnityWebRequest request = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + openAIApiKey);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string responseText = request.downloadHandler.text;
            string reply = ExtractOpenAIResponse(responseText);
            lastFullReply = reply;
            guideText.text = ExtractSummaryOnly(reply);
            Debug.Log("✅ GPT-4o reply:\n" + reply);
        }
        else
        {
            guideText.text = "❌ Failed to get response.";
            Debug.LogError("OpenAI API Error: " + request.error + "\n" + request.downloadHandler.text);
        }
    }

    void ShowInfoPanel()
    {
        infoPanel.SetActive(true);
        ExtractTitleAndInfo(lastFullReply, out string title, out string content);
        infoTitle.text = title;
        infoContent.text = content;
    }

    string ExtractOpenAIResponse(string json)
    {
        try
        {
            var wrapper = JsonUtility.FromJson<OpenAIWrapper>("{\"wrapper\":" + json + "}");
            return wrapper.wrapper.choices[0].message.content;
        }
        catch
        {
            return "(⚠️ Could not parse GPT-4o reply)";
        }
    }

    string ExtractSummaryOnly(string fullReply)
    {
        if (string.IsNullOrEmpty(fullReply)) return "No object detected.";
        string[] lines = fullReply.Split('\n');
        return lines.Length > 0 ? lines[0] : fullReply;
    }

    void ExtractTitleAndInfo(string reply, out string title, out string content)
    {
        title = "Unknown";
        content = "No information available.";
        if (string.IsNullOrEmpty(reply)) return;

        string[] lines = reply.Split('\n');
        foreach (string line in lines)
        {
            if (line.StartsWith("[TITLE]:"))
                title = line.Replace("[TITLE]:", "").Trim();
            if (line.StartsWith("[INFO]:"))
                content = line.Replace("[INFO]:", "").Trim();
        }
    }

    // ✅ Response classes only
    [System.Serializable] public class OpenAIWrapper { public OpenAIResponse wrapper; }
    [System.Serializable] public class OpenAIResponse { public Choice[] choices; }
    [System.Serializable] public class Choice { public AIMessage message; }
    [System.Serializable] public class AIMessage { public string content; }
}

