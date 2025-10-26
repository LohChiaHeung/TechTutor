using UnityEngine;
using UnityEngine.UI;

public class ModelTapHandler : MonoBehaviour
{
    public GameObject canvasObject;
    public GameObject backgroundPanelObject;
    public AudioClip descriptionAudio;
    public Button closeButton;
    public GameObject keyHighlightGroup; // Optional

    private AudioSource audioSource;

    void Start()
    {
        if (canvasObject != null) canvasObject.SetActive(false);
        if (backgroundPanelObject != null) backgroundPanelObject.SetActive(false);
        if (keyHighlightGroup != null) keyHighlightGroup.SetActive(false);

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(OnCloseButtonTapped);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    Debug.Log("[ModelTapHandler] Model tapped!");
                    ShowDescriptionCard();
                }
            }
        }
    }

    public void ShowDescriptionCard()
    {
        if (canvasObject != null) canvasObject.SetActive(true);
        if (backgroundPanelObject != null) backgroundPanelObject.SetActive(true);
        if (keyHighlightGroup != null) keyHighlightGroup.SetActive(true);

        if (descriptionAudio != null && audioSource != null)
        {
            audioSource.clip = descriptionAudio;
            audioSource.Play();
        }
    }

    public void OnCloseButtonTapped()
    {
        if (canvasObject != null) canvasObject.SetActive(false);
        if (backgroundPanelObject != null) backgroundPanelObject.SetActive(false);
        if (keyHighlightGroup != null) keyHighlightGroup.SetActive(false);
    }
}
