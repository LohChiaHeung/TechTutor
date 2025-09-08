using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OcrWord_New
{
    // Pixel coords in the ORIGINAL image (origin: top-left)
    public float x;   // left
    public float y;   // top
    public float w;   // width
    public float h;   // height
    public string text;
    public float conf;
}

[Serializable]
public class OcrResponse_New
{
    public int width;     // original image width in pixels
    public int height;    // original image height in pixels
    public List<OcrWord_New> words = new List<OcrWord_New>();
}
