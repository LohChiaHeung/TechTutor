using System;
using System.Collections.Generic;

[Serializable]
public class OcrWord
{
    public string text;
    public float x, y, w, h;
    public float conf;
}

//[Serializable]
//public class OcrResponse
//{
//    public int width;
//    public int height;
//    public List<OcrWord> words = new();
//}
