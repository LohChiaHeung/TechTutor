using System;

[Serializable]
public class AIGuide
{
    public StepItem[] steps;
}

[Serializable]
public class StepItem
{
    public string title;
    public string instruction;
    public string[] keywords;
    public string[] alts;
    public string action_type;
    public string notes;
}
