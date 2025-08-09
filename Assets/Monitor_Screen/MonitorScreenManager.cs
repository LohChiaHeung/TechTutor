using UnityEngine;

public class ScreenMaterialManager : MonoBehaviour
{
    public Renderer screenRenderer;       // assign Cube_RenderMonitor (or Quad)
    public Material[] screenMaterials;    // assign materials: Desktop, StartMenu, Gmail, etc.

    void Awake()
    {
        if (!screenRenderer) screenRenderer = GetComponent<Renderer>();
    }

    public void SetScreen(int index)
    {
        if (!screenRenderer || screenMaterials == null) return;
        if (index < 0 || index >= screenMaterials.Length) return;

        // Single material slot assumed. If you have multiple, copy array and replace the slot you need.
        screenRenderer.material = screenMaterials[index];

        Debug.Log($"[ScreenMaterialManager] Applied material index {index} -> {screenMaterials[index].name}");
    }
}
