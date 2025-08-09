using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public static class WavUtil
{
    public static void SaveWav(string path, AudioClip clip)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
        int sampleCount = clip.samples * clip.channels;
        float[] samples = new float[sampleCount];
        clip.GetData(samples, 0);

        // Convert float [-1,1] to 16-bit PCM
        byte[] pcm16 = new byte[sampleCount * 2];
        int pcmIndex = 0;
        for (int i = 0; i < sampleCount; i++)
        {
            short val = (short)Mathf.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
            pcm16[pcmIndex++] = (byte)(val & 0xFF);
            pcm16[pcmIndex++] = (byte)((val >> 8) & 0xFF);
        }

        WriteWavHeader(fileStream, clip.channels, clip.frequency, pcm16.Length);
        fileStream.Write(pcm16, 0, pcm16.Length);
    }

    static void WriteWavHeader(Stream stream, int channels, int sampleRate, int dataLength)
    {
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        int byteRate = sampleRate * channels * 2;
        writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataLength);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)(channels * 2));
        writer.Write((short)16);
        writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
        writer.Write(dataLength);
    }

    // Coroutine loader: load local WAV file into an AudioClip
    public static IEnumerator LoadWavClip(string fullPath, Action<AudioClip> onLoaded)
    {
        string url = "file://" + fullPath;
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return www.SendWebRequest();
        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning("[WavUtil] Load failed: " + www.error);
            onLoaded?.Invoke(null);
        }
        else
        {
            var clip = DownloadHandlerAudioClip.GetContent(www);
            onLoaded?.Invoke(clip);
        }
    }
}
