using UnityEngine;

public class Draggable : MonoBehaviour
{
    private Vector3 offset;
    private Plane dragPlane;
    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
    }

    void OnMouseDown()
    {
        dragPlane = new Plane(Vector3.up, transform.position); // Y plane for Ground Plane
        Ray camRay = mainCam.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(camRay, out float dist))
        {
            offset = transform.position - camRay.GetPoint(dist);
        }
    }

    void OnMouseDrag()
    {
        Ray camRay = mainCam.ScreenPointToRay(Input.mousePosition);

        if (dragPlane.Raycast(camRay, out float dist))
        {
            transform.position = camRay.GetPoint(dist) + offset;
        }
    }
}