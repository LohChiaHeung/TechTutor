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
            case "AR_MicrosoftWordTutorial":
            case "AR_GmailTutorial":
            case "AR_SnippingToolsTutorial":
            case "AR_AI_Tutorial":
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
            case "AR_AllTutorial":
            case "AR_IdentifyComputerComponents":
            case "FlipBookTest":
            case "3DBooksModel":
            case "AR_ImageTest":
            case "AR_PanelTutorial":
            case "Test_CompComponents":
            case "AR_DeskSimulation":
            case "AR_DeskSimulation_MicrosoftWord":
            case "AR_DeskSimulation_Gmail":
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
