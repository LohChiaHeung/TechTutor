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
        SceneManager.LoadScene("SampleScene");
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
        SceneManager.LoadScene("SimpleChatTest");

    }
    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}


