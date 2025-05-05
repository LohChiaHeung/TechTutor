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

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void LoadMarkerBasedTutorial()
    {
        SceneManager.LoadScene("MarkerBasedTutorial");
    }

    public void LoadFAQScene()
    {
        SceneManager.LoadScene("FAQScene");
    }

    public void LoadChatSupportMainScene()
    {
        SceneManager.LoadScene("SupportScene");
    }

    public void LoadMainScene()
    {
        SceneManager.LoadScene("Main_Scene");
    }

    public void LoadDevelopmentScene()
    {
        SceneManager.LoadScene("DevelopmentPhaseScene");
    }

    public void LoadARGuideMainScene()
    {
        SceneManager.LoadScene("ARGuide_Scene");
    }

    public void LoadARLearnMainScene()
    {
        SceneManager.LoadScene("ARLearn_Scene");
    }

    public void LoadAIChatBotMainScene()
    {
        SceneManager.LoadScene("AIChatbot_Scene");
    }

    public void LoadVirtualAssemblyScene()
    {
        SceneManager.LoadScene("VirtualAssemblyScene");
    }

    public void LoadStepTestScene()
    {
        SceneManager.LoadScene("SnapTestScene");
    }
}


