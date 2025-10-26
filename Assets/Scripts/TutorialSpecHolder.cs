using UnityEngine;
using static TechTutorAskUI;

public class TutorialSpecHolder : MonoBehaviour
{
    public static TutorialSpecHolder I;

    public TutorialSpec spec; // The parsed tutorial

    void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }
        I = this;
        DontDestroyOnLoad(gameObject);
    }
}
