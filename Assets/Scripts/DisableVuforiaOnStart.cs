using UnityEngine;
using Vuforia;

public class DisableVuforiaOnStart : MonoBehaviour
{
    void Start()
    {
        if (VuforiaBehaviour.Instance != null)
        {
            VuforiaBehaviour.Instance.enabled = false;
            Debug.Log("✅ Vuforia disabled on startup.");
        }
    }
}
