using System;

[Serializable]
public class AIGuide
{
    public AIGuideStep[] steps;
}

[Serializable]
public class AIGuideStep
{
    public string title;
    public string instruction;
    public string[] keywords;
}
