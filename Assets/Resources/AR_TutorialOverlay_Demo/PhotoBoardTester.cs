using UnityEngine;

public class PhotoBoardTester : MonoBehaviour
{
    public GameObject photoBoardPrefab;

    void Start()
    {
        var tex = Resources.Load<Texture2D>("sampleshot");
        if (!tex) { Debug.LogError("Put sampleshot.png in Assets/Resources"); return; }

        var board = Instantiate(photoBoardPrefab);
        var ctrl = board.GetComponent<PhotoBoardController>();
        ctrl.SetTexture(tex, 0.35f); // 35 cm wide poster
        ctrl.FaceCamera();

        // In AR, position is relative to the AR Camera's tracked pose
        Transform cam = Camera.main.transform;

        // Keep upright (remove any camera pitch/roll from the forward vector)
        Vector3 forward = cam.forward;
        forward.y = 0f;
        forward.Normalize();

        board.transform.position = cam.position + forward * 0.6f;
        board.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
