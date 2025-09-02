using System;
using System.Collections;
using UnityEngine;

public static class OpenAIAgent
{
    // Use a callback to return the string result
    public static IEnumerator SendPrompt(string prompt, Action<string> onResult)
    {
        yield return new WaitForSeconds(1.5f); // simulate delay

        string mockReply =
@"Step 1: Open Gmail.
Step 2: Click the 'Compose' button.
Step 3: Type your email.
Step 4: Click 'Send'.";

        onResult?.Invoke(mockReply);
    }
}
