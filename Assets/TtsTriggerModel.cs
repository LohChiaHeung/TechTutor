// File: TtsTriggerModel.cs
// Purpose: Speak a line (via TTS audio) and, when playback finishes, trigger a model spawn.
// Scope: Standalone. No SpeechToText references to avoid conflicts with your other scenes.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class TtsTriggerModel : MonoBehaviour
{
    [Header("Assign in Inspector")]
    [Tooltip("Dedicated AudioSource for TTS playback (do NOT reuse global SFX/voice sources).")]
    public AudioSource ttsAudio;

    [Tooltip("Your PlaneLocker (or the script that spawns/activates models).")]
    public PlaneLocker planeLocker;

    [System.Serializable]
    public struct Trigger
    {
        [Tooltip("Friendly key (e.g., 'keyboard', 'mouse', 'monitor').")]
        public string key;

        [Tooltip("Model index for PlaneLocker.SpawnPreviewForSelectedModel().")]
        public int modelIndex;

        [Tooltip("TTS clip to play before triggering the model.")]
        public AudioClip ttsClip;
    }

    [Header("Triggers")]
    [Tooltip("List of key→(ttsClip, modelIndex) mappings.")]
    public List<Trigger> triggers = new List<Trigger>();

    [Header("Timing")]
    [Tooltip("Extra wait (seconds) after TTS ends before spawning.")]
    public float postTtsDelay = 0.15f;

    bool _isBusy;

    void Reset()
    {
        // Try to find a local AudioSource automatically
        if (!ttsAudio) ttsAudio = GetComponent<AudioSource>();
    }

    /// <summary>
    /// Public entry point you can call from UI buttons or code.
    /// Example: SpeakAndTriggerByKey(\"keyboard\");
    /// </summary>
    public void SpeakAndTriggerByKey(string key)
    {
        if (_isBusy)
        {
            Debug.Log("[TTS-Trigger] Busy, ignoring request: " + key);
            return;
        }

        int idx = triggers.FindIndex(t => t.key.Equals(key, System.StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            Debug.LogWarning("[TTS-Trigger] No trigger found for key: " + key);
            return;
        }

        var trg = triggers[idx];
        StartCoroutine(Co_SpeakThenTrigger(trg));
    }

    IEnumerator Co_SpeakThenTrigger(Trigger trg)
    {
        _isBusy = true;

        if (!planeLocker)
        {
            Debug.LogError("[TTS-Trigger] PlaneLocker not assigned.");
            _isBusy = false;
            yield break;
        }

        // 1) Play TTS (if provided)
        if (ttsAudio && trg.ttsClip)
        {
            ttsAudio.Stop();
            ttsAudio.clip = trg.ttsClip;
            ttsAudio.Play();

            // Wait until TTS finishes
            while (ttsAudio.isPlaying)
                yield return null;
        }
        else
        {
            Debug.LogWarning("[TTS-Trigger] No TTS clip or AudioSource assigned; skipping voice playback.");
        }

        // 2) Small, configurable buffer after TTS
        if (postTtsDelay > 0f)
            yield return new WaitForSeconds(postTtsDelay);

        // 3) Trigger the model section
        try
        {
            planeLocker.SpawnPreviewForSelectedModel(trg.modelIndex);
            Debug.Log($"[TTS-Trigger] Spawned model index {trg.modelIndex} for key '{trg.key}'.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TTS-Trigger] Failed to spawn model: " + ex.Message);
        }

        _isBusy = false;
    }
}
