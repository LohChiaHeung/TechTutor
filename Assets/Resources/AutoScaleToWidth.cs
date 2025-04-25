using UnityEngine;

public class AutoScaleToWidth : MonoBehaviour
{
    public float targetWidthMeters = 0.4f; // 40cm

    void Start()
    {
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            float currentWidth = renderer.bounds.size.x;
            float scaleFactor = targetWidthMeters / currentWidth;
            transform.localScale = transform.localScale * scaleFactor;
        }
    }
}


