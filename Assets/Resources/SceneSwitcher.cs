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
}


