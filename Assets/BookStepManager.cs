using UnityEngine;
using TMPro;

public class BookStepManager : MonoBehaviour
{
    public MeshRenderer leftPageRenderer;
    public MeshRenderer rightPageRenderer;

    public Material pageMaterialTemplate; // Assign a base paper material
    public Font textFont;

    public string[] tutorialSteps;
    private int stepIndex = 0;

    public TextMeshPro leftTextTMP;
    public TextMeshPro rightTextTMP;

    void Start()
    {
        UpdatePages();
    }

    public void NextPage()
    {
        if (stepIndex + 2 < tutorialSteps.Length)
        {
            stepIndex += 2;
            UpdatePages();
        }
    }

    void UpdatePages()
    {
        string leftText = stepIndex < tutorialSteps.Length ? tutorialSteps[stepIndex] : "";
        string rightText = stepIndex + 1 < tutorialSteps.Length ? tutorialSteps[stepIndex + 1] : "";

        leftPageRenderer.material = GenerateTextMaterial(leftText);
        rightPageRenderer.material = GenerateTextMaterial(rightText);
    }

    Material GenerateTextMaterial(string content)
    {
        Texture2D tex = new Texture2D(512, 512);
        Color fillColor = new Color(1f, 1f, 1f, 1f);
        Color[] fillPixels = tex.GetPixels();
        for (int i = 0; i < fillPixels.Length; i++) fillPixels[i] = fillColor;
        tex.SetPixels(fillPixels);
        tex.Apply();

        // You can draw text on the texture using GUI if needed (see below)

        Material mat = new Material(pageMaterialTemplate);
        mat.mainTexture = tex;
        return mat;
    }
}
