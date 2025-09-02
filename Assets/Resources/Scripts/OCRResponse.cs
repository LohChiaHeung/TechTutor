// OCRResponse.cs
using System;

[Serializable]
public class OCRResponse
{
    public int width;      // image width
    public int height;     // image height
    public OCRWord[] words;
}

[Serializable]
public class OCRWord
{
    public string text;    // the recognized text
    public float x, y;     // top-left pixel position
    public float w, h;     // width, height in pixels
}
