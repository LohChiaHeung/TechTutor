using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public static class TesseractEditorRunner
{
#if UNITY_EDITOR_WIN && !UNITY_ANDROID
    private static readonly string ToolsDir = Path.Combine(Application.dataPath, "Editor", "TesseractWin");
    private static readonly string TesseractExe = Path.Combine(ToolsDir, "tesseract.exe");
    private static readonly string TessdataDir = Path.Combine(ToolsDir, "tessdata");

    public static string Recognize(Texture2D tex, string lang = "eng", string extraArgs = "--psm 6 --oem 1 -c preserve_interword_spaces=1")
    {
        if (!File.Exists(TesseractExe))
        {
            Debug.LogError("[Tesseract] tesseract.exe not found: " + TesseractExe);
            return "";
        }
        if (!Directory.Exists(TessdataDir) || !File.Exists(Path.Combine(TessdataDir, $"{lang}.traineddata")))
        {
            Debug.LogError("[Tesseract] tessdata or language file missing: " + TessdataDir + $" / {lang}.traineddata");
            return "";
        }

        // Save temp PNG
        string tmpDir = Path.Combine(Application.temporaryCachePath, "tess");
        Directory.CreateDirectory(tmpDir);
        string inputPng = Path.Combine(tmpDir, "input.png");
        File.WriteAllBytes(inputPng, tex.EncodeToPNG());
        Debug.Log("[Tesseract] Input PNG: " + inputPng);

        var psi = new ProcessStartInfo
        {
            FileName = TesseractExe,
            // use stdout output, force tessdata dir, set language and args
            Arguments = $"\"{inputPng}\" stdout -l {lang} --tessdata-dir \"{TessdataDir}\" {extraArgs}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        try
        {
            using (var p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                string err = p.StandardError.ReadToEnd();
                p.WaitForExit();

                if (!string.IsNullOrWhiteSpace(err))
                    Debug.LogWarning("[Tesseract][stderr] " + err.Trim());

                if (p.ExitCode != 0)
                {
                    Debug.LogWarning($"[Tesseract] ExitCode={p.ExitCode}");
                    return "";
                }

                return output?.Trim() ?? "";
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Tesseract] " + e);
            return "";
        }
    }
#else
    public static string Recognize(Texture2D tex, string lang = "eng", string extraArgs = "--psm 6 --oem 1")
    {
        UnityEngine.Debug.LogWarning("[Tesseract] Editor-only runner. Build to Android for tess-two.");
        return "";
    }
#endif
}
