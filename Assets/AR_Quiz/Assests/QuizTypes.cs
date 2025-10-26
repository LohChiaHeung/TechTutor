using System;

[Serializable] public class QuizPayload { public string topic; public QuizItem[] items; }

[Serializable]
public class QuizItem
{
    // add "pick3d" as a supported type
    public string type;          // "mcq" | "tf" | "pick3d"

    // common
    public string question;
    public string explain;

    // MCQ
    public string[] options;     // for mcq (length 4 recommended)
    public int answer_index;     // for mcq (0..3)

    // True/False
    public bool answer_bool;     // for tf

    // Pick on 3D model
    // Accepted labels that count as correct (matched against KeyTag.keyName or GO name; case-insensitive)
    public string[] accept_names; // e.g. { "Windows", "Win", "Super" }
}
