using UnityEngine;
public class Billboard : MonoBehaviour
{
    Camera cam;
    void Start() { cam = Camera.main; }
    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (cam) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}
