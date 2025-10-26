using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlippingBook : MonoBehaviour
{
    public GameObject pagePrefab;           // A single flat page (Quad or Plane)
    public Transform pageParent;            // Where to spawn pages (empty GameObject inside book spine)
    public Button nextButton, prevButton;   // Optional buttons to control flipping

    private GameObject[] pages;
    private int currentPage = 0;
    private int totalPages = 20;

    void Start()
    {
        pages = new GameObject[totalPages];

        for (int i = 0; i < totalPages; i++)
        {
            GameObject page = Instantiate(pagePrefab, pageParent);
            page.name = $"Page_{i + 1}";
            page.transform.localPosition = new Vector3(0, 0, i * 0.001f); // small Z offset
            page.transform.localRotation = Quaternion.identity;
            page.SetActive(i == 0); // only first page is visible

            // Add step label
            TextMeshProUGUI text = page.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.text = $"Step {i + 1}";

            pages[i] = page;
        }

        if (nextButton != null)
            nextButton.onClick.AddListener(NextPage);
        if (prevButton != null)
            prevButton.onClick.AddListener(PreviousPage);
    }

    public void NextPage()
    {
        if (currentPage < totalPages - 1)
        {
            pages[currentPage].SetActive(false);
            currentPage++;
            pages[currentPage].SetActive(true);
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
        {
            pages[currentPage].SetActive(false);
            currentPage--;
            pages[currentPage].SetActive(true);
        }
    }

    public void GoToPage(int pageIndex)
    {
        if (pageIndex >= 0 && pageIndex < totalPages)
        {
            pages[currentPage].SetActive(false);
            currentPage = pageIndex;
            pages[currentPage].SetActive(true);
        }
    }
}
