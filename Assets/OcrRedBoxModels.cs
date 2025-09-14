using System;
using System.Collections.Generic;

[Serializable]
public class OcrRedboxWord
{
    public string text;
    public float x, y, w, h, conf;
}

[Serializable]
public class OcrRedboxResponse
{
    public int width;
    public int height;
    public List<OcrRedboxWord> words;
}
