using UnityEngine;

public static class PixelRectOutline
{
    // Draws a green rectangle outline at (x,y) with size (w,h) onto tex.
    // (x,y) are bottom-left pixel coordinates in Texture2D space.
    public static void DrawRectOutline(Texture2D tex, int x, int y, int w, int h, int thickness = 2)
    {
        if (!tex || w <= 0 || h <= 0) return;
        thickness = Mathf.Max(1, thickness);
        // Top
        FillRect(tex, x, y + h - thickness, w, thickness);
        // Bottom
        FillRect(tex, x, y, w, thickness);
        // Left
        FillRect(tex, x, y, thickness, h);
        // Right
        FillRect(tex, x + w - thickness, y, thickness, h);
    }

    static void FillRect(Texture2D tex, int x, int y, int w, int h)
    {
        int x0 = Mathf.Clamp(x, 0, tex.width - 1);
        int y0 = Mathf.Clamp(y, 0, tex.height - 1);
        int x1 = Mathf.Clamp(x + w, 0, tex.width);
        int y1 = Mathf.Clamp(y + h, 0, tex.height);

        int ww = x1 - x0;
        int hh = y1 - y0;
        if (ww <= 0 || hh <= 0) return;

        Color[] row = new Color[ww];
        for (int i = 0; i < ww; i++) row[i] = Color.green;

        for (int yy = y0; yy < y1; yy++)
            tex.SetPixels(x0, yy, ww, 1, row);
    }
}
