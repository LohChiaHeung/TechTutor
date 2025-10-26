using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NarrationPlayerCached : MonoBehaviour
{
    [Header("Drag your plugin's TTS Manager here (from the imported prefabs/scripts)")]
    public MonoBehaviour ttsManager; // e.g. the package's manager component in your scene

    [Header("Playback")]
    public bool interruptPrevious = true;

    private AudioSource source;
    private readonly Dictionary<string, AudioClip> cache = new(); // text -> clip

    void Awake()
    {
        source = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D voice; set >0 if you want 3D
        DebugLogTtsMethods();  // <-- add this line temporarily
    }

    void DebugLogTtsMethods()
    {
        if (ttsManager == null) { Debug.LogWarning("ttsManager not assigned"); return; }
        var t = ttsManager.GetType();
        var methods = t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        foreach (var m in methods) Debug.Log($"[TTS API] {m}");
    }
    public void Stop()
    {
        if (source && source.isPlaying) source.Stop();
        // If your plugin exposes a Cancel/Stop method, call it here too.
    }

    public void SpeakFromPanel(PanelNarration panel)
    {
        if (!panel) return;
        Speak(panel.BuildNarrationText());
    }

    public void Speak(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        if (interruptPrevious) Stop();

        // 1) Use cache if we have it
        if (cache.TryGetValue(text, out var clip) && clip != null)
        {
            Play(clip);
            return;
        }

        // 2) Ask plugin to synthesize once, then cache
        if (ttsManager == null)
        {
            Debug.LogWarning("[NarrationPlayerCached] ttsManager is not assigned.");
            return;
        }

        // Try to find a method like: SynthesizeAndPlay(string, AudioSource, Action<AudioClip>)
        var t = ttsManager.GetType();
        var withCb = t.GetMethod("SynthesizeAndPlay", new[] { typeof(string), typeof(AudioSource), typeof(Action<AudioClip>) });
        if (withCb != null)
        {
            Action<AudioClip> onReady = (AudioClip generated) =>
            {
                if (generated != null) cache[text] = generated;
            };
            withCb.Invoke(ttsManager, new object[] { text, source, onReady });
            return;
        }

        // Fallback: try SynthesizeAndPlay(string, AudioSource) and then capture source.clip when ready
        var noCb = t.GetMethod("SynthesizeAndPlay", new[] { typeof(string), typeof(AudioSource) });
        if (noCb != null)
        {
            noCb.Invoke(ttsManager, new object[] { text, source });
            StartCoroutine(CaptureClipWhenReady(text, timeoutSec: 5f));
            return;
        }

        // If your plugin uses a different method name, change here:
        Debug.LogWarning("[NarrationPlayerCached] Couldn't find plugin method. Update NarrationPlayerCached to match your TTS API.");
    }

    private IEnumerator CaptureClipWhenReady(string key, float timeoutSec)
    {
        float end = Time.unscaledTime + timeoutSec;
        while (Time.unscaledTime < end)
        {
            if (source.clip != null)
            {
                cache[key] = source.clip;
                yield break;
            }
            yield return null;
        }
        Debug.LogWarning("[NarrationPlayerCached] Timed out waiting for synthesized clip.");
    }

    private void Play(AudioClip clip)
    {
        source.clip = clip;
        source.Play();
    }

    public void ClearCache() => cache.Clear();
}
