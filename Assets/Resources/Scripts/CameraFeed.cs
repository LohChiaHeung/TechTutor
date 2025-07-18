using UnityEngine;
using UnityEngine.UI;

public class CameraFeed : MonoBehaviour
{
    private WebCamTexture webcamTexture;
    public RawImage rawImage;

    void Start()
    {
        webcamTexture = new WebCamTexture();
        rawImage.texture = webcamTexture;
        rawImage.material.mainTexture = webcamTexture;
        webcamTexture.Play();
    }
}
