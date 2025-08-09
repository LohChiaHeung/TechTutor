// Modified Book.cs to apply page flip animation to a 3D model instead of UI
// This version assumes your 3D book has page GameObjects that you want to rotate like a flip

using UnityEngine;
using System.Collections;

public class Book3D : MonoBehaviour
{
    public GameObject leftPage;      // assign the left page mesh
    public GameObject rightPage;     // assign the right page mesh
    public float flipDuration = 1f;  // time in seconds for a flip
    public bool interactable = true;
    private bool isFlipping = false;
    public BookStepManager stepManager;  // Drag the script in Inspector


    private int currentPageIndex = 0;
    public int totalPages = 5;       // total number of pages

    void Update()
    {
        if (!interactable || isFlipping)
            return;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            FlipRightToLeft();
        }
    }

    public void FlipRightToLeft()
    {
        if (currentPageIndex < totalPages)
        {
            StartCoroutine(FlipPage(rightPage.transform));
            currentPageIndex++;
        }
    }

    public void FlipLeftToRight()
    {
        if (currentPageIndex > 0)
        {
            StartCoroutine(FlipPage(leftPage.transform, true));
            currentPageIndex--;
        }
    }

    IEnumerator FlipPage(Transform page, bool reverse = false)
    {
        isFlipping = true;

        Debug.Log($"[Book3D] 🌀 Starting {(reverse ? "LeftToRight" : "RightToLeft")} flip...");

        Quaternion startRot = page.localRotation;
        //Quaternion endRot = reverse ?
        //    startRot * Quaternion.Euler(0, 0, -180) :  // reverse flip
        //    startRot * Quaternion.Euler(0, 0, 180);   // normal flip
        
        Quaternion endRot = startRot * Quaternion.Euler(0, -180, 0); // ← update this


        float t = 0;
        while (t < flipDuration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / flipDuration);
            page.localRotation = Quaternion.Slerp(startRot, endRot, progress);
            yield return null;
        }

        page.localRotation = endRot;
        Debug.Log("[Book3D] ✅ Flip complete.");

        if (!reverse && stepManager != null)
        {
            stepManager.NextPage(); // ⬅️ Advance to next step after flipping
        }

        isFlipping = false;

    }

}
