using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SimpleSteps : MonoBehaviour
{
    public TextMeshProUGUI titleText, bodyText;
    [TextArea(3, 10)] public string rawBullets = "- Welcome to TechTutor\n- Ask your question\n- Say 'Next' to continue";
    List<string> steps = new(); int idx;

    void Start() { steps = Parse(rawBullets); Show(); }
    public void Next() { idx = Mathf.Min(idx + 1, steps.Count - 1); Show(); }
    public void Back() { idx = Mathf.Max(idx - 1, 0); Show(); }

    void Show()
    {
        if (steps.Count == 0) { titleText.text = ""; bodyText.text = ""; return; }
        titleText.text = $"Step {idx + 1}/{steps.Count}";
        bodyText.text = steps[idx];
        // Tell the speech bridge to speak
        SendMessage("OnStepChangedSpeak", steps[idx], SendMessageOptions.DontRequireReceiver);
        // (optional) gesture hook
        SendMessage("OnStepGesture", steps[idx], SendMessageOptions.DontRequireReceiver);
    }
    static List<string> Parse(string s)
    {
        var list = new List<string>(); if (string.IsNullOrWhiteSpace(s)) return list;
        foreach (var line in s.Split('\n'))
        {
            var L = line.Trim();
            if (L.StartsWith("-") || L.StartsWith("•") || char.IsDigit(L[0])) list.Add(L.TrimStart('-', '•', ' ', '\t', '1', '2', '3', '4', '5', '.'));
        }
        return list;
    }
}
