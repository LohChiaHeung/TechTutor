using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class RoboflowDetector : MonoBehaviour
{
    public string apiKey = "9O7dyant7zW83XhlPgdD";
    public string modelName = "keyboard-detection-v2";
    public int modelVersion = 1;

    private Texture2D screenTexture;

    void Start()
    {
        // Repeat detection every 5 seconds (adjust as needed)
        InvokeRepeating(nameof(CaptureAndSendToRoboflow), 2f, 5f);
    }

    void CaptureAndSendToRoboflow()
    {
        StartCoroutine(CaptureScreenAndDetect());
    }

    IEnumerator CaptureScreenAndDetect()
    {
        yield return new WaitForEndOfFrame();

        screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        byte[] imageBytes = screenTexture.EncodeToJPG();
        string base64Image = System.Convert.ToBase64String(imageBytes);

        string url = $"https://detect.roboflow.com/{modelName}/{modelVersion}?api_key={apiKey}";

        WWWForm form = new WWWForm();
        form.AddField("base64", base64Image);

        UnityWebRequest www = UnityWebRequest.Post(url, form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + www.downloadHandler.text);
            // TODO: Parse response and display results
        }
        else
        {
            Debug.LogError("Error: " + www.error);
        }
    }
}
