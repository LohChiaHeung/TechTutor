using UnityEngine;

public class GuideRunContext : MonoBehaviour
{
    public static GuideRunContext I;

    // data to hand to AR_ImageTest
    public Texture2D screenshot;   // the captured image (or null if not set)
    public AIGuide guide;          // AI steps (JSON parsed)

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Clear()
    {
        screenshot = null;
        guide = null;
    }
}
