using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    
    public void LoadEmailTutorial()
    {
        SceneManager.LoadScene("SendEmailScene");
    }

    public void LoadComputerKeyboardComponents()
    {
        SceneManager.LoadScene("ComputerComponent");
    }

    public void LoadCalculatorTutorial()
    {
        SceneManager.LoadScene("CalculatorTutorial");
    }

    public void LoadMicrosoftWordTutorial()
    {
        SceneManager.LoadScene("MicrosoftWordTutorial");
    }

    public void LoadChatBot()
    {
        SceneManager.LoadScene("ChatBotScene");

    }

    //public void LoadMainMenu()
    //{
    //    SceneManager.LoadScene("MainMenu");
    //}

    //public void LoadMarkerBasedTutorial()
    //{
    //    SceneManager.LoadScene("MarkerBasedTutorial");
    //}

    public void LoadFAQScene()
    {
        SceneManager.LoadScene("FAQScene");
    }

    public void LoadChatSupportMainScene()
    {
        SceneManager.LoadScene("SupportScene");
    }

    public void MainScene_MainMenu()
    {
        SceneManager.LoadScene("Main_Scene");
    }

    //public void LoadDevelopmentScene()
    //{
    //    SceneManager.LoadScene("DevelopmentPhaseScene");
    //}

    public void MainScene_ARGuide()
    {
        SceneManager.LoadScene("ARGuide_Scene");
    }

    public void MainScene_ARLearn()
    {
        SceneManager.LoadScene("ARLearn_Scene");
    }

    public void MainScene_AIChatbot()
    {
        SceneManager.LoadScene("AIChatbot_Scene");
    }

    //public void LoadVirtualAssemblyScene()
    //{
    //    SceneManager.LoadScene("VirtualAssemblyScene");
    //}

    //public void LoadStepTestScene()
    //{
    //    SceneManager.LoadScene("SnapTestScene");
    //}

    //public void LoadKeyboardDetection()
    //{
    //    SceneManager.LoadScene("Keyboard_Test");
    //}

    //public void LoadKeyboardDetections()
    //{
    //    //SceneManager.LoadScene("AR_Keyboard_Tutorial");

    //    SceneManager.LoadScene("OnlyKeyboard");
    //}

    public void LoadAR_AllTutorial()
    {
        //SceneManager.LoadScene("AR_Keyboard_Tutorial");
        SceneManager.LoadScene("AR_AllTutorial");
    }

    public void LoadARGmailTutorial()
    {
        SceneManager.LoadScene("AR_GmailTutorial", LoadSceneMode.Single);
    }

    public void LoadARSnippingTools()
    {
        SceneManager.LoadScene("AR_SnippingToolsTutorial", LoadSceneMode.Single);
    }

    public void LoadARMicrosoftWordTutorial()
    {
        SceneManager.LoadScene("AR_MicrosoftWordTutorial", LoadSceneMode.Single);
    }

    public void LoadAR_IdentifyComputerComponent()
    {
        GameObject obj = GameObject.Find("DontDestroyOnLoad");
        if (obj != null)
        {
            Destroy(obj);
            Debug.Log("✅ Destroyed DontDestroyOnLoad object before scene switch.");
        }

        SceneManager.LoadScene("AR_IdentifyComputerComponents", LoadSceneMode.Single);
    }


    //public void BookFlip()
    //{
    //    SceneManager.LoadScene("FlipBookTest", LoadSceneMode.Single);
    //}

    //public void Book3DModelFlip()
    //{
    //    SceneManager.LoadScene("3DBooksModel", LoadSceneMode.Single);
    //}

    public void ARTutorialOverlay()
    {
        SceneManager.LoadScene("AR_TutorialOverlay_Demo", LoadSceneMode.Single);
    }

    public void TextVoiceToSpeech()
    {
        SceneManager.LoadScene("TestVoiceToSpeech", LoadSceneMode.Single);
    }

    public void AR_AI_Tutorial()
    {
        SceneManager.LoadScene("AR_AI_Tutorial", LoadSceneMode.Single);
    }

    public void TestCompComponenets()
    {
        SceneManager.LoadScene("Test_CompComponents", LoadSceneMode.Single);
    }

    public void AR_SnippingTools()
    {
        SceneManager.LoadScene("AR_DeskSimulation", LoadSceneMode.Single);
    }
    public void AR_MicrosoftWord()
    {
        SceneManager.LoadScene("AR_DeskSimulation_MicrosoftWord", LoadSceneMode.Single);
    }
    public void AR_Gmail()
    {
        SceneManager.LoadScene("AR_DeskSimulation_Gmail", LoadSceneMode.Single);
    }

    public void AR_Quiz()
    {
        SceneManager.LoadScene("AR_QRQuiz", LoadSceneMode.Single);
    }


}


