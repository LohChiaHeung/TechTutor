using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneOrientationManager : MonoBehaviour
{
    void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        Debug.Log("Setting orientation for: " + currentScene);

        switch (currentScene)
        {
            case "Main_Scene":
            case "ChatBotScene":
            case "MainMenu":
            case "FAQScene":
            case "SupportScene":
            case "DevelopmentPhaseScene":
            case "ARGuide_Scene":
            case "ARLearn_Scene":
            case "AIChatbot_Scene":
                SetPortraitOnly();
                break;

            case "SendEmailScene":
            case "CalculatorTutorial":
            case "MicrosoftWordTutorial":
                SetAutoRotate(); 
                break;

            case "MarkerBasedTutorial":
            case "ComputerComponent":
            case "VirtualAssemblyScene":
            case "Keyboard_Test":
            case "SnapshotARScene":
            case "OnlyKeyboard":
            case "AR_Keyboard_Tutorial":
            case "TestVoiceToSpeech":
                SetLandscapeOnly();
                break;

            default:
                SetAutoRotate();
                break;
        }

        // Prevent lingering effect
        Destroy(this);
    }

    void SetPortraitOnly()
    {
        Screen.orientation = ScreenOrientation.Portrait;
    }

    void SetLandscapeOnly()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
    }

    void SetAutoRotate()
    {
        Screen.orientation = ScreenOrientation.AutoRotation;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortraitUpsideDown = false;
    }
}
