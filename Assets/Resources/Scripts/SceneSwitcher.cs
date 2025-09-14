using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{

    void KillPersistentEventSystems()
    {
        // Destroy EventSystems that live in DontDestroyOnLoad scene
        foreach (var es in FindObjectsOfType<EventSystem>(true))
            if (es.gameObject.scene.buildIndex == -1) // -1 = DontDestroyOnLoad scene
                Destroy(es.gameObject);
    }

    //public void LoadEmailTutorial()
    //{
    //    SceneManager.LoadScene("SendEmailScene");
    //}

    //public void LoadComputerKeyboardComponents()
    //{
    //    SceneManager.LoadScene("ComputerComponent");
    //}

    public void LoadCalculatorTutorial()
    {
        SceneManager.LoadScene("CalculatorTutorial");
    }

    //public void LoadMicrosoftWordTutorial()
    //{
    //    SceneManager.LoadScene("MicrosoftWordTutorial");
    //}

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

    //public void LoadARGmailTutorial()
    //{
    //    SceneManager.LoadScene("AR_GmailTutorial", LoadSceneMode.Single);
    //}

    //public void LoadARSnippingTools()
    //{
    //    SceneManager.LoadScene("AR_SnippingToolsTutorial", LoadSceneMode.Single);
    //}

    //public void LoadARMicrosoftWordTutorial()
    //{
    //    //SceneManager.LoadScene("AR_MicrosoftWordTutorial", LoadSceneMode.Single);
    //    SceneManager.LoadScene("AR_PortraitMode_MsWord", LoadSceneMode.Single);
    //}

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
        if (StepState.I) StepState.I.CurrentStep = 0;
        KillPersistentEventSystems();
        KillAllDontDestroyOnLoad();
        SceneManager.LoadScene("AR_DeskSimulation", LoadSceneMode.Single);
    }
    public void AR_MicrosoftWord()
    {
        if (StepState.I) StepState.I.CurrentStep = 0;
        KillPersistentEventSystems();
        KillAllDontDestroyOnLoad();
        //SceneManager.LoadScene("AR_DeskSimulation_MicrosoftWord", LoadSceneMode.Single);
        SceneManager.LoadScene("AR_PortraitMode_MsWord", LoadSceneMode.Single);
    }
    public void AR_Gmail()
    {
        if (StepState.I) StepState.I.CurrentStep = 0;
        KillPersistentEventSystems();
        KillAllDontDestroyOnLoad();
        SceneManager.LoadScene("AR_DeskSimulation_Gmail", LoadSceneMode.Single);
    }

    public void AR_Quiz()
    {
        SceneManager.LoadScene("AR_QRQuiz", LoadSceneMode.Single);
    }

    public void LoadOCRPlease()
    {
        SceneManager.LoadScene("TT2_OcrDemo", LoadSceneMode.Single);
    }

    void KillAllDontDestroyOnLoad()
    {
        var temp = new GameObject("TempDDOL");
        DontDestroyOnLoad(temp);
        var ddolScene = temp.scene;

        foreach (var root in ddolScene.GetRootGameObjects())
        {
            if (root != temp) Destroy(root);
        }

        Destroy(temp);
    }

}


