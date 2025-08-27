using UnityEngine;

public class CanvasDock : MonoBehaviour
{
    [Header("Canvas that currently lives under Desk Simulator")]
    public Transform canvas;            // AR_DeskSimulation/Desk Simulator/Canvas
    [Header("Where to park the canvas in Canvas mode (empty root at scene top level)")]
    public Transform parkingRoot;       // empty GameObject in scene root (e.g., "CanvasParkingRoot")

    private Transform originalParent;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    private Vector3 originalLocalScale;
    private bool docked = true;

    void Awake()
    {
        if (!canvas) canvas = transform; // allow dropping this on the Canvas object
        originalParent = canvas.parent;
        originalLocalPos = canvas.localPosition;
        originalLocalRot = canvas.localRotation;
        originalLocalScale = canvas.localScale;
    }

    public void DetachToParking()
    {
        if (!canvas || !parkingRoot || !docked) return;
        canvas.SetParent(parkingRoot, true); // keep world pose
        docked = false;
        if (!canvas.gameObject.activeSelf) canvas.gameObject.SetActive(true);
        Debug.Log("[CanvasDock] Canvas detached to parking root.");
    }

    public void RestoreToDesk()
    {
        if (!canvas || docked) return;
        canvas.SetParent(originalParent, false); // restore local pose
        canvas.localPosition = originalLocalPos;
        canvas.localRotation = originalLocalRot;
        canvas.localScale = originalLocalScale;
        docked = true;
        Debug.Log("[CanvasDock] Canvas re-attached under Desk Simulator.");
    }

    public bool IsDocked => docked;
}
