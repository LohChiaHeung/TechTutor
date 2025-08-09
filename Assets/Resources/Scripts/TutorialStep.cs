[System.Serializable]
public class TutorialStep
{
    public string title;
    public string description;
}

[System.Serializable]
public class TutorialList
{
    public TutorialStep[] steps;
}
