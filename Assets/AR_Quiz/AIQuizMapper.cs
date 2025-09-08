using System.Collections.Generic;

public static class AIQuizMapper
{
    public static List<QuizQuestion> ToRuntimeQuestions(QuizPayload p)
    {
        var list = new List<QuizQuestion>();
        if (p == null || p.items == null) return list;

        foreach (var it in p.items)
        {
            if (it == null) continue;

            // Only map MCQ for now; pick3d is managed by your keyboard special question
            if ((it.type ?? "mcq").ToLower() == "mcq")
            {
                var q = new QuizQuestion
                {
                    type = "mcq",
                    prompt = it.question,
                    options = SafeOptions(it.options),
                    correct = ClampIndex(it.answer_index),
                    note = it.explain
                };
                list.Add(q);
            }
        }
        return list;
    }

    private static string[] SafeOptions(string[] src)
    {
        var arr = new string[4] { "", "", "", "" };
        if (src != null) for (int i = 0; i < arr.Length && i < src.Length; i++) arr[i] = src[i] ?? "";
        return arr;
    }
    private static int ClampIndex(int ix) => ix < 0 ? 0 : (ix > 3 ? 3 : ix);
}
